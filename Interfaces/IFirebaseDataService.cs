using CommuniZEN.Models;
using static CommuniZEN.Models.Appointment;

namespace CommuniZEN.Interfaces
{
    public interface IFirebaseDataService
    {
        #region User Profile Management
        Task CreateUserProfileAsync(string userId, RegisterRequest profile);
        Task<string> GetCurrentUserIdAsync();
        Task<UserProfile> GetUserProfileAsync(string userId);
        Task UpdateUserProfileAsync(string userId, UserProfile updatedProfile);
        Task DeleteUserProfileAsync(string userId);
        Task<List<UserProfile>> GetUsersByRoleAsync(UserRole role);
        Task<bool> CheckUserExistsAsync(string userId);
        #endregion

        #region Session and Statistics
        Task SaveUserSessionAsync(string userId, UserSession session);
        Task<Dictionary<string, object>> GetUserStatisticsAsync(string userId);
        #endregion

        #region Practitioner Profile Management
        Task<List<PracticeProfile>> GetAllPractitionersAsync();
        Task SavePracticeProfileAsync(string userId, PracticeProfile profile);
        Task UpdatePracticeProfileAsync(string userId, string profileId, PracticeProfile profile);
        Task DeletePracticeProfileAsync(string userId, string profileId);
        Task<List<PracticeProfile>> GetPractitionerProfilesAsync(string userId);
        Task<List<PracticeProfile>> SearchPractitionersAsync(string searchQuery);
        #endregion

        #region Image Management
        Task<string> UploadProfileImageAsync(string userId, Stream imageStream);
        Task<string> GetProfileImageAsync(string userId);
        #endregion

        #region Availability Management
        Task<Availability> GetPractitionerAvailabilityAsync(string practitionerId);
        Task SaveAvailabilityAsync(string practitionerId, Availability availability);
        #endregion

        #region Appointment Management
        Task SaveAppointmentAsync(Appointment appointment);
        Task<List<Appointment>> GetAppointmentsAsync(string practitionerId);
        Task<List<Appointment>> GetUserAppointmentsAsync(string userId);
        Task<List<Appointment>> GetPractitionerAppointmentsAsync(string practitionerId);
        Task UpdateAppointmentStatusAsync(string appointmentId, AppointmentStatus status);
        Task<bool> IsTimeSlotAvailableAsync(string practitionerId, DateTime date, string timeSlot);
        #endregion
    }
}