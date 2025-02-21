using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace CommuniZEN.ViewModels
{
    public partial class ClientAppointmentsViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IFirebaseDataService _dataService;
        private string _practitionerUserId; 
        private string _practitionerId;      
        private string _clientId;
        [ObservableProperty]
        private string practitionerName;

        [ObservableProperty]
        private string practitionerSpecialization;

        [ObservableProperty]
        private string practitionerBio;

        [ObservableProperty]
        private string practitionerImage;

        [ObservableProperty]
        private bool isBookingTabSelected = true;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private DateTime selectedDate = DateTime.Today;

        [ObservableProperty]
        private DateTime minimumDate = DateTime.Today;

   

        [ObservableProperty]
        private DateTime maximumDate = DateTime.Today.AddDays(30);

        [ObservableProperty]
        private ObservableCollection<TimeSlot> availableTimeSlots;

        [ObservableProperty]
        private ObservableCollection<Appointment> myAppointments;

        public ClientAppointmentsViewModel(IFirebaseDataService dataService)
        {
            _dataService = dataService;
            AvailableTimeSlots = new ObservableCollection<TimeSlot>();
            MyAppointments = new ObservableCollection<Appointment>();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            Debug.WriteLine("=== ApplyQueryAttributes ===");

            if (query.TryGetValue("UserId", out var userId))
            {
                _practitionerUserId = userId.ToString();
                Debug.WriteLine($"Got practitioner UserId: {_practitionerUserId}");
            }

            if (query.TryGetValue("PractitionerId", out var practitionerId))
            {
                _practitionerId = practitionerId.ToString();
                Debug.WriteLine($"Got practitioner ID: {_practitionerId}");
            }

            if (string.IsNullOrEmpty(_practitionerUserId))
            {
                Debug.WriteLine("ERROR: No practitioner UserId found in query parameters");
                return;
            }

            _ = InitializeAsync();
        }


        private async Task InitializeAsync()
        {
            try
            {
                Debug.WriteLine("=== InitializeAsync ===");
                var authService = Application.Current.Handler.MauiContext.Services.GetService<IFirebaseAuthService>();
                _clientId = await authService.GetCurrentUserIdAsync();
                Debug.WriteLine($"Initialized with ClientId: {_clientId}");
                Debug.WriteLine($"Current PractitionerId: {_practitionerId}");
                // Load practitioner details
                var practitioners = await _dataService.GetAllPractitionersAsync();
                var practitioner = practitioners.FirstOrDefault(p => p.UserId == _practitionerUserId);

                if (practitioner != null)
                {
                    PractitionerName = practitioner.Name;
                    PractitionerSpecialization = practitioner.Specialization;
                    PractitionerBio = practitioner.Bio;
                    PractitionerImage = practitioner.ProfileImage;
                }
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeAsync: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await Shell.Current.DisplayAlert("Error", "Failed to initialize", "OK");
            }
        }

        [RelayCommand]
        private void SwitchTab(string tabIndex)
        {
            IsBookingTabSelected = tabIndex == "0";
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (IsBookingTabSelected)
            {
                await LoadAvailableTimeSlotsAsync();
            }
            else
            {
                await LoadMyAppointmentsAsync();
            }
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            if (IsBookingTabSelected)
            {
                _ = LoadAvailableTimeSlotsAsync();
            }
        }

        private async Task LoadAvailableTimeSlotsAsync()
        {
            if (string.IsNullOrEmpty(_practitionerUserId))
            {
                Debug.WriteLine("ERROR: Cannot load time slots - practitioner UserId is null or empty");
                return;
            }

            try
            {
                IsLoading = true;
                Debug.WriteLine($"=== LoadAvailableTimeSlotsAsync ===");
                Debug.WriteLine($"Loading slots using practitioner UserId: {_practitionerUserId}");
                Debug.WriteLine($"Selected date: {SelectedDate:yyyy-MM-dd}");

                var slots = await _dataService.GetAvailableTimeSlotsAsync(_practitionerUserId, SelectedDate);

                Debug.WriteLine($"Retrieved {slots?.Count ?? 0} slots from service");

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    AvailableTimeSlots.Clear();

                    if (slots != null && slots.Any())
                    {
                        foreach (var slot in slots.OrderBy(s => s.StartTime))
                        {
                            Debug.WriteLine($"Adding slot to UI: {slot.DisplayTime}");
                            AvailableTimeSlots.Add(slot);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("No available time slots found for the selected date");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR loading time slots: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await Shell.Current.DisplayAlert("Error", "Failed to load available time slots", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadMyAppointmentsAsync()
        {
            if (string.IsNullOrEmpty(_clientId)) return;

            try
            {
                IsLoading = true;
                var appointments = await _dataService.GetClientAppointmentsAsync(_clientId);

                MyAppointments.Clear();
                foreach (var appointment in appointments)
                {
                    MyAppointments.Add(appointment);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading appointments: {ex}");
                await Shell.Current.DisplayAlert("Error", "Failed to load appointments", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task BookAppointment(TimeSlot timeSlot)
        {
            try
            {
                if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_practitionerUserId))
                {
                    await Shell.Current.DisplayAlert("Error", "Unable to book appointment", "OK");
                    return;
                }

                bool confirm = await Shell.Current.DisplayAlert(
                    "Confirm Booking",
                    $"Would you like to book an appointment for {SelectedDate.ToShortDateString()} at {timeSlot.DisplayTime}?",
                    "Yes", "No");

                if (!confirm) return;

                var appointment = new Appointment
                {
                    Id = Guid.NewGuid().ToString(),
                    PractitionerId = _practitionerUserId,  // Use Firebase Auth ID instead of practice profile ID
                    ClientId = _clientId,
                    Date = SelectedDate,
                    TimeSlot = timeSlot,
                    Status = AppointmentStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                IsLoading = true;
                await _dataService.CreateAppointmentAsync(appointment);
                await Shell.Current.DisplayAlert("Success", "Appointment booked successfully!", "OK");

                IsBookingTabSelected = false;
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error booking appointment: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to book appointment", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }


        [RelayCommand]
        private async Task CancelAppointment(Appointment appointment)
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Confirm Cancellation",
                "Are you sure you want to cancel this appointment?",
                "Yes", "No");

            if (!confirm) return;

            try
            {
                IsLoading = true;
                appointment.Status = AppointmentStatus.Cancelled;
                await _dataService.UpdateAppointmentAsync(appointment);
                await LoadMyAppointmentsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cancelling appointment: {ex}");
                await Shell.Current.DisplayAlert("Error", "Failed to cancel appointment", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
