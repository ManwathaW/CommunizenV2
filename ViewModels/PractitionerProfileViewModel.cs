using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;


namespace CommuniZEN.ViewModels
{

    public partial class PractitionerProfileViewModel : ObservableObject, IQueryAttributable
    {
        #region Services
        private readonly IFirebaseDataService _dataService;
        #endregion

        #region Private Fields
        private string _practitionerId;
        private string _practitionerUserId;
        #endregion

        #region Observable Properties
        [ObservableProperty]
        private PracticeProfile practitioner;

        [ObservableProperty]
        private ObservableCollection<string> availableDays;

        [ObservableProperty]
        private ObservableCollection<TimeSlot> timeSlots;

        [ObservableProperty]
        private DateSlot selectedDate;

        [ObservableProperty]
        private TimeSlot selectedTimeSlot;

        [ObservableProperty]
        private bool isLoading;
        #endregion

        #region Constructor
        public PractitionerProfileViewModel(IFirebaseDataService dataService)
        {
            _dataService = dataService;
            InitializeCollections();
        }

        private void InitializeCollections()
        {
            AvailableDays = new ObservableCollection<string>();
            TimeSlots = new ObservableCollection<TimeSlot>();
        }
        #endregion

        #region Loading Methods

        private async Task LoadPractitionerProfileAsync()
        {
            try
            {
                IsLoading = true;

                var profiles = await _dataService.GetPractitionerProfilesAsync(_practitionerUserId);
                Practitioner = profiles.FirstOrDefault(p => p.Id == _practitionerId);

                if (Practitioner == null)
                {
                    await Shell.Current.DisplayAlert("Error", "Practitioner not found", "OK");
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                await LoadAvailabilityAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to load practitioner profile", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadAvailabilityAsync()
        {
            try
            {
                var availability = await _dataService.GetPractitionerAvailabilityAsync(_practitionerId);
                if (availability != null)
                {
                    UpdateAvailableDays(availability.AvailableDays);
                    UpdateTimeSlots(availability.TimeSlots);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading availability: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to load availability", "OK");
            }
        }

        private void UpdateAvailableDays(List<string> days)
        {
            AvailableDays.Clear();
            foreach (var day in days)
            {
                AvailableDays.Add(day);
            }
        }

        private void UpdateTimeSlots(List<string> slots)
        {
            TimeSlots.Clear();
            foreach (var time in slots)
            {
                TimeSlots.Add(new TimeSlot { Time = time });
            }
        }
        #endregion

        #region Commands
        [RelayCommand]
        private async Task BookAppointment()
        {
            if (!ValidateAppointmentSelection())
            {
                await Shell.Current.DisplayAlert("Error", "Please select both date and time", "OK");
                return;
            }

            try
            {
                var userId = await _dataService.GetCurrentUserIdAsync();
                var appointment = CreateAppointment(userId);

                await _dataService.SaveAppointmentAsync(appointment);
                await ShowSuccessAndNavigateBack();
            }
            catch (Exception ex)
            {
                HandleBookingError(ex);
            }
        }

        private bool ValidateAppointmentSelection()
        {
            return SelectedDate != null && SelectedTimeSlot != null;
        }

        private Appointment CreateAppointment(string userId)
        {
            return new Appointment
            {
                UserId = userId,
                PractitionerId = _practitionerId,
                PractitionerUserId = _practitionerUserId,
                Date = SelectedDate.Date,
                TimeSlot = SelectedTimeSlot.Time,
                Status = AppointmentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
        }

        private async Task ShowSuccessAndNavigateBack()
        {
            await Shell.Current.DisplayAlert("Success", "Appointment booked successfully", "OK");
            await Shell.Current.GoToAsync("..");
        }

        private void HandleBookingError(Exception ex)
        {
            Debug.WriteLine($"Error booking appointment: {ex.Message}");
            Shell.Current.DisplayAlert("Error", "Failed to book appointment", "OK");
        }

        [RelayCommand]
        private async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }
        #endregion

        #region Query Attributes
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (ValidateQueryParameters(query))
            {
                _practitionerId = query["PractitionerId"].ToString();
                _practitionerUserId = query["PractitionerUserId"].ToString();
                _ = LoadPractitionerProfileAsync();
            }
        }

        private bool ValidateQueryParameters(IDictionary<string, object> query)
        {
            return query.TryGetValue("PractitionerId", out var id) &&
                   query.TryGetValue("PractitionerUserId", out var userId);
        }
        #endregion
    }

}