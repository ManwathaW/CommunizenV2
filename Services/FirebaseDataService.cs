using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Storage;
using FirebaseAdmin.Auth;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;


public class FirebaseDataService : IFirebaseDataService
{

    private readonly FirebaseClient _firebaseClient;
    private readonly string _storageBucket;
    private const string APPOINTMENTS_PATH = "appointments";
    private const string AVAILABILITY_PATH = "availability";
    private readonly IFirebaseAuthService _authService;
    private readonly string _authToken;
    public FirebaseDataService()
    {
        _authToken = FirebaseConfig.ApiKey;
        _firebaseClient = new FirebaseClient(
            "https://communizen-c112-default-rtdb.asia-southeast1.firebasedatabase.app/",
            new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult(_authToken)
            });
    }


    #region User Profile Management
    public async Task CreateUserProfileAsync(string userId, RegisterRequest profile)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty");

            var userData = new
            {
                Email = profile.Email,
                FullName = profile.FullName,
                PhoneNumber = profile.PhoneNumber,
                Role = (int)profile.Role,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                ProfileImage = "profile_placeholder.png"
            };

            await _firebaseClient
                .Child("users")
                .Child(userId)
                .PutAsync(JsonConvert.SerializeObject(userData));

            if (profile.Role == UserRole.Practitioner)
            {
                var practiceData = new PracticeProfile
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Email = profile.Email,
                    Name = profile.FullName,
                    PracticeName = $"Dr. {profile.FullName}'s Practice",
                    PhoneNumber = profile.PhoneNumber,
                    Specialization = profile.Specialization ?? "General Practice",
                    LicenseNumber = profile.LicenseNumber,
                    Location = "Not Set",
                    Latitude = 0.0,
                    Longitude = 0.0,
                    ProfileImage = "profile_placeholder.png",
                    CreatedAt = DateTime.UtcNow
                };

                await SavePracticeProfileAsync(userId, practiceData);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating profile: {ex.Message}");
            throw new Exception($"Failed to create user profile: {ex.Message}", ex);
        }
    }

    public async Task<UserProfile> GetUserProfileAsync(string userId)
    {
        try
        {
            var userProfile = await _firebaseClient
                .Child("users")
                .Child(userId)
                .OnceSingleAsync<UserProfile>();

            if (userProfile != null)
            {
                var imageData = await _firebaseClient
                    .Child("profile_images")
                    .Child(userId)
                    .OnceSingleAsync<Dictionary<string, string>>();

                if (imageData != null && imageData.ContainsKey("imageData"))
                {
                    userProfile.ProfileImage = imageData["imageData"];
                }
            }

            return userProfile;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve user profile: {ex.Message}");
        }
    }

    public async Task UpdateUserProfileAsync(string userId, UserProfile updatedProfile)
    {
        try
        {
            await _firebaseClient
                .Child("users")
                .Child(userId)
                .PutAsync(JsonConvert.SerializeObject(updatedProfile));
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to update user profile: {ex.Message}");
        }
    }

    public async Task DeleteUserProfileAsync(string userId)
    {
        try
        {
            await _firebaseClient
                .Child("users")
                .Child(userId)
                .DeleteAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to delete user profile: {ex.Message}");
        }
    }
    #endregion

    #region User Management
    public async Task<bool> CheckUserExistsAsync(string userId)
    {
        try
        {
            var user = await _firebaseClient
                .Child("users")
                .Child(userId)
                .OnceSingleAsync<object>();

            return user != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<UserProfile>> GetUsersByRoleAsync(UserRole role)
    {
        try
        {
            var users = await _firebaseClient
                .Child("users")
                .OrderBy("Role")
                .EqualTo((int)role)
                .OnceAsync<UserProfile>();

            return users?.Select(u => u.Object).ToList() ?? new List<UserProfile>();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve users by role: {ex.Message}");
        }
    }
    #endregion

    #region Practice Profile Management
    public async Task SavePracticeProfileAsync(string userId, PracticeProfile profile)
    {
        try
        {
            await _firebaseClient
                .Child("practitioners")
                .Child(userId)
                .Child(profile.Id)
                .PutAsync(JsonConvert.SerializeObject(profile));
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save practice profile: {ex.Message}");
        }
    }

    public async Task UpdatePracticeProfileAsync(string userId, string profileId, PracticeProfile profile)
    {
        try
        {
            await _firebaseClient
                .Child("practitioners")
                .Child(userId)
                .Child(profileId)
                .PutAsync(JsonConvert.SerializeObject(profile));
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to update practice profile: {ex.Message}");
        }
    }

    public async Task DeletePracticeProfileAsync(string userId, string profileId)
    {
        try
        {
            await _firebaseClient
                .Child("practitioners")
                .Child(userId)
                .Child(profileId)
                .DeleteAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to delete practice profile: {ex.Message}");
        }
    }

    public async Task<List<PracticeProfile>> GetAllPractitionersAsync()
    {
        try
        {
            Debug.WriteLine("Getting all practitioners from Firebase");
            var allPractitioners = new List<PracticeProfile>();

            // Get all user nodes first
            var userNodes = await _firebaseClient
                .Child("practitioners")
                .OnceAsync<Dictionary<string, PracticeProfile>>();

            // For each user node
            foreach (var userNode in userNodes)
            {
                var userId = userNode.Key;
                var practitionerProfiles = userNode.Object;

                if (practitionerProfiles != null)
                {
                    foreach (var profile in practitionerProfiles.Values)
                    {
                        try
                        {
                            // Get profile image
                            profile.ProfileImage = await GetProfileImageAsync(userId);
                            allPractitioners.Add(profile);
                            Debug.WriteLine($"Added practitioner: {profile.Name}, ID: {profile.Id}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error processing practitioner: {ex.Message}");
                        }
                    }
                }
            }

            Debug.WriteLine($"Total practitioners found: {allPractitioners.Count}");
            return allPractitioners;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in GetAllPractitionersAsync: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw new Exception($"Failed to get practitioners: {ex.Message}");
        }
    }

    public async Task<List<PracticeProfile>> GetPractitionerProfilesAsync(string userId)
    {
        try
        {
            var profiles = await _firebaseClient
                .Child("practitioners")
                .Child(userId)
                .OnceAsync<PracticeProfile>();

            var result = profiles?.Select(p =>
            {
                p.Object.Id = p.Key;
                return p.Object;
            }).ToList() ?? new List<PracticeProfile>();

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get practitioner profiles: {ex.Message}");
        }
    }

    public async Task<List<PracticeProfile>> SearchPractitionersAsync(string searchQuery)
    {
        try
        {
            var allProfiles = await GetAllPractitionersAsync();

            if (string.IsNullOrWhiteSpace(searchQuery))
                return allProfiles;

            return allProfiles.Where(p =>
                p.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                p.PracticeName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                p.Specialization.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                p.Location.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to search practitioners: {ex.Message}");
        }
    }
    #endregion

    #region Image Management
    public async Task<string> UploadProfileImageAsync(string userId, Stream imageStream)
    {
        try
        {
            Debug.WriteLine($"Starting image upload for user: {userId}");
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();
            var base64Image = Convert.ToBase64String(imageBytes);

            var imageData = new { imageData = base64Image };
            await _firebaseClient
                .Child("profile_images")
                .Child(userId)
                .PutAsync(JsonConvert.SerializeObject(imageData));

            Debug.WriteLine("Image uploaded successfully");
            return base64Image;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in UploadProfileImageAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<string> GetProfileImageAsync(string userId)
    {
        try
        {
            var imageData = await _firebaseClient
                .Child("profile_images")
                .Child(userId)
                .OnceSingleAsync<Dictionary<string, string>>();

            if (imageData != null && imageData.ContainsKey("imageData"))
            {
                return imageData["imageData"];
            }

            return "profile_placeholder.png";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting profile image: {ex.Message}");
            return "profile_placeholder.png";
        }
    }


    public async Task<string> UploadChatImageAsync(string chatId, Stream imageStream)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();
            var base64Image = Convert.ToBase64String(imageBytes);

            var imagePath = $"chat_images/{chatId}/{Guid.NewGuid()}";
            var imageData = new { imageData = base64Image };

            await _firebaseClient
                .Child(imagePath)
                .PutAsync(JsonConvert.SerializeObject(imageData));

            return imagePath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error uploading chat image: {ex.Message}");
            throw;
        }
    }
    #endregion

    #region Session and Statistics
    public async Task SaveUserSessionAsync(string userId, UserSession session)
    {
        try
        {
            await _firebaseClient
                .Child("sessions")
                .Child(userId)
                .PostAsync(JsonConvert.SerializeObject(session));
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save user session: {ex.Message}");
        }
    }

    public async Task<Dictionary<string, object>> GetUserStatisticsAsync(string userId)
    {
        try
        {
            var stats = await _firebaseClient
                .Child("statistics")
                .Child(userId)
                .OnceSingleAsync<Dictionary<string, object>>();

            return stats ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve user statistics: {ex.Message}");
        }
    }
    #endregion

    #region Authentication
    public async Task<string> GetCurrentUserIdAsync()
    {
        try
        {
            var auth = FirebaseAuth.DefaultInstance;
            var decodedToken = await auth.VerifyIdTokenAsync(_authToken);
            return decodedToken.Uid;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get user ID: {ex.Message}");
        }
    }
    #endregion


    #region Managing Appointments 

    public async Task<List<TimeSlot>> GetTimeSlotsAsync(DateTime date)
    {
        try
        {
            Debug.WriteLine($"=== GetTimeSlotsAsync START ===");
            Debug.WriteLine($"Requesting time slots for date: {date:yyyy-MM-dd}");

            var dateKey = date.ToString("yyyy-MM-dd");
            var practitionerId = await GetCurrentPractitionerId();
            Debug.WriteLine($"Using practitionerId: {practitionerId}");

            var path = $"{AVAILABILITY_PATH}/{practitionerId}/{dateKey}";
            Debug.WriteLine($"Attempting to fetch from path: {path}");

            var snapshot = await _firebaseClient
                .Child(AVAILABILITY_PATH)
                .Child(practitionerId)
                .Child(dateKey)
                .OnceAsync<object>();

            if (snapshot == null)
            {
                Debug.WriteLine("No data found in Firebase");
                return new List<TimeSlot>();
            }

            var timeSlots = new List<TimeSlot>();
            foreach (var item in snapshot)
            {
                Debug.WriteLine($"Processing item with key: {item.Key}");
                Debug.WriteLine($"Raw data: {JsonConvert.SerializeObject(item.Object)}");

                try
                {
                    var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        JsonConvert.SerializeObject(item.Object));

                    var startMinutes = Convert.ToDouble(data["StartTime"]);
                    var endMinutes = Convert.ToDouble(data["EndTime"]);
                    var isAvailable = data.ContainsKey("IsAvailable") ? Convert.ToBoolean(data["IsAvailable"]) : true;

                    var timeSlot = new TimeSlot
                    {
                        Id = item.Key,
                        StartTime = TimeSpan.FromMinutes(startMinutes),
                        EndTime = TimeSpan.FromMinutes(endMinutes),
                        IsAvailable = isAvailable,
                        AppointmentId = data.ContainsKey("AppointmentId") ? data["AppointmentId"]?.ToString() : null
                    };

                    Debug.WriteLine($"Created TimeSlot: {timeSlot.DisplayTime}, Available: {timeSlot.IsAvailable}");
                    timeSlots.Add(timeSlot);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing time slot: {ex.Message}");
                }
            }

            Debug.WriteLine($"Returning {timeSlots.Count} time slots");
            Debug.WriteLine("=== GetTimeSlotsAsync END ===");
            return timeSlots;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetTimeSlotsAsync ERROR: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private TimeSpan ConvertToTimeSpan(object value)
    {
        if (value == null) return TimeSpan.Zero;

        switch (value)
        {
            case double d:
                return TimeSpan.FromMinutes(d);
            case long l:
                return TimeSpan.FromMinutes(l);
            case int i:
                return TimeSpan.FromMinutes(i);
            case string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out double minutes):
                return TimeSpan.FromMinutes(minutes);
            default:
                Debug.WriteLine($"Invalid time value: {value}");
                return TimeSpan.Zero;
        }
    }


    public async Task<List<TimeSlot>> GetAvailableTimeSlotsAsync(string practitionerId, DateTime date)
    {
        try
        {
            Debug.WriteLine($"Getting available time slots for practitioner: {practitionerId}, date: {date}");
            var dateKey = date.ToString("yyyy-MM-dd");

            var snapshot = await _firebaseClient
                .Child(AVAILABILITY_PATH)
                .Child(practitionerId)  // Use practitionerId in the path
                .Child(dateKey)
                .OnceAsync<dynamic>();

            var timeSlots = new List<TimeSlot>();

            if (snapshot != null)
            {
                foreach (var item in snapshot)
                {
                    var timeSlot = new TimeSlot
                    {
                        Id = item.Key,
                        StartTime = TimeSpan.FromMinutes(Convert.ToDouble(item.Object.StartTime)),
                        EndTime = TimeSpan.FromMinutes(Convert.ToDouble(item.Object.EndTime)),
                        IsAvailable = item.Object.IsAvailable ?? true,
                        AppointmentId = item.Object.AppointmentId
                    };

                    if (timeSlot.IsAvailable)
                    {
                        timeSlots.Add(timeSlot);
                    }
                }
            }

            Debug.WriteLine($"Found {timeSlots.Count} available time slots");
            return timeSlots;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in GetAvailableTimeSlotsAsync: {ex.Message}");
            throw;
        }
    }

    public async Task AddTimeSlotAsync(DateTime date, TimeSlot slot)
    {
        try
        {
            Debug.WriteLine("=== AddTimeSlotAsync START ===");
            var practitionerId = await GetCurrentPractitionerId();
            var dateKey = date.ToString("yyyy-MM-dd");

            Debug.WriteLine($"Adding slot for date: {dateKey}");
            Debug.WriteLine($"PractitionerId: {practitionerId}");
            Debug.WriteLine($"Time slot: {slot.StartTime} - {slot.EndTime}");

            var slotData = new
            {
                StartTime = slot.StartTime.TotalMinutes,
                EndTime = slot.EndTime.TotalMinutes,
                IsAvailable = true,
                AppointmentId = (string)null
            };

            var path = $"{AVAILABILITY_PATH}/{practitionerId}/{dateKey}";
            Debug.WriteLine($"Writing to path: {path}");
            Debug.WriteLine($"Data to write: {JsonConvert.SerializeObject(slotData)}");

            await _firebaseClient
                .Child(AVAILABILITY_PATH)
                .Child(practitionerId)
                .Child(dateKey)
                .PostAsync(JsonConvert.SerializeObject(slotData));

            Debug.WriteLine("=== AddTimeSlotAsync END ===");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AddTimeSlotAsync ERROR: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }



    public async Task RemoveTimeSlotAsync(DateTime date, TimeSlot slot)
    {
        try
        {
            var dateKey = date.ToString("yyyy-MM-dd");
            await _firebaseClient
                .Child(AVAILABILITY_PATH)
                .Child(dateKey)
                .Child(slot.Id)
                .DeleteAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in RemoveTimeSlotAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Appointment>> GetPractitionerAppointmentsAsync()
    {
        try
        {
            var currentUserId = await GetCurrentPractitionerId();
            var snapshot = await _firebaseClient
                .Child(APPOINTMENTS_PATH)
                .OrderBy("PractitionerId")
                .EqualTo(currentUserId)
                .OnceAsync<Appointment>();

            return snapshot?.Select(x =>
            {
                var appointment = x.Object;
                appointment.Id = x.Key;
                return appointment;
            }).ToList() ?? new List<Appointment>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in GetPractitionerAppointmentsAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Appointment>> GetClientAppointmentsAsync(string clientId)
    {
        try
        {
            var snapshot = await _firebaseClient
                .Child(APPOINTMENTS_PATH)
                .OrderBy("ClientId")
                .EqualTo(clientId)
                .OnceAsync<Appointment>();

            return snapshot?.Select(x =>
            {
                var appointment = x.Object;
                appointment.Id = x.Key;
                return appointment;
            }).ToList() ?? new List<Appointment>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in GetClientAppointmentsAsync: {ex.Message}");
            throw;
        }
    }


    public async Task CreateAppointmentAsync(Appointment appointment)
    {
        try
        {
            // Create the appointment
            var result = await _firebaseClient
                .Child(APPOINTMENTS_PATH)
                .PostAsync(JsonConvert.SerializeObject(appointment));

            // Update the time slot availability
            var dateKey = appointment.Date.ToString("yyyy-MM-dd");
            await _firebaseClient
                .Child(AVAILABILITY_PATH)
                .Child(appointment.PractitionerId)  // Use PractitionerId in the path
                .Child(dateKey)
                .Child(appointment.TimeSlot.Id)
                .PutAsync(new
                {
                    StartTime = appointment.TimeSlot.StartTime.TotalMinutes,
                    EndTime = appointment.TimeSlot.EndTime.TotalMinutes,
                    IsAvailable = false,
                    AppointmentId = result.Key
                });

            Debug.WriteLine($"Appointment created successfully with ID: {result.Key}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in CreateAppointmentAsync: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateAppointmentAsync(Appointment appointment)
    {
        try
        {
            await _firebaseClient
                .Child(APPOINTMENTS_PATH)
                .Child(appointment.Id)
                .PutAsync(appointment);

            // If appointment is cancelled, make the time slot available again
            if (appointment.Status == AppointmentStatus.Cancelled)
            {
                var dateKey = appointment.Date.ToString("yyyy-MM-dd");
                await _firebaseClient
                    .Child(AVAILABILITY_PATH)
                    .Child(dateKey)
                    .Child(appointment.TimeSlot.Id)
                    .Child("IsAvailable")
                    .PutAsync(true);

                await _firebaseClient
                    .Child(AVAILABILITY_PATH)
                    .Child(dateKey)
                    .Child(appointment.TimeSlot.Id)
                    .Child("AppointmentId")
                    .DeleteAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in UpdateAppointmentAsync: {ex.Message}");
            throw;
        }
    }


    // Additional helper methods


    private string GenerateDateKey(DateTime date)
    {
        return date.ToString("yyyy-MM-dd");
    }

    private async Task<bool> IsTimeSlotAvailable(string dateKey, string timeSlotId)
    {
        var snapshot = await _firebaseClient
            .Child(AVAILABILITY_PATH)
            .Child(dateKey)
            .Child(timeSlotId)
            .Child("IsAvailable")
            .OnceSingleAsync<bool>();

        return snapshot;
    }

    private async Task<string> GetCurrentPractitionerId()
    {
        try
        {
            var authProvider = Application.Current.Handler.MauiContext.Services.GetService<IFirebaseAuthService>();
            if (authProvider == null)
                throw new InvalidOperationException("Firebase auth service not initialized");

            var userId = await authProvider.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("No authenticated user found");

            return userId;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetCurrentPractitionerId Error: {ex.Message}");
            throw;
        }
    }

    Task<string> IFirebaseDataService.GetFirebaseAuthToken()
    {
        throw new NotImplementedException();
    }

    #endregion

    #region User Profile Management


    public async Task<string> CreateJournalEntryAsync(string userId, JournalEntry entry)
    {
        try
        {
            var response = await _firebaseClient
                .Child("journal_entries")
                .Child(userId)
                .PostAsync(new
                {
                    type = entry.Type.ToString(),
                    content = entry.Content,
                    timestamp = entry.Timestamp.ToString("o")
                });

            return response.Key;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Firebase save error: {ex}");
            throw;
        }
    }

    public async Task<List<JournalEntry>> GetUserJournalEntriesAsync(string userId)
    {
        try
        {
            var journalReference = _firebaseClient
                .Child("journal_entries")
                .Child(userId);

            var entries = await journalReference.OnceAsync<object>();

            var journalEntries = new List<JournalEntry>();
            foreach (var entry in entries)
            {
                var data = entry.Object as IDictionary<string, object>;
                if (data != null)
                {
                    journalEntries.Add(new JournalEntry
                    {
                        Id = entry.Key,
                        Type = Enum.Parse<JournalEntryType>(data["Type"].ToString()),
                        Content = data["Content"].ToString(),
                        Timestamp = DateTime.Parse(data["Timestamp"].ToString())
                    });
                }
            }

            return journalEntries.OrderByDescending(e => e.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting journal entries: {ex.Message}");
            throw;
        }
    }

    public async Task<string> UploadJournalAudioAsync(string userId, string fileName, Stream audioStream)
    {
        try
        {
            // Convert audio stream to base64 string
            using var memoryStream = new MemoryStream();
            await audioStream.CopyToAsync(memoryStream);
            var audioBase64 = Convert.ToBase64String(memoryStream.ToArray());

            // Save to Firebase Realtime Database
            var audioData = new
            {
                fileName = fileName,
                audioData = audioBase64,
                uploadTime = DateTime.UtcNow.ToString("o")
            };

            var response = await _firebaseClient
                .Child("journal_audio")
                .Child(userId)
                .PostAsync(audioData);

            return response.Key;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Upload audio error: {ex}");
            throw;
        }
    }

    public async Task UpdateJournalEntryAsync(string userId, string entryId, JournalEntry entry)
    {
        try
        {
            await _firebaseClient
                .Child("journal_entries")
                .Child(userId)
                .Child(entryId)
                .PutAsync(new
                {
                    Type = entry.Type.ToString(),
                    Content = entry.Content,
                    Timestamp = entry.Timestamp.ToString("o"),
                    UserId = userId
                });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating journal entry: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteJournalEntryAsync(string userId, string entryId)
    {
        try
        {
            await _firebaseClient
                .Child("journal_entries")
                .Child(userId)
                .Child(entryId)
                .DeleteAsync();

            // If it's an audio entry, also delete the audio file
            if (await CheckIfAudioExists(userId, entryId))
            {
                var storage = new FirebaseStorage(_storageBucket);
                await storage
                    .Child("journal_audio")
                    .Child(userId)
                    .Child($"{entryId}.m4a")
                    .DeleteAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting journal entry: {ex.Message}");
            throw;
        }
    }

    private async Task<bool> CheckIfAudioExists(string userId, string entryId)
    {
        try
        {
            var entry = await _firebaseClient
                .Child("journal_entries")
                .Child(userId)
                .Child(entryId)
                .OnceSingleAsync<JournalEntry>();

            return entry?.Type == JournalEntryType.Audio;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Stream> GetAudioStreamAsync(string audioUrl)
    {
        try
        {
            var httpClient = new HttpClient();
            return await httpClient.GetStreamAsync(audioUrl);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting audio stream: {ex.Message}");
            throw;
        }
    }
}





#endregion
