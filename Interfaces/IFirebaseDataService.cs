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
        Task<string> UploadChatImageAsync(string chatId, Stream imageStream);
        #endregion

        #region Availability Management
   
        #endregion

        #region Appointment Management
        Task<List<TimeSlot>> GetTimeSlotsAsync(DateTime date);
        Task<List<TimeSlot>> GetAvailableTimeSlotsAsync(string practitionerId, DateTime date);
        Task AddTimeSlotAsync(DateTime date, TimeSlot slot);
        Task RemoveTimeSlotAsync(DateTime date, TimeSlot slot);
        Task<List<Appointment>> GetPractitionerAppointmentsAsync();
        Task<List<Appointment>> GetClientAppointmentsAsync(string clientId);
        Task<string> GetFirebaseAuthToken();



        Task CreateAppointmentAsync(Appointment appointment);
        Task UpdateAppointmentAsync(Appointment appointment);
        #endregion

        #region Journal Management
        // Creates a new journal entry in Firebase
        Task<string> CreateJournalEntryAsync(string userId, JournalEntry entry);

        // Retrieves all journal entries for a specific user
        Task<List<JournalEntry>> GetUserJournalEntriesAsync(string userId);

        // Updates an existing journal entry
        Task UpdateJournalEntryAsync(string userId, string entryId, JournalEntry entry);

        // Deletes a journal entry
        Task DeleteJournalEntryAsync(string userId, string entryId);

        // Uploads audio file for journal entry
        Task<string> UploadJournalAudioAsync(string userId, string entryId, Stream audioStream);
        #endregion
    }
}