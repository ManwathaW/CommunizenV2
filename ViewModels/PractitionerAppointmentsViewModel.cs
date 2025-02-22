using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using Firebase.Database;
using Firebase.Database.Query;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

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
                Debug.WriteLine("=== PractitionerAppointmentsViewModel Constructor START ===");

                if (dataService == null) throw new ArgumentNullException(nameof(dataService));
                _dataService = dataService;

                _firebaseClient = new FirebaseClient("https://communizen-c112-default-rtdb.asia-southeast1.firebasedatabase.app/");

                Debug.WriteLine("Initializing...");
                MainThread.BeginInvokeOnMainThread(async () => await InitializeAsync());

                Debug.WriteLine("=== PractitionerAppointmentsViewModel Constructor END ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Constructor Error: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
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
            try
            {
                if (_dataService == null)
                {
                    Debug.WriteLine("ERROR: _dataService is null");
                    return;
                }

                if (EndTime <= StartTime)
                {
                    await Shell.Current.DisplayAlert("Error", "End time must be after start time", "OK");
                    return;
                }

                IsLoading = true;
                Debug.WriteLine($"=== AddTimeSlot START ===");

                var newSlot = new TimeSlot
                {
                    StartTime = StartTime,
                    EndTime = EndTime,
                    IsAvailable = true
                };

                await _dataService.AddTimeSlotAsync(SelectedDate, newSlot);
                await Task.Delay(500); // Short delay for Firebase
                await LoadTimeSlotsAsync();

                StartTime = new TimeSpan(9, 0, 0);
                EndTime = new TimeSpan(10, 0, 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AddTimeSlot Error: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to add time slot", "OK");
            }
            finally
            {
                IsLoading = false;
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
                Debug.WriteLine("=== LoadTimeSlotsAsync START ===");

                var slots = await _dataService.GetTimeSlotsAsync(SelectedDate);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    TimeSlots.Clear();
                    foreach (var slot in slots.OrderBy(s => s.StartTime))
                    {
                        TimeSlots.Add(slot);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadTimeSlotsAsync Error: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to load time slots", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }



        private async Task LoadAppointmentsAsync()
        {
            if (IsLoading) return; // Prevent multiple loads

            try
            {
                IsLoading = true;
                Debug.WriteLine("=== LoadAppointmentsAsync START ===");

                var appointments = await _dataService.GetPractitionerAppointmentsAsync();
                if (appointments == null) appointments = new List<Appointment>();

                // Sort appointments in memory before updating UI
                var sortedAppointments = appointments
                    .OrderByDescending(a => a.Date)
                    .ThenBy(a => a.TimeSlot?.StartTime ?? TimeSpan.Zero)
                    .ToList();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Appointments.Clear();
                    foreach (var app in sortedAppointments)
                    {
                        Appointments.Add(app);
                    }
                });

                Debug.WriteLine($"Loaded {Appointments.Count} appointments");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading appointments: {ex.Message}");
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
                Debug.WriteLine("=== ConfirmAppointment START ===");
                if (appointment == null)
                {
                    Debug.WriteLine("Error: Appointment is null");
                    return;
                }

                Debug.WriteLine($"Confirming appointment: {appointment.Id}");

                bool confirm = await Application.Current.MainPage.DisplayAlert(
                    "Confirm Appointment",
                    "Are you sure you want to confirm this appointment?",
                    "Yes", "No");

                if (!confirm) return;

                IsLoading = true;
                appointment.Status = AppointmentStatus.Confirmed;

                await _dataService.UpdateAppointmentAsync(appointment);
                Debug.WriteLine("Database updated");

                await LoadAppointmentsAsync();
                Debug.WriteLine("Appointments reloaded");

                await Application.Current.MainPage.DisplayAlert("Success",
                    "Appointment confirmed successfully", "OK");

                Debug.WriteLine("=== ConfirmAppointment END ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error confirming appointment: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await Application.Current.MainPage.DisplayAlert("Error",
                    "Failed to confirm appointment", "OK");
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

            bool confirm = await Application.Current.MainPage.DisplayAlert(
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

                await Application.Current.MainPage.DisplayAlert("Success",
                    "Appointment cancelled successfully", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cancelling appointment: {ex}");
                await Application.Current.MainPage.DisplayAlert("Error",
                    "Failed to cancel appointment", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}

