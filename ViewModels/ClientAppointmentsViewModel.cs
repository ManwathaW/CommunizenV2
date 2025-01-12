using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Models;
using CommuniZEN.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.ViewModels
{
    public partial class ClientAppointmentsViewModel : ObservableObject
    {
        private readonly IFirebaseDataService _dataService;
        private readonly IFirebaseAuthService _authService;

        [ObservableProperty]
        private ObservableCollection<Appointment> upcomingAppointments;

        [ObservableProperty]
        private ObservableCollection<Appointment> pastAppointments;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private AppointmentSummary summary;

        public ClientAppointmentsViewModel(IFirebaseDataService dataService, IFirebaseAuthService authService)
        {
            _dataService = dataService;
            _authService = authService;
            UpcomingAppointments = new ObservableCollection<Appointment>();
            PastAppointments = new ObservableCollection<Appointment>();
            _ = LoadAppointmentsAsync();
        }

        private async Task LoadAppointmentsAsync()
        {
            try
            {
                IsLoading = true;
                var userId = await _authService.GetCurrentUserIdAsync();
                var appointments = await _dataService.GetUserAppointmentsAsync(userId);

                UpdateAppointmentCollections(appointments);
                UpdateSummary(appointments);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to load appointments", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateAppointmentCollections(List<Appointment> appointments)
        {
            var now = DateTime.Now;
            UpcomingAppointments.Clear();
            PastAppointments.Clear();

            foreach (var appointment in appointments.OrderBy(a => a.Date))
            {
                if (appointment.Date >= now.Date)
                {
                    UpcomingAppointments.Add(appointment);
                }
                else
                {
                    PastAppointments.Add(appointment);
                }
            }
        }

        private void UpdateSummary(List<Appointment> appointments)
        {
            var now = DateTime.Now;
            Summary = new AppointmentSummary
            {
                TotalAppointments = appointments.Count,
                PendingAppointments = appointments.Count(a => a.Status == AppointmentStatus.Pending),
                TodayAppointments = appointments.Count(a => a.Date.Date == now.Date)
            };
        }

        [RelayCommand]
        private async Task CancelAppointment(Appointment appointment)
        {
            var confirm = await Shell.Current.DisplayAlert(
                "Cancel Appointment",
                "Are you sure you want to cancel this appointment?",
                "Yes", "No");

            if (confirm)
            {
                try
                {
                    await _dataService.UpdateAppointmentStatusAsync(appointment.Id, AppointmentStatus.Cancelled);
                    await LoadAppointmentsAsync();
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Error", "Failed to cancel appointment", "OK");
                }
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            await LoadAppointmentsAsync();
        }
    }
}
