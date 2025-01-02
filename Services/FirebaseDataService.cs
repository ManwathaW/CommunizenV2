using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using Firebase.Database;
using Firebase.Database.Query;
using FirebaseAdmin.Auth;
using Firebase.Auth;
using Newtonsoft.Json;

public class FirebaseDataService : IFirebaseDataService
{
    private readonly FirebaseClient _firebaseClient;

    public FirebaseDataService()
    {
        _firebaseClient = new FirebaseClient(
            "https://communizen-c112-default-rtdb.asia-southeast1.firebasedatabase.app/",  // Updated URL
            new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult(FirebaseConfig.ApiKey)
            });
    }

    public async Task CreateUserProfileAsync(string userId, RegisterRequest profile)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be empty");

            // Create user profile
            var userData = new
            {
                Email = profile.Email,
                FullName = profile.FullName,
                PhoneNumber = profile.PhoneNumber,
                Role = (int)profile.Role,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                ProfileImage = "profile_placeholder.png"
            };

            // Save user data
            await _firebaseClient
                .Child("users")
                .Child(userId)
                .PutAsync(JsonConvert.SerializeObject(userData));

            // If practitioner, create practice profile
            if (profile.Role == UserRole.Practitioner)
            {
                var practiceData = new
                {
                    Email = profile.Email,
                    FullName = profile.FullName,
                    PracticeName = $"Dr. {profile.FullName}'s Practice",
                    PhoneNumber = profile.PhoneNumber,
                    Specialization = profile.Specialization ?? "General Practice",
                    LicenseNumber = profile.LicenseNumber,
                    Location = "Not Set",
                    Latitude = 0.0,
                    Longitude = 0.0,
                    ProfileImage = "profile_placeholder.png",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    Id = Guid.NewGuid().ToString()
                };

                await _firebaseClient
                    .Child("practitioners")
                    .Child(userId)
                    .Child(practiceData.Id)
                    .PutAsync(JsonConvert.SerializeObject(practiceData));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating profile: {ex.Message}");
            throw new Exception($"Failed to create user profile: {ex.Message}", ex);
        }
    }

    // ... Rest of your existing methods remain the same ...

public async Task<UserProfile> GetUserProfileAsync(string userId)
    {
        try
        {
            var userProfile = await _firebaseClient
                .Child("users")
                .Child(userId)
                .OnceSingleAsync<UserProfile>();

            if (userProfile == null)
            {
                // Create a default profile if none exists
                userProfile = new UserProfile
                {
                    Id = userId,
                    Email = "", // This will be set from auth
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _firebaseClient
                    .Child("users")
                    .Child(userId)
                    .PutAsync(userProfile);
            }

            return userProfile;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to retrieve or create user profile: {ex.Message}");
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

    public async Task<List<PracticeProfile>> GetAllPractitionersAsync()
    {
        try
        {
            var practitioners = await _firebaseClient
                .Child("practitioners")
                .OnceAsync<PracticeProfile>();

            return practitioners?.Select(p => p.Object).ToList() ?? new List<PracticeProfile>();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get practitioners: {ex.Message}");
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

    public async Task<string> UploadProfileImageAsync(string userId, Stream imageStream)
    {
        try
        {
            var imagePath = $"profile_images/{userId}/{Guid.NewGuid()}.jpg";
            // Note: You'll need to implement the actual image upload to Firebase Storage
            // This is a placeholder that returns a fake URL
            return $"https://firebasestorage.googleapis.com/{imagePath}";
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to upload image: {ex.Message}");
        }
    }

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

    public async Task<List<PracticeProfile>> GetPractitionerProfilesAsync(string userId)
    {
        try
        {
            var profiles = await _firebaseClient
                .Child("practitioners")
                .Child(userId)
                .OnceAsync<PracticeProfile>();

            return profiles?.Select(p => p.Object).ToList() ?? new List<PracticeProfile>();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get practitioner profiles: {ex.Message}");
        }
    }

    
    


}