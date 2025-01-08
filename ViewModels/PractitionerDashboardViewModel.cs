using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Models;
using CommuniZEN.Interfaces;
using CommuniZEN.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;


namespace CommuniZEN.ViewModels
{
    public partial class PractitionerDashboardViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IFirebaseDataService _dataService;
        private readonly IFirebaseAuthService _authService;

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

        [ObservableProperty]
        private string location;

        [ObservableProperty]
        private double latitude;

        [ObservableProperty]
        private double longitude;

        [ObservableProperty]
        private bool isEditing;

        [ObservableProperty]
        private bool hasProfile;

        public PractitionerDashboardViewModel(IFirebaseDataService dataService, IFirebaseAuthService authService)
        {
            _dataService = dataService;
            _authService = authService;
            Profile = new PracticeProfile();
            _ = LoadProfileAsync();
        }

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
                    Name = Profile.Name;
                    Specialization = Profile.Specialization;
                    Bio = Profile.Bio;
                    Location = Profile.Location;
                    Latitude = Profile.Latitude;
                    Longitude = Profile.Longitude;
                    ProfileImage = Profile.ProfileImage ?? "profile_placeholder.png";
                    HasProfile = true;
                    IsEditing = false;
                }
                else
                {
                    HasProfile = false;
                    IsEditing = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading profile: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to load profile", "OK");
            }
        }

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

                await _dataService.SavePracticeProfileAsync(userId, Profile);
                HasProfile = true;
                IsEditing = false;
                await Shell.Current.DisplayAlert("Success", "Profile saved successfully", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving profile: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
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
                Name = Profile.Name;
                Specialization = Profile.Specialization;
                Bio = Profile.Bio;
                Location = Profile.Location;
                Latitude = Profile.Latitude;
                Longitude = Profile.Longitude;
                ProfileImage = Profile.ProfileImage ?? "profile_placeholder.png";
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
                    Name = string.Empty;
                    Specialization = string.Empty;
                    Bio = string.Empty;
                    Location = string.Empty;
                    Latitude = 0;
                    Longitude = 0;
                    ProfileImage = "profile_placeholder.png";
                    HasProfile = false;
                    IsEditing = true;
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
        private async Task OpenMapPicker()
        {
            await Shell.Current.GoToAsync("mapPicker");
        }
    }
}
