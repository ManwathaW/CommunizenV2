using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using CommuniZEN.Services;
using Microsoft.Maui.Devices.Sensors;

namespace CommuniZEN.ViewModels
{
    public partial class PractitionerDashboardViewModel : ObservableObject
    {
        private readonly IFirebaseDataService _dataService;
        private readonly IFirebaseAuthService _authService;
        private readonly IGeolocation _geolocation;
        private readonly IGeocoding _geocoding;
        private readonly IMediaPicker _mediaPicker;

        [ObservableProperty]
        private string practitionerName;

        [ObservableProperty]
        private string mainSpecialization;

        [ObservableProperty]
        private ObservableCollection<PracticeProfile> practiceProfiles;

        [ObservableProperty]
        private bool isAddingProfile;

        [ObservableProperty]
        private string newProfileImage = "profile_placeholder.png";

        [ObservableProperty]
        private string newPracticeName;

        [ObservableProperty]
        private string newSpecialization;

        [ObservableProperty]
        private string locationSearch;

        [ObservableProperty]
        private string selectedLocation;

        [ObservableProperty]
        private Location selectedCoordinates;

        private Stream imageStream;

        public PractitionerDashboardViewModel(
            IFirebaseDataService dataService,
            IFirebaseAuthService authService,
            IGeolocation geolocation,
            IGeocoding geocoding,
            IMediaPicker mediaPicker)
        {
            _dataService = dataService;
            _authService = authService;
            _geolocation = geolocation;
            _geocoding = geocoding;
            _mediaPicker = mediaPicker;

            PracticeProfiles = new ObservableCollection<PracticeProfile>();
            LoadProfilesAsync();
        }

        private async void LoadProfilesAsync()
        {
            try
            {
                var userId = await _authService.GetCurrentUserIdAsync();
                var profiles = await _dataService.GetPractitionerProfilesAsync(userId);

                PracticeProfiles.Clear();
                foreach (var profile in profiles)
                {
                    PracticeProfiles.Add(profile);
                }

                // Set main practitioner info
                var mainProfile = profiles.FirstOrDefault();
                if (mainProfile != null)
                {
                    PractitionerName = mainProfile.FullName;
                    MainSpecialization = mainProfile.Specialization;
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private void ShowAddProfile()
        {
            IsAddingProfile = true;
            InitializeLocationAsync();
        }

        [RelayCommand]
        private void CancelAddProfile()
        {
            IsAddingProfile = false;
            ClearNewProfileFields();
        }

        private async void InitializeLocationAsync()
        {
            try
            {
                var location = await _geolocation.GetLastKnownLocationAsync();
                if (location == null)
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Medium);
                    location = await _geolocation.GetLocationAsync(request);
                }

                if (location != null)
                {
                    SelectedCoordinates = location;
                    await UpdateLocationAddressAsync(location);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Unable to get location.", "OK");
            }
        }

        [RelayCommand]
        private async Task PickImage()
        {
            try
            {
                var result = await _mediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Select Profile Photo"
                });

                if (result != null)
                {
                    imageStream = await result.OpenReadAsync();
                    NewProfileImage = result.FullPath;
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Unable to pick image.", "OK");
            }
        }

        [RelayCommand]
        private async Task SearchLocation()
        {
            if (string.IsNullOrWhiteSpace(LocationSearch)) return;

            try
            {
                var locations = await _geocoding.GetLocationsAsync(LocationSearch);
                var location = locations?.FirstOrDefault();

                if (location != null)
                {
                    SelectedCoordinates = location;
                    await UpdateLocationAddressAsync(location);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Location search failed.", "OK");
            }
        }

        private async Task UpdateLocationAddressAsync(Location location)
        {
            try
            {
                var placemarks = await _geocoding.GetPlacemarksAsync(location);
                var placemark = placemarks?.FirstOrDefault();

                if (placemark != null)
                {
                    SelectedLocation = $"{placemark.Thoroughfare} {placemark.SubThoroughfare}, " +
                                     $"{placemark.Locality}, {placemark.AdminArea}";
                }
            }
            catch (Exception)
            {
                SelectedLocation = $"({location.Latitude:F6}, {location.Longitude:F6})";
            }
        }

        [RelayCommand]
        private async Task SaveProfile()
        {
            if (string.IsNullOrWhiteSpace(NewPracticeName) ||
                string.IsNullOrWhiteSpace(NewSpecialization) ||
                SelectedCoordinates == null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Please fill all required fields.", "OK");
                return;
            }

            try
            {
                var userId = await _authService.GetCurrentUserIdAsync();
                string imageUrl = null;

                if (imageStream != null)
                {
                    imageUrl = await _dataService.UploadProfileImageAsync(userId, imageStream);
                }

                var profile = new PracticeProfile
                {
                    Id = Guid.NewGuid().ToString(),
                    PracticeName = NewPracticeName,
                    Specialization = NewSpecialization,
                    Location = SelectedLocation,
                    Latitude = SelectedCoordinates.Latitude,
                    Longitude = SelectedCoordinates.Longitude,
                    ProfileImage = imageUrl ?? NewProfileImage,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _dataService.SavePracticeProfileAsync(userId, profile);
                PracticeProfiles.Add(profile);

                IsAddingProfile = false;
                ClearNewProfileFields();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to save profile.", "OK");
            }
        }

        [RelayCommand]
        private async Task DeleteProfile(PracticeProfile profile)
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Confirm Delete",
                "Are you sure you want to delete this practice profile?",
                "Yes", "No");

            if (confirm)
            {
                try
                {
                    var userId = await _authService.GetCurrentUserIdAsync();
                    await _dataService.DeletePracticeProfileAsync(userId, profile.Id);
                    PracticeProfiles.Remove(profile);
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
                }
            }
        }

        private void ClearNewProfileFields()
        {
            NewPracticeName = string.Empty;
            NewSpecialization = string.Empty;
            NewProfileImage = "profile_placeholder.png";
            LocationSearch = string.Empty;
            SelectedLocation = string.Empty;
            imageStream?.Dispose();
            imageStream = null;
        }
    }


}