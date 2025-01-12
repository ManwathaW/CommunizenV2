using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using Firebase.Database;
using Firebase.Database.Query;
using FirebaseAdmin.Auth;
using Firebase.Auth;
using Newtonsoft.Json;
using System.Diagnostics;

public class FirebaseDataService : IFirebaseDataService
{
    private readonly FirebaseClient _firebaseClient;
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

    public async Task<Availability> GetPractitionerAvailabilityAsync(string practitionerId)
    {
        try
        {
            var availability = await _firebaseClient
                .Child("availability")
                .Child(practitionerId)
                .OnceSingleAsync<Availability>();

            if (availability == null)
            {
                // Return default availability if none is set
                return new Availability
                {
                    PractitionerId = practitionerId,
                    AvailableDays = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
                    TimeSlots = new List<string>
                {
                    "09:00 - 10:00",
                    "10:00 - 11:00",
                    "11:00 - 12:00",
                    "14:00 - 15:00",
                    "15:00 - 16:00",
                    "16:00 - 17:00"
                }
                };
            }

            return availability;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting practitioner availability: {ex.Message}");
            throw new Exception($"Failed to get practitioner availability: {ex.Message}");
        }
    }

    public async Task UpdateAppointmentStatusAsync(string appointmentId, AppointmentStatus status)
    {
        try
        {
            var appointment = await _firebaseClient
                .Child("appointments")
                .Child(appointmentId)
                .OnceSingleAsync<Appointment>();

            if (appointment != null)
            {
                appointment.Status = status;

                // Update in practitioner's appointments
                await _firebaseClient
                    .Child("appointments")
                    .Child(appointment.PractitionerId)
                    .Child(appointmentId)
                    .PutAsync(JsonConvert.SerializeObject(appointment));

                // Update in user's appointments
                await _firebaseClient
                    .Child("user_appointments")
                    .Child(appointment.UserId)
                    .Child(appointmentId)
                    .PutAsync(JsonConvert.SerializeObject(appointment));
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to update appointment status: {ex.Message}");
        }
    }

    // method to get appointments
    public async Task<List<Appointment>> GetAppointmentsAsync(string practitionerId)
    {
        try
        {
            var appointments = await _firebaseClient
                .Child("appointments")
                .Child(practitionerId)
                .OnceAsync<Appointment>();

            return appointments?.Select(a =>
            {
                a.Object.Id = a.Key;
                return a.Object;
            }).ToList() ?? new List<Appointment>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting appointments: {ex.Message}");
            throw new Exception($"Failed to get appointments: {ex.Message}");
        }
    }

    public async Task SaveAvailabilityAsync(string practitionerId, Availability availability)
    {
        try
        {
            await _firebaseClient
                .Child("availability")
                .Child(practitionerId)
                .PutAsync(JsonConvert.SerializeObject(availability));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving availability: {ex.Message}");
            throw new Exception($"Failed to save availability: {ex.Message}");
        }
    }


    public async Task SaveAppointmentAsync(Appointment appointment)
    {
        try
        {
            if (string.IsNullOrEmpty(appointment.Id))
            {
                appointment.Id = Guid.NewGuid().ToString();
            }

            appointment.CreatedAt = DateTime.UtcNow;
            await _firebaseClient
                .Child("appointments")
                .Child(appointment.PractitionerId)
                .Child(appointment.Id)
                .PutAsync(JsonConvert.SerializeObject(appointment));

            // Also save to user's appointments
            await _firebaseClient
                .Child("user_appointments")
                .Child(appointment.UserId)
                .Child(appointment.Id)
                .PutAsync(JsonConvert.SerializeObject(appointment));
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to save appointment: {ex.Message}");
        }
    }



    public async Task<List<Appointment>> GetUserAppointmentsAsync(string userId)
    {
        try
        {
            var appointments = await _firebaseClient
                .Child("user_appointments")
                .Child(userId)
                .OnceAsync<Appointment>();

            return appointments?.Select(a => {
                a.Object.Id = a.Key;
                return a.Object;
            }).ToList() ?? new List<Appointment>();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get user appointments: {ex.Message}");
        }
    }

    public async Task<List<Appointment>> GetPractitionerAppointmentsAsync(string practitionerId)
    {
        try
        {
            var appointments = await _firebaseClient
                .Child("appointments")
                .Child(practitionerId)
                .OnceAsync<Appointment>();

            return appointments?.Select(a => {
                a.Object.Id = a.Key;
                return a.Object;
            }).ToList() ?? new List<Appointment>();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get practitioner appointments: {ex.Message}");
        }
    }

  

    public async Task<bool> IsTimeSlotAvailableAsync(string practitionerId, DateTime date, string timeSlot)
    {
        try
        {
            var appointments = await _firebaseClient
                .Child("appointments")
                .Child(practitionerId)
                .OrderBy("Date")
                .EqualTo(date.ToString("o"))
                .OnceAsync<Appointment>();

            var conflictingAppointment = appointments?.FirstOrDefault(a =>
                a.Object.TimeSlot == timeSlot &&
                a.Object.Status != AppointmentStatus.Cancelled &&
                a.Object.Status != AppointmentStatus.Rejected);

            return conflictingAppointment == null;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to check time slot availability: {ex.Message}");
        }
    }

    public async Task<List<Appointment>> GetAllPractitionerAppointmentsAsync(string practitionerId)
    {
        try
        {
            var appointments = await _firebaseClient
                .Child("appointments")
                .Child(practitionerId)
                .OnceAsync<Appointment>();

            return appointments?.Select(a =>
            {
                a.Object.Id = a.Key;
                return a.Object;
            }).ToList() ?? new List<Appointment>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting appointments: {ex.Message}");
            throw new Exception($"Failed to get appointments: {ex.Message}");
        }
    }

    public async Task DeleteAppointmentAsync(string appointmentId, string practitionerId, string userId)
    {
        try
        {
            // Delete from practitioner's appointments
            await _firebaseClient
                .Child("appointments")
                .Child(practitionerId)
                .Child(appointmentId)
                .DeleteAsync();

            // Delete from user's appointments
            await _firebaseClient
                .Child("user_appointments")
                .Child(userId)
                .Child(appointmentId)
                .DeleteAsync();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to delete appointment: {ex.Message}");
        }
    }

    #endregion

}