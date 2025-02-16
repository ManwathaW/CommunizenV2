using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IMap = Microsoft.Maui.Maps.IMap;
using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using Microsoft.Maui.Maps;
using System.Collections.ObjectModel;
using Microsoft.Maui.Devices.Sensors;
using System.Diagnostics;

namespace CommuniZEN.ViewModels
{

    public partial class BookingsViewModel : ObservableObject
    {
        private readonly IFirebaseDataService _firebaseService;
        private readonly INavigationService _navigationService;
        private readonly IGeolocation _geolocation;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private bool isMapView;

        [ObservableProperty]
        private string searchText;

        [ObservableProperty]
        private PracticeProfile selectedPractitioner;

        [ObservableProperty]
        private MapSpan mapSpan;

        [ObservableProperty]
        private Location currentLocation;

        public ObservableCollection<PracticeProfile> Practitioners { get; } = new();
        public ObservableCollection<PracticeProfile> FilteredPractitioners { get; } = new();

        public BookingsViewModel(
            IFirebaseDataService firebaseService,
            INavigationService navigationService,
            IGeolocation geolocation)
        {
            _firebaseService = firebaseService;
            _navigationService = navigationService;
            _geolocation = geolocation;

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SearchText))
                {
                    FilterPractitioners();
                }
            };

            Task.Run(GetCurrentLocationAsync);
        }

        private async Task GetCurrentLocationAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status == PermissionStatus.Granted)
                {
                    var location = await _geolocation.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Medium,
                        Timeout = TimeSpan.FromSeconds(5)
                    });

                    if (location != null)
                    {
                        CurrentLocation = new Location(location.Latitude, location.Longitude);
                        MapSpan = MapSpan.FromCenterAndRadius(
                            CurrentLocation,
                            Distance.FromKilometers(5));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Location error: {ex}");
            }
        }

        [RelayCommand]
        private async Task LoadPractitioners()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                var practitioners = await _firebaseService.GetAllPractitionersAsync();

                Practitioners.Clear();
                foreach (var practitioner in practitioners)
                {
                    if (practitioner != null)
                    {
                        Practitioners.Add(practitioner);
                        Debug.WriteLine($"Added practitioner: {practitioner.Name}, ID: {practitioner.Id}");
                    }
                }
                FilterPractitioners();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading practitioners: {ex}");
                await Shell.Current.DisplayAlert("Error", "Failed to load practitioners", "OK");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private void ToggleView()
        {
            IsMapView = !IsMapView;
            if (IsMapView)
            {
                SelectedPractitioner = null;
                if (CurrentLocation != null)
                {
                    MapSpan = MapSpan.FromCenterAndRadius(
                        CurrentLocation,
                        Distance.FromKilometers(5));
                }
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            IsRefreshing = true;
            await LoadPractitioners();
            IsRefreshing = false;
        }

        [RelayCommand]
        private void SelectPractitioner(PracticeProfile practitioner)
        {
            if (practitioner != null)
            {
                SelectedPractitioner = practitioner;
                if (practitioner.Latitude != 0 && practitioner.Longitude != 0)
                {
                    var location = new Location(practitioner.Latitude, practitioner.Longitude);
                    MapSpan = MapSpan.FromCenterAndRadius(
                        location,
                        Distance.FromKilometers(1));
                }
            }
        }

        [RelayCommand]
        private async Task ViewPractitionerProfile(PracticeProfile practitioner)
        {
            if (practitioner == null) return;

            Debug.WriteLine($"Navigating to appointments with:");
            Debug.WriteLine($"Practice Profile ID: {practitioner.Id}");
            Debug.WriteLine($"Practitioner User ID: {practitioner.UserId}");

            await _navigationService.NavigateToAsync($"appointments?PractitionerId={practitioner.Id}&UserId={practitioner.UserId}");
        }

        private void FilterPractitioners()
        {
            try
            {
                FilteredPractitioners.Clear();

                var filtered = string.IsNullOrWhiteSpace(SearchText)
                    ? Practitioners
                    : Practitioners.Where(p =>
                        (p.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (p.Specialization?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (p.Location?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));

                foreach (var practitioner in filtered)
                {
                    if (practitioner.Latitude != 0 && practitioner.Longitude != 0)
                    {
                        FilteredPractitioners.Add(practitioner);
                    }
                }

                // Zoom to single search result
                if (IsMapView && filtered.Count() == 1)
                {
                    var practitioner = filtered.First();
                    if (practitioner.Latitude != 0 && practitioner.Longitude != 0)
                    {
                        MapSpan = MapSpan.FromCenterAndRadius(
                            new Location(practitioner.Latitude, practitioner.Longitude),
                            Distance.FromKilometers(1));
                    }
                }

                Debug.WriteLine($"Filtered practitioners count: {FilteredPractitioners.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in FilterPractitioners: {ex}");
            }
        }

        public async Task Initialize()
        {
            await LoadPractitioners();
        }
    }
}

    