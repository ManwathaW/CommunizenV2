using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Storage;
using FirebaseAdmin.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            Debug.WriteLine("=== GetTimeSlotsAsync START ===");
            var dateKey = date.ToString("yyyy-MM-dd");
            var practitionerId = await GetCurrentPractitionerId();
            Debug.WriteLine($"Fetching from path: availability/{practitionerId}/{dateKey}");

            var timeSlots = new List<TimeSlot>();
            var snapshot = await _firebaseClient
                .Child(AVAILABILITY_PATH)
                .Child(practitionerId)
                .Child(dateKey)
                .OnceAsync<object>();

            if (snapshot == null)
            {
                Debug.WriteLine("No data found");
                return timeSlots;
            }

            foreach (var item in snapshot)
            {
                try
                {
                    Debug.WriteLine($"Raw data for slot {item.Key}: {JsonConvert.SerializeObject(item.Object)}");

                    var jObject = JObject.FromObject(item.Object);

                    double startMinutes = jObject["StartTimeMinutes"].ToObject<double>();
                    double endMinutes = jObject["EndTimeMinutes"].ToObject<double>();
                    bool isAvailable = jObject["IsAvailable"]?.ToObject<bool>() ?? true;
                    string appointmentId = jObject["AppointmentId"]?.ToString();

                    var timeSlot = new TimeSlot
                    {
                        Id = item.Key,
                        StartTimeMinutes = startMinutes,
                        EndTimeMinutes = endMinutes,
                        IsAvailable = isAvailable,
                        AppointmentId = appointmentId
                    };

                    // Changed this line to use DateTime for formatting
                    var startTime = DateTime.Today.Add(timeSlot.StartTime);
                    var endTime = DateTime.Today.Add(timeSlot.EndTime);
                    Debug.WriteLine($"Successfully created slot: {startTime:hh:mm tt} - {endTime:hh:mm tt}");

                    timeSlots.Add(timeSlot);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing slot {item.Key}: {ex.Message}");
                    Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    continue;
                }
            }

            return timeSlots;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetTimeSlotsAsync Error: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }


    public async Task<List<TimeSlot>> GetAvailableTimeSlotsAsync(string practitionerId, DateTime date)
    {
        try
        {
            if (string.IsNullOrEmpty(practitionerId))
            {
                Debug.WriteLine("ERROR: practitionerId is null or empty");
                return new List<TimeSlot>();
            }

            var dateKey = date.ToString("yyyy-MM-dd");
            Debug.WriteLine($"=== GetAvailableTimeSlotsAsync ===");
            Debug.WriteLine($"Using practitionerId: {practitionerId}");
            Debug.WriteLine($"Date Key: {dateKey}");
            Debug.WriteLine($"Full Path: {AVAILABILITY_PATH}/{practitionerId}/{dateKey}");

            // First, verify the path exists
            var pathExists = await _firebaseClient
                .Child(AVAILABILITY_PATH)
                .Child(practitionerId)
                .Child(dateKey)
                .OnceSingleAsync<object>();

            if (pathExists == null)
            {
                Debug.WriteLine($"No data found at path: {AVAILABILITY_PATH}/{practitionerId}/{dateKey}");
                return new List<TimeSlot>();
            }

            Debug.WriteLine("Found data at path, retrieving slots...");

            var snapshot = await _firebaseClient
                .Child(AVAILABILITY_PATH)
                .Child(practitionerId)
                .Child(dateKey)
                .OnceAsync<object>();

            var timeSlots = new List<TimeSlot>();

            if (snapshot != null)
            {
                Debug.WriteLine($"Found {snapshot.Count()} raw slots");
                foreach (var item in snapshot)
                {
                    try
                    {
                        Debug.WriteLine($"Processing slot with key: {item.Key}");
                        Debug.WriteLine($"Raw data: {JsonConvert.SerializeObject(item.Object)}");

                        var jObject = JObject.FromObject(item.Object);

                        if (jObject.TryGetValue("StartTimeMinutes", out var startMinutes) &&
                            jObject.TryGetValue("EndTimeMinutes", out var endMinutes))
                        {
                            bool isAvailable = true;
                            if (jObject.TryGetValue("IsAvailable", out var availableToken))
                            {
                                isAvailable = availableToken.ToObject<bool>();
                            }

                            if (isAvailable)
                            {
                                var timeSlot = new TimeSlot
                                {
                                    Id = item.Key,
                                    StartTimeMinutes = startMinutes.ToObject<double>(),
                                    EndTimeMinutes = endMinutes.ToObject<double>(),
                                    IsAvailable = true
                                };

                                Debug.WriteLine($"Created slot: {timeSlot.DisplayTime}");
                                timeSlots.Add(timeSlot);
                            }
                            else
                            {
                                Debug.WriteLine("Slot is not available, skipping");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Slot missing required time data, skipping");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing individual slot: {ex.Message}");
                        continue;
                    }
                }
            }

            Debug.WriteLine($"Final count of available slots: {timeSlots.Count}");
            return timeSlots.OrderBy(ts => ts.StartTimeMinutes).ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ERROR in GetAvailableTimeSlotsAsync: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
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

            var slotData = new
            {
                StartTimeMinutes = Math.Round(slot.StartTime.TotalMinutes, 2),
                EndTimeMinutes = Math.Round(slot.EndTime.TotalMinutes, 2),
                IsAvailable = true,
                AppointmentId = (string)null
            };

            Debug.WriteLine($"Adding to path: availability/{practitionerId}/{dateKey}");
            Debug.WriteLine($"Data: {JsonConvert.SerializeObject(slotData)}");

            await _firebaseClient
                .Child(AVAILABILITY_PATH)
                .Child(practitionerId)
                .Child(dateKey)
                .PostAsync(slotData);  // Note: Sending the object directly, not serializing it

            Debug.WriteLine("Time slot added successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AddTimeSlotAsync Error: {ex.Message}");
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


    public async Task<List<Appointment>> GetClientAppointmentsAsync(string clientId)
    {
        try
        {
            Debug.WriteLine($"=== GetClientAppointmentsAsync ===");
            Debug.WriteLine($"Loading appointments for client: {clientId}");

            var snapshot = await _firebaseClient
                .Child(APPOINTMENTS_PATH)
                .OrderBy("ClientId")
                .EqualTo(clientId)
                .OnceAsync<Appointment>();

            Debug.WriteLine($"Raw response: {JsonConvert.SerializeObject(snapshot)}");

            var appointments = snapshot?.Select(x =>
            {
                var appointment = x.Object;
                appointment.Id = x.Key;
                Debug.WriteLine($"Processed appointment: {JsonConvert.SerializeObject(appointment)}");
                return appointment;
            }).ToList() ?? new List<Appointment>();

            Debug.WriteLine($"Total appointments found: {appointments.Count}");
            return appointments;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in GetClientAppointmentsAsync: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<List<Appointment>> GetPractitionerAppointmentsAsync()
    {
        try
        {
            var currentUserId = await GetCurrentPractitionerId();
            Debug.WriteLine($"=== GetPractitionerAppointmentsAsync ===");
            Debug.WriteLine($"Loading appointments for practitioner: {currentUserId}");

            // Add a debug query to see what appointments exist
            var allAppointments = await _firebaseClient
                .Child(APPOINTMENTS_PATH)
                .OnceAsync<Appointment>();

            Debug.WriteLine("All appointments in database:");
            foreach (var app in allAppointments)
            {
                Debug.WriteLine($"Found appointment - ID: {app.Key}");
                Debug.WriteLine($"PractitionerId: {app.Object.PractitionerId}");
                Debug.WriteLine($"ClientId: {app.Object.ClientId}");
                Debug.WriteLine($"Date: {app.Object.Date}");
                Debug.WriteLine("---");
            }

            var snapshot = await _firebaseClient
                .Child(APPOINTMENTS_PATH)
                .OrderBy("PractitionerId")
                .EqualTo(currentUserId)
                .OnceAsync<Appointment>();

            var appointments = snapshot?.Select(x =>
            {
                var appointment = x.Object;
                appointment.Id = x.Key;
                Debug.WriteLine($"Processed appointment: {JsonConvert.SerializeObject(appointment)}");
                return appointment;
            }).ToList() ?? new List<Appointment>();

            Debug.WriteLine($"Total appointments found: {appointments.Count}");
            return appointments;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in GetPractitionerAppointmentsAsync: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }



    public async Task CreateAppointmentAsync(Appointment appointment)
    {
        try
        {
            Debug.WriteLine($"Creating appointment for practitioner: {appointment.PractitionerId}");
            Debug.WriteLine($"Date: {appointment.Date:yyyy-MM-dd}, Time: {appointment.TimeSlot.DisplayTime}");

            // Create the appointment
            var result = await _firebaseClient
                .Child(APPOINTMENTS_PATH)
                .PostAsync(JsonConvert.SerializeObject(appointment));

            Debug.WriteLine($"Appointment created with ID: {result.Key}");

            // Update the time slot availability
            var dateKey = appointment.Date.ToString("yyyy-MM-dd");
            var timeSlotData = new
            {
                StartTimeMinutes = appointment.TimeSlot.StartTimeMinutes,
                EndTimeMinutes = appointment.TimeSlot.EndTimeMinutes,
                IsAvailable = false,
                AppointmentId = result.Key
            };

            await _firebaseClient
                .Child(AVAILABILITY_PATH)
                .Child(appointment.PractitionerId)
                .Child(dateKey)
                .Child(appointment.TimeSlot.Id)
                .PutAsync(JsonConvert.SerializeObject(timeSlotData));
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

    private async Task<string> GetCurrentPractitionerId()
    {
        try
        {
            var authProvider = Application.Current.Handler.MauiContext.Services.GetService<IFirebaseAuthService>();
            if (authProvider == null)
                throw new InvalidOperationException("Firebase auth service not initialized");

            var userId = await authProvider.GetCurrentUserIdAsync();
            Debug.WriteLine($"GetCurrentPractitionerId returned: {userId}");

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
