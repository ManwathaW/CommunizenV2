using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Models;
using CommuniZEN.Interfaces;
using System.Collections.ObjectModel;
using static CommuniZEN.Models.Appointment;

namespace CommuniZEN.ViewModels
{
    public partial class ClientAppointmentsViewModel : ObservableObject
    {
        private readonly IFirebaseDataService _dataService;
        private readonly IFirebaseAuthService _authService;

        #region Observable Properties
        [ObservableProperty]
        private ObservableCollection<Appointment> upcomingAppointments;

        [ObservableProperty]
        private ObservableCollection<Appointment> pastAppointments;

        [ObservableProperty]
        private ObservableCollection<PracticeProfile> availablePractitioners;

        [ObservableProperty]
        private ObservableCollection<AppointmentDate> availableDates;

        [ObservableProperty]
        private PracticeProfile selectedPractitioner;

        [ObservableProperty]
        private AppointmentDate selectedDate;

        [ObservableProperty]
        private TimeSlot selectedTimeSlot;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool canBook;
        #endregion

        public ClientAppointmentsViewModel(IFirebaseDataService dataService, IFirebaseAuthService authService)
        {
            _dataService = dataService;
            _authService = authService;
            InitializeCollections();
            _ = LoadInitialDataAsync();
        }

        private void InitializeCollections()
        {
            UpcomingAppointments = new ObservableCollection<Appointment>();
            PastAppointments = new ObservableCollection<Appointment>();
            AvailablePractitioners = new ObservableCollection<PracticeProfile>();
            AvailableDates = new ObservableCollection<AppointmentDate>();
        }

        #region Data Loading
        private async Task LoadInitialDataAsync()
        {
            try
            {
                IsLoading = true;
                await Task.WhenAll(
                    LoadAppointmentsAsync(),
                    LoadPractitionersAsync()
                );
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
                var userId = await _authService.GetCurrentUserIdAsync();
                var appointments = await _dataService.GetUserAppointmentsAsync(userId);
                UpdateAppointmentCollections(appointments);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to load appointments", "OK");
            }
        }

        private void UpdateAppointmentCollections(List<Appointment> appointments)
        {
            var now = DateTime.Now;
            UpcomingAppointments.Clear();
            PastAppointments.Clear();

            foreach (var appointment in appointments.OrderBy(a => a.Date))
            {
                if (appointment.Date >= now.Date &&
                    appointment.Status != AppointmentStatus.Cancelled &&
                    appointment.Status != AppointmentStatus.Completed)
                {
                    UpcomingAppointments.Add(appointment);
                }
                else
                {
                    PastAppointments.Add(appointment);
                }
            }
        }

        private async Task LoadPractitionersAsync()
        {
            try
            {
                var practitioners = await _dataService.GetAllPractitionersAsync();
                AvailablePractitioners.Clear();
                foreach (var practitioner in practitioners)
                {
                    AvailablePractitioners.Add(practitioner);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to load practitioners", "OK");
            }
        }
        #endregion

        #region Availability Management
        partial void OnSelectedPractitionerChanged(PracticeProfile value)
        {
            if (value != null)
            {
                _ = LoadPractitionerAvailabilityAsync(value.Id);
            }
        }

        private async Task LoadPractitionerAvailabilityAsync(string practitionerId)
        {
            try
            {
                IsLoading = true;
                var availability = await _dataService.GetPractitionerAvailabilityAsync(practitionerId);
                var existingAppointments = await _dataService.GetPractitionerAppointmentsAsync(practitionerId);

                GenerateAvailableDates(availability, existingAppointments);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to load availability", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void GenerateAvailableDates(Availability availability, List<Appointment> existingAppointments)
        {
            AvailableDates.Clear();
            var today = DateTime.Today;

            for (int i = 0; i < 30; i++)
            {
                var date = today.AddDays(i);
                var dayName = date.ToString("ddd");

                if (!availability.AvailableDays.Contains(dayName))
                    continue;

                var timeSlots = availability.TimeSlots.Select(time => new TimeSlot
                {
                    Time = time,
                    IsAvailable = !existingAppointments.Any(a =>
                        a.Date.Date == date.Date &&
                        a.TimeSlot == time &&
                        a.Status != AppointmentStatus.Cancelled),
                    IsSelected = false
                }).ToList();

                if (timeSlots.Any(t => t.IsAvailable))
                {
                    AvailableDates.Add(new AppointmentDate
                    {
                        Date = date,
                        TimeSlots = timeSlots,
                        IsAvailable = true,
                        IsSelected = false
                    });
                }
            }
        }
        #endregion

        #region Commands
        [RelayCommand]
        private void SelectDate(AppointmentDate date)
        {
            if (date == null) return;

            foreach (var d in AvailableDates)
            {
                d.IsSelected = false;
            }

            date.IsSelected = true;
            SelectedDate = date;
            SelectedTimeSlot = null;
            UpdateCanBook();
        }

        [RelayCommand]
        private void SelectTimeSlot(TimeSlot timeSlot)
        {
            if (timeSlot == null || !timeSlot.IsAvailable)
                return;

            if (SelectedDate != null)
            {
                foreach (var slot in SelectedDate.TimeSlots)
                {
                    slot.IsSelected = false;
                }
            }

            timeSlot.IsSelected = true;
            SelectedTimeSlot = timeSlot;
            UpdateCanBook();
        }

        [RelayCommand]
        private async Task BookAppointmentAsync()
        {
            if (!CanBook) return;

            try
            {
                IsLoading = true;
                var userId = await _authService.GetCurrentUserIdAsync();

                // Verify time slot is still available
                var isAvailable = await _dataService.IsTimeSlotAvailableAsync(
                    SelectedPractitioner.Id,
                    SelectedDate.Date,
                    SelectedTimeSlot.Time);

                if (!isAvailable)
                {
                    await Shell.Current.DisplayAlert(
                        "Not Available",
                        "This time slot is no longer available. Please select another time.",
                        "OK");
                    await LoadPractitionerAvailabilityAsync(SelectedPractitioner.Id);
                    return;
                }

                var appointment = new Appointment
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    PractitionerId = SelectedPractitioner.Id,
                    PractitionerName = SelectedPractitioner.Name,
                    Date = SelectedDate.Date,
                    TimeSlot = SelectedTimeSlot.Time,
                    Status = AppointmentStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                await _dataService.SaveAppointmentAsync(appointment);
                await LoadInitialDataAsync();

                // Reset selection
                SelectedTimeSlot = null;
                SelectedDate = null;
                UpdateCanBook();

                await Shell.Current.DisplayAlert(
                    "Success",
                    "Appointment request sent successfully",
                    "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to book appointment", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CancelAppointmentAsync(Appointment appointment)
        {
            var confirm = await Shell.Current.DisplayAlert(
                "Cancel Appointment",
                "Are you sure you want to cancel this appointment?",
                "Yes", "No");

            if (!confirm) return;

            try
            {
                IsLoading = true;
                await _dataService.UpdateAppointmentStatusAsync(
                    appointment.Id,
                    AppointmentStatus.Cancelled);
                await LoadAppointmentsAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to cancel appointment", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadInitialDataAsync();
        }
        #endregion

        private void UpdateCanBook()
        {
            CanBook = SelectedPractitioner != null &&
                     SelectedDate != null &&
                     SelectedTimeSlot != null &&
                     SelectedTimeSlot.IsAvailable;
        }
    }
}