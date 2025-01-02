using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommuniZEN.Models;
using CommuniZEN.Interfaces;
using CommuniZEN.Services;
using Firebase.Database;


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
        Task<List<PracticeProfile>> GetAllPractitionersAsync();
        Task<List<PracticeProfile>> SearchPractitionersAsync(string searchQuery); // Changed return type
        Task<string> UploadProfileImageAsync(string userId, Stream imageStream);
        Task SavePracticeProfileAsync(string userId, PracticeProfile profile);
        Task DeletePracticeProfileAsync(string userId, string profileId);
        Task<List<PracticeProfile>> GetPractitionerProfilesAsync(string userId);

    }
    }