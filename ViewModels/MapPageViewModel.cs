using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Controls;
using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using System.Collections.ObjectModel;

namespace CommuniZEN.ViewModels
{
    [QueryProperty(nameof(InitialSearchQuery), "searchQuery")]
    public partial class MapPageViewModel : ObservableObject
    {
        private readonly IFirebaseDataService _dataService;

        [ObservableProperty]
        private string searchQuery;

        [ObservableProperty]
        private bool isMapView = true;

        [ObservableProperty]
        private ObservableCollection<PracticeProfile> practitioners = new();

        [ObservableProperty]
        private ObservableCollection<CustomPin> mapPins = new();

        public MapPageViewModel(IFirebaseDataService dataService)
        {
            _dataService = dataService;
        }

        public string InitialSearchQuery
        {
            set
            {
                SearchQuery = value;
                if (!string.IsNullOrEmpty(value))
                {
                    SearchPractitionersAsync().ConfigureAwait(false);
                }
            }
        }

        [RelayCommand]
        private async Task SearchPractitioners()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery)) return;
            await SearchPractitionersAsync();
        }

        private async Task SearchPractitionersAsync()
        {
            try
            {
                var results = await _dataService.SearchPractitionersAsync(SearchQuery);

                Practitioners.Clear();
                MapPins.Clear();

                foreach (var practitioner in results)
                {
                    Practitioners.Add(practitioner);
                    MapPins.Add(new CustomPin
                    {
                        Id = practitioner.Id,
                        Name = practitioner.PracticeName,
                        Location = new Location(practitioner.Latitude, practitioner.Longitude),
                        Specialization = practitioner.Specialization
                    });
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to search practitioners", "OK");
            }
        }

        [RelayCommand]
        private void ToggleView()
        {
            IsMapView = !IsMapView;
        }

        [RelayCommand]
        private async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
