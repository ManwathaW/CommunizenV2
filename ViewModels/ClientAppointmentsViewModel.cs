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
        private string _practitionerId;
        private string _clientId;

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
            if (query.TryGetValue("PractitionerId", out var practitionerId))
            {
                _practitionerId = practitionerId.ToString();
            }
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var authService = Application.Current.Handler.MauiContext.Services.GetService<IFirebaseAuthService>();
                _clientId = await authService.GetCurrentUserIdAsync();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeAsync: {ex.Message}");
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
            if (string.IsNullOrEmpty(_practitionerId)) return;

            try
            {
                IsLoading = true;
                var slots = await _dataService.GetAvailableTimeSlotsAsync(_practitionerId, SelectedDate);

                AvailableTimeSlots.Clear();
                foreach (var slot in slots.Where(s => s.IsAvailable))
                {
                    AvailableTimeSlots.Add(slot);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading time slots: {ex}");
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
                if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_practitionerId))
                {
                    await Shell.Current.DisplayAlert("Error", "Unable to book appointment", "OK");
                    return;
                }

                var appointment = new Appointment
                {
                    PractitionerId = _practitionerId,
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
                Debug.WriteLine($"Error booking appointment: {ex}");
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
