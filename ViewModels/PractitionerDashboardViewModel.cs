using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;


namespace CommuniZEN.ViewModels
{
    public partial class PractitionerDashboardViewModel : ObservableObject, IQueryAttributable
    {
        #region Services
        private readonly IFirebaseDataService _dataService;
        private readonly IFirebaseAuthService _authService;
        #endregion



        #region Profile Properties
        [ObservableProperty]
        private PracticeProfile profile;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string specialization;

        [ObservableProperty]
        private string bio;

        [ObservableProperty]
        private string profileImage = "profile_placeholder.png";
        #endregion

        #region Location Properties
        [ObservableProperty]
        private string location;

        [ObservableProperty]
        private double latitude;

        [ObservableProperty]
        private double longitude;
        #endregion

        #region UI State Properties
        [ObservableProperty]
        private bool isEditing;

        [ObservableProperty]
        private bool hasProfile;
        #endregion

      
        

    

        #region Constructor
        public PractitionerDashboardViewModel(IFirebaseDataService dataService, IFirebaseAuthService authService)
        {
            _dataService = dataService;
            _authService = authService;
            Profile = new PracticeProfile();
    
          
            _ = LoadProfileAsync();
        }

      

   

        #endregion

      

        #region Profile Management
        private async Task LoadProfileAsync()
        {
            try
            {
                var userId = await _authService.GetCurrentUserIdAsync();
                var profiles = await _dataService.GetPractitionerProfilesAsync(userId);
                var existingProfile = profiles.FirstOrDefault();

                if (existingProfile != null)
                {
                    Profile = existingProfile;
                    SetProfileProperties(existingProfile);
                    
                }
                else
                {
                    ResetProfile();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading profile: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to load profile", "OK");
            }
        }

        private void SetProfileProperties(PracticeProfile profile)
        {
            Name = profile.Name;
            Specialization = profile.Specialization;
            Bio = profile.Bio;
            Location = profile.Location;
            Latitude = profile.Latitude;
            Longitude = profile.Longitude;
            ProfileImage = profile.ProfileImage ?? "profile_placeholder.png";
            HasProfile = true;
            IsEditing = false;
        }

        private void ResetProfile()
        {
            HasProfile = false;
            IsEditing = true;
           
           
        }
        #endregion



        // New commands for the added functionality
        [RelayCommand]
        private async Task OpenChatWithClients()
        {
            try
            {
                await Shell.Current.GoToAsync("chatpage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Unable to open chat at this time.", "OK");
            }
        }

        [RelayCommand]
        private async Task OpenSessionNotes()
        {
            try
            {
                await Shell.Current.GoToAsync("journalpage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Unable to open session notes at this time.", "OK");
            }
        }

        [RelayCommand]
        private async Task OpenAnalytics()
        {
            await Shell.Current.DisplayAlert("Client Analytics", "Analytics feature coming soon", "OK");
        }

        [RelayCommand]
        private async Task OpenResources()
        {
            await Shell.Current.DisplayAlert("Resources", "Resources feature coming soon", "OK");
        }



        #region Commands





        [RelayCommand]
        private async Task SaveProfile()
        {
            try
            {
                var userId = await _authService.GetCurrentUserIdAsync();

                if (Profile == null)
                {
                    Profile = new PracticeProfile();
                }

                UpdateProfileData(userId);
                await _dataService.SavePracticeProfileAsync(userId, Profile);
                

                HasProfile = true;
                IsEditing = false;
                await Shell.Current.DisplayAlert("Success", "Profile and availability saved successfully", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving profile: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private void UpdateProfileData(string userId)
        {
            Profile.UserId = userId;
            Profile.Name = Name;
            Profile.Specialization = Specialization;
            Profile.Bio = Bio;
            Profile.ProfileImage = ProfileImage;
            Profile.Location = Location;
            Profile.Latitude = Latitude;
            Profile.Longitude = Longitude;
            Profile.CreatedAt = DateTime.UtcNow;

            if (string.IsNullOrEmpty(Profile.Id))
            {
                Profile.Id = Guid.NewGuid().ToString();
            }
        }

        [RelayCommand]
        private void EditProfile()
        {
            IsEditing = true;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            if (HasProfile)
            {
                IsEditing = false;
                SetProfileProperties(Profile);
            }
        }

        [RelayCommand]
        private async Task DeleteProfile()
        {
            try
            {
                if (!string.IsNullOrEmpty(Profile?.Id))
                {
                    var userId = await _authService.GetCurrentUserIdAsync();
                    await _dataService.DeletePracticeProfileAsync(userId, Profile.Id);
                    Profile = new PracticeProfile();
                    ResetProfile();
                    await Shell.Current.DisplayAlert("Success", "Profile deleted successfully", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting profile: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task ChangeProfilePicture()
        {
            try
            {
                var options = new MediaPickerOptions
                {
                    Title = "Select Profile Picture"
                };

                var photo = await MediaPicker.PickPhotoAsync(options);
                if (photo != null)
                {
                    using var stream = await photo.OpenReadAsync();
                    var userId = await _authService.GetCurrentUserIdAsync();
                    ProfileImage = await _dataService.UploadProfileImageAsync(userId, stream);

                    if (Profile != null)
                    {
                        Profile.ProfileImage = ProfileImage;
                        await SaveProfile();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error changing profile picture: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to update profile picture: " + ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task ViewAppointments()
        {
            try
            {
                await Shell.Current.GoToAsync("practitionerappointments");
            }
            catch (Exception ex)
            {
                // Add error handling
                Debug.WriteLine($"Navigation error: {ex.Message}");
                // Optionally show an alert to the user
                await Shell.Current.DisplayAlert("Error", "Unable to view appointments at this time.", "OK");
            }
        }
            [RelayCommand]
        private async Task OpenMapPicker()
        {
            await Shell.Current.GoToAsync("mapPicker");
        }
        #endregion

        #region Query Attributes
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Latitude", out var lat) &&
                query.TryGetValue("Longitude", out var lng) &&
                query.TryGetValue("Address", out var addr))
            {
                Latitude = Convert.ToDouble(lat);
                Longitude = Convert.ToDouble(lng);
                Location = addr.ToString();

                if (Profile != null)
                {
                    Profile.Location = Location;
                    Profile.Latitude = Latitude;
                    Profile.Longitude = Longitude;
                }
            }
        }
        #endregion
    }
}
