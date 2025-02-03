using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using Firebase.Database;
using Firebase.Database.Query;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace CommuniZEN.ViewModels
{

    public partial class PractitionerAppointmentsViewModel : ObservableObject
    {
        private const string AVAILABILITY_PATH = "availability";
        private const string APPOINTMENTS_PATH = "appointments";
        private readonly IFirebaseDataService _dataService;
        private readonly FirebaseClient _firebaseClient;
        private string _practitionerId;

        [ObservableProperty]
        private bool isAvailabilityTabSelected = true;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private DateTime selectedDate = DateTime.Today;

        [ObservableProperty]
        private DateTime minimumDate = DateTime.Today;

        [ObservableProperty]
        private DateTime maximumDate = DateTime.Today.AddDays(30);

        [ObservableProperty]
        private TimeSpan startTime = new(9, 0, 0);

        [ObservableProperty]
        private TimeSpan endTime = new(10, 0, 0);

        [ObservableProperty]
        private ObservableCollection<TimeSlot> timeSlots = new();

        [ObservableProperty]
        private ObservableCollection<Appointment> appointments = new();

        [ObservableProperty]
        private TimeSlot selectedTimeSlot;

        public PractitionerAppointmentsViewModel(IFirebaseDataService dataService)
        {
            try
            {
                if (dataService == null) throw new ArgumentNullException(nameof(dataService));
                _dataService = dataService;
                TimeSlots = new ObservableCollection<TimeSlot>();
                Appointments = new ObservableCollection<Appointment>();
                _firebaseClient = new FirebaseClient("https://communizen-c112-default-rtdb.asia-southeast1.firebasedatabase.app/");
                Task.Run(async () => await InitializeAsync());
            }
           
           catch (Exception ex)
            {
              Debug.WriteLine($"INNER EXCEPTION: {ex.InnerException?.Message}"); // Log the actual error
            }
        }



        private async Task InitializeAsync()
        {
            try
            {
                var authService = Application.Current.Handler.MauiContext.Services.GetService<IFirebaseAuthService>();
                _practitionerId = await authService.GetCurrentUserIdAsync();
                await LoadDataAsync();
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                Debug.WriteLine($"TargetInvocationException: {ex.InnerException?.Message}");
                Debug.WriteLine($"Stack Trace: {ex.InnerException?.StackTrace}");
                await Shell.Current.DisplayAlert("Error", "Failed to initialize", "OK");
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
            IsAvailabilityTabSelected = tabIndex == "0";
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (IsAvailabilityTabSelected)
            {
                await LoadTimeSlotsAsync();
            }
            else
            {
                await LoadAppointmentsAsync();
            }
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            if (IsAvailabilityTabSelected)
            {
                _ = LoadTimeSlotsAsync();
            }
        }

        [RelayCommand]
        private async Task AddTimeSlot()
        {
            if (_dataService == null)
            {
                Debug.WriteLine("Data service is null");
                return;
            }

            if (EndTime <= StartTime)
            {
                await Shell.Current.DisplayAlert("Error", "End time must be after start time", "OK");
                return;
            }

            try
            {
                var newSlot = new TimeSlot
                {
                    Id = Guid.NewGuid().ToString(),
                    StartTime = StartTime,
                    EndTime = EndTime,
                    IsAvailable = true
                };

                await _dataService.AddTimeSlotAsync(SelectedDate, newSlot);
                await LoadTimeSlotsAsync();

                StartTime = new TimeSpan(9, 0, 0);
                EndTime = new TimeSpan(10, 0, 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AddTimeSlot Error: {ex}");
                await Shell.Current.DisplayAlert("Error", "Failed to add time slot", "OK");
            }
        }

        [RelayCommand]
        private async Task RemoveTimeSlot(TimeSlot slot)
        {
            if (slot == null) return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Confirm Removal",
                "Are you sure you want to remove this time slot?",
                "Yes", "No");

            if (!confirm) return;

            try
            {
                await _dataService.RemoveTimeSlotAsync(SelectedDate, slot);
                await LoadTimeSlotsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RemoveTimeSlot Error: {ex}");
                await Shell.Current.DisplayAlert("Error", "Failed to remove time slot", "OK");
            }
        }

        private async Task LoadTimeSlotsAsync()
        {
            try
            {
                IsLoading = true;
                var slots = await _dataService.GetTimeSlotsAsync(SelectedDate);

                TimeSlots.Clear();
                foreach (var slot in slots)
                {
                    Debug.WriteLine($"Adding slot: {slot.StartTime} - {slot.EndTime}");
                    TimeSlots.Add(slot);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading time slots: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to load time slots", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadAppointmentsAsync()
        {
            try
            {
                IsLoading = true;
                var apps = await _dataService.GetPractitionerAppointmentsAsync();

                Appointments.Clear();
                foreach (var app in apps)
                {
                    Appointments.Add(app);
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
        private async Task ConfirmAppointment(Appointment appointment)
        {
            try
            {
                IsLoading = true;
                appointment.Status = AppointmentStatus.Confirmed;
                await _dataService.UpdateAppointmentAsync(appointment);
                await LoadAppointmentsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error confirming appointment: {ex}");
                await Shell.Current.DisplayAlert("Error", "Failed to confirm appointment", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CancelAppointment(Appointment appointment)
        {
            if (appointment == null) return;

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
                await LoadAppointmentsAsync();
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

