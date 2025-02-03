using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace CommuniZEN.ViewModels
{
    public partial class BookingsViewModel : ObservableObject
    {
        private readonly IFirebaseDataService _dataService;

        [ObservableProperty]
        private ObservableCollection<PracticeProfile> practitioners;

        [ObservableProperty]
        private ObservableCollection<PracticeProfile> filteredPractitioners;

        [ObservableProperty]
        private string searchText;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private bool isLoading;

        public BookingsViewModel(IFirebaseDataService dataService)
        {
            Debug.WriteLine("BookingsViewModel constructor called");
            _dataService = dataService;
            Practitioners = new ObservableCollection<PracticeProfile>();
            FilteredPractitioners = new ObservableCollection<PracticeProfile>();
            _ = InitializeAsync();
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterPractitioners();
        }

        private void FilterPractitioners()
        {
            if (Practitioners == null) return;

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredPractitioners = new ObservableCollection<PracticeProfile>(Practitioners);
                return;
            }

            var searchTerms = SearchText.ToLower().Split(' ');
            var filtered = Practitioners.Where(p =>
                searchTerms.All(term =>
                    (p.Name?.ToLower().Contains(term) ?? false) ||
                    (p.Location?.ToLower().Contains(term) ?? false) ||
                    (p.Specialization?.ToLower().Contains(term) ?? false)
                )
            );

            FilteredPractitioners = new ObservableCollection<PracticeProfile>(filtered);
        }

        private async Task InitializeAsync()
        {
            Debug.WriteLine("InitializeAsync called");
            await LoadPractitioners();
        }

        private async Task LoadPractitioners()
        {
            try
            {
                IsLoading = true;
                Debug.WriteLine("Loading practitioners...");
                var allProfiles = await _dataService.GetAllPractitionersAsync();
                Debug.WriteLine($"Retrieved {allProfiles?.Count ?? 0} practitioners");
                Practitioners.Clear();
                FilteredPractitioners.Clear();

                if (allProfiles != null && allProfiles.Any())
                {
                    foreach (var profile in allProfiles)
                    {
                        Practitioners.Add(profile);
                        FilteredPractitioners.Add(profile);
                        Debug.WriteLine($"Added practitioner to collection: {profile.Name}");
                    }
                }
                else
                {
                    Debug.WriteLine("No practitioners found");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading practitioners: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await Shell.Current.DisplayAlert("Error", "Failed to load practitioners", "OK");
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task ViewPractitionerProfile(PracticeProfile practitioner)
        {
            if (practitioner == null) return;
            var parameters = new Dictionary<string, object>
            {
                { "PractitionerId", practitioner.Id },
                { "PractitionerUserId", practitioner.UserId }
            };
            await Shell.Current.GoToAsync("appointments", parameters);
        }

        [RelayCommand]
        private async Task Refresh()
        {
            IsRefreshing = true;
            await LoadPractitioners();
        }
    }
}