using CommuniZEN.Models;

namespace CommuniZEN.Interfaces
{
    public interface IFirebaseDataService
    {
        Task CreateUserProfileAsync(string userId, RegisterRequest profile);
        Task<string> GetCurrentUserIdAsync();
        Task<UserProfile> GetUserProfileAsync(string userId);
        Task UpdateUserProfileAsync(string userId, UserProfile updatedProfile);
        Task DeleteUserProfileAsync(string userId);
        Task<List<UserProfile>> GetUsersByRoleAsync(UserRole role);
        Task<bool> CheckUserExistsAsync(string userId);
        Task SaveUserSessionAsync(string userId, UserSession session);
        Task<Dictionary<string, object>> GetUserStatisticsAsync(string userId);

        // PracticeProfile methods
        Task<List<PracticeProfile>> GetAllPractitionersAsync();
        Task<string> UploadProfileImageAsync(string userId, Stream imageStream);
        Task<string> GetProfileImageAsync(string userId);
        Task SavePracticeProfileAsync(string userId, PracticeProfile profile);
        Task DeletePracticeProfileAsync(string userId, string profileId);
        Task<List<PracticeProfile>> GetPractitionerProfilesAsync(string userId);
        Task UpdatePracticeProfileAsync(string userId, string profileId, PracticeProfile profile);
        Task<List<PracticeProfile>> SearchPractitionersAsync(string searchQuery);
    }
}