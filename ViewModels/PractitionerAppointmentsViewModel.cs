using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace CommuniZEN.ViewModels
{
    public partial class PractitionerAppointmentsViewModel : ObservableObject
    {
        private readonly IFirebaseDataService _dataService;
        private readonly IFirebaseAuthService _authService;

        #region Observable Properties

        [ObservableProperty]
        private ObservableCollection<TimeSlot> timeSlots;




        [ObservableProperty]
        private ObservableCollection<Appointment> pendingAppointments;

        [ObservableProperty]
        private ObservableCollection<Appointment> upcomingAppointments;

        [ObservableProperty]
        private ObservableCollection<Appointment> pastAppointments;

        [ObservableProperty]
        private ObservableCollection<AppointmentDate> availableDates;

        [ObservableProperty]
        private AppointmentDate selectedDate;

        [ObservableProperty]
        private TimeSlot selectedTimeSlot;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string statusMessage;

        [ObservableProperty]
        private bool isDateSelected;

        [ObservableProperty]
        private string practitionerId;
        #endregion

        #region Constants
        private List<TimeSlot> DefaultTimeSlots => new List<string>
{
    "09:00", "09:30", "10:00", "10:30", "11:00", "11:30",
    "13:00", "13:30", "14:00", "14:30", "15:00", "15:30",
    "16:00", "16:30", "17:00", "17:30"
}.Select(time => new TimeSlot
{
    Time = time,
    IsAvailable = true,
    IsSelected = false
}).ToList();
        #endregion

        public PractitionerAppointmentsViewModel(IFirebaseDataService dataService, IFirebaseAuthService authService)
        {
            _dataService = dataService;
            _authService = authService;
            InitializeCollections();
            IsDateSelected = false;
            _ = LoadInitialDataAsync();
        }

        private void InitializeCollections()
        {
            PendingAppointments = new ObservableCollection<Appointment>();
            UpcomingAppointments = new ObservableCollection<Appointment>();
            PastAppointments = new ObservableCollection<Appointment>();
            AvailableDates = new ObservableCollection<AppointmentDate>();
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading data...";

                var userId = await _authService.GetCurrentUserIdAsync();
                PractitionerId = userId;

                await Task.WhenAll(
                    LoadAppointmentsAsync(),
                    LoadAvailabilityAsync()
                );
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        #region Availability Management

        private async Task LoadAvailabilityAsync()
        {
            try
            {
                Debug.WriteLine("Starting LoadAvailabilityAsync");
                var availability = await _dataService.GetPractitionerAvailabilityAsync(PractitionerId);
                Debug.WriteLine($"Loaded availability for practitioner: {PractitionerId}");

                GenerateAvailableDates(availability);
                Debug.WriteLine($"Generated {AvailableDates.Count} available dates");

                await CheckExistingAppointments();
                Debug.WriteLine("Finished checking existing appointments");

                // Force UI update after loading
                OnPropertyChanged(nameof(AvailableDates));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading availability: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to load availability", "OK");
            }
        }

        private void GenerateAvailableDates(Availability availability)
        {
            AvailableDates.Clear();
            var today = DateTime.Today;

            for (int i = 0; i < 30; i++)
            {
                var date = today.AddDays(i);
                var dayName = date.ToString("ddd");

                // Create default time slots
                var newTimeSlots = DefaultTimeSlots.Select(time => new TimeSlot
                {
                    Time = time,
                    IsAvailable = true,
                    IsSelected = false
                }).ToList();

                // If we have existing availability, update the time slots
                if (availability?.DailySchedule != null)
                {
                    var existingSchedule = availability.DailySchedule.FirstOrDefault(d => d.Date.Date == date.Date);
                    if (existingSchedule != null)
                    {
                        newTimeSlots = existingSchedule.TimeSlots;
                    }
                }

                AvailableDates.Add(new AppointmentDate
                {
                    Date = date,
                    TimeSlots = newTimeSlots,
                    IsAvailable = true,
                    IsSelected = false
                });
            }
        }


        private async Task CheckExistingAppointments()
        {
            var appointments = await _dataService.GetPractitionerAppointmentsAsync(PractitionerId);

            foreach (var date in AvailableDates)
            {
                var dateAppointments = appointments.Where(a =>
                    a.Date.Date == date.Date.Date &&
                    (a.Status == AppointmentStatus.Approved || a.Status == AppointmentStatus.Pending));

                foreach (var appointment in dateAppointments)
                {
                    var timeSlot = date.TimeSlots.FirstOrDefault(t => t.Time == appointment.TimeSlot);
                    if (timeSlot != null)
                    {
                        timeSlot.IsAvailable = false;
                    }
                }
            }
        }

        [RelayCommand]
        private async Task UpdateAvailability()
        {
            if (SelectedDate == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Updating availability...";

                var availability = await _dataService.GetPractitionerAvailabilityAsync(PractitionerId);
                if (availability == null)
                {
                    availability = new Availability
                    {
                        Id = Guid.NewGuid().ToString(),
                        PractitionerId = PractitionerId,
                        AvailableDays = new List<string>(),
                        TimeSlots = DefaultTimeSlots.Select(time => new TimeSlot
                        {
                            Time = time,
                            IsAvailable = true,
                            IsSelected = false
                        }).ToList(),
                        DailySchedule = new List<DayAvailability>()
                    };
                }

                var dayName = SelectedDate.Date.ToString("ddd");
                if (!availability.AvailableDays.Contains(dayName))
                {
                    availability.AvailableDays.Add(dayName);
                }

                // Update or add daily schedule
                var daySchedule = availability.DailySchedule.FirstOrDefault(d => d.Date.Date == SelectedDate.Date.Date);
                if (daySchedule == null)
                {
                    daySchedule = new DayAvailability
                    {
                        Date = SelectedDate.Date,
                        TimeSlots = TimeSlots.ToList()  // Convert ObservableCollection to List
                    };
                    availability.DailySchedule.Add(daySchedule);
                }
                else
                {
                    daySchedule.TimeSlots = TimeSlots.ToList();  // Convert ObservableCollection to List
                }

                // Update default time slots if not set
                if (!availability.TimeSlots.Any())
                {
                    availability.TimeSlots = DefaultTimeSlots.Select(time => new TimeSlot
                    {
                        Time = time,
                        IsAvailable = true,
                        IsSelected = false
                    }).ToList();
                }

                await _dataService.SaveAvailabilityAsync(PractitionerId, availability);
                await LoadAvailabilityAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to update availability", "OK");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        [RelayCommand]
        private void SelectDate(AppointmentDate date)
        {
            try
            {
                if (date == null) return;

                Debug.WriteLine($"Selecting date: {date.Date}");

                // Clear previous selection
                foreach (var d in AvailableDates)
                {
                    d.IsSelected = false;
                }

                // Set new selection
                date.IsSelected = true;

                // If no time slots exist, initialize them
                if (date.TimeSlots == null || !date.TimeSlots.Any())
                {
                    date.TimeSlots = DefaultTimeSlots.Select(time => new TimeSlot
                    {
                        Time = time,
                        IsAvailable = true,
                        IsSelected = false
                    }).ToList();
                }

                Debug.WriteLine($"Number of time slots: {date.TimeSlots.Count}");

                // Update the observable collection
                TimeSlots.Clear();
                foreach (var slot in date.TimeSlots)
                {
                    TimeSlots.Add(slot);
                }

                SelectedDate = date;
                IsDateSelected = true;

                // Force UI updates
                OnPropertyChanged(nameof(TimeSlots));
                OnPropertyChanged(nameof(IsDateSelected));
                OnPropertyChanged(nameof(SelectedDate));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SelectDate: {ex.Message}");
            }
        }

        partial void OnSelectedDateChanged(AppointmentDate value)
        {
            if (value?.TimeSlots != null)
            {
                TimeSlots = new ObservableCollection<TimeSlot>(value.TimeSlots);
            }
            IsDateSelected = value != null;
        }

        [RelayCommand]
        private void ToggleTimeSlot(TimeSlot timeSlot)
        {
            if (timeSlot == null) return;

            timeSlot.IsAvailable = !timeSlot.IsAvailable;

            // Update both collections
            var index = SelectedDate.TimeSlots.IndexOf(
                SelectedDate.TimeSlots.First(t => t.Time == timeSlot.Time));
            SelectedDate.TimeSlots[index] = timeSlot;

            // Force UI updates
            OnPropertyChanged(nameof(TimeSlots));
            OnPropertyChanged(nameof(SelectedDate));
        }


        #endregion

        #region Appointment Management
        private async Task LoadAppointmentsAsync()
        {
            try
            {
                var appointments = await _dataService.GetPractitionerAppointmentsAsync(PractitionerId);
                UpdateAppointmentCollections(appointments);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to load appointments", "OK");
            }
        }

        private void UpdateAppointmentCollections(List<Appointment> appointments)
        {
            PendingAppointments.Clear();
            UpcomingAppointments.Clear();
            PastAppointments.Clear();

            var now = DateTime.Now;

            foreach (var appointment in appointments.OrderBy(a => a.Date))
            {
                if (appointment.Status == AppointmentStatus.Pending)
                {
                    PendingAppointments.Add(appointment);
                }
                else if (appointment.Date >= now &&
                        appointment.Status != AppointmentStatus.Cancelled)
                {
                    UpcomingAppointments.Add(appointment);
                }
                else
                {
                    PastAppointments.Add(appointment);
                }
            }
        }

        [RelayCommand]
        private async Task ApproveAppointment(Appointment appointment)
        {
            try
            {
                IsLoading = true;
                await _dataService.UpdateAppointmentStatusAsync(appointment.Id, AppointmentStatus.Approved);
                await LoadInitialDataAsync();
                await Shell.Current.DisplayAlert("Success", "Appointment approved", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to approve appointment", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RejectAppointment(Appointment appointment)
        {
            try
            {
                var reason = await Shell.Current.DisplayPromptAsync(
                    "Reject Appointment",
                    "Please provide a reason for rejection:",
                    "Submit",
                    "Cancel");

                if (string.IsNullOrEmpty(reason)) return;

                IsLoading = true;
                appointment.Notes = reason;
                await _dataService.UpdateAppointmentStatusAsync(appointment.Id, AppointmentStatus.Rejected);
                await LoadInitialDataAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to reject appointment", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            await LoadInitialDataAsync();
        }
        #endregion
    }
}