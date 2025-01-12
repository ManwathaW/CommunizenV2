using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Interfaces;
using CommuniZEN.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.ViewModels
{

    public partial class PractitionerAppointmentsViewModel : ObservableObject
    {
        private readonly IFirebaseDataService _dataService;
        private readonly IFirebaseAuthService _authService;

        [ObservableProperty]
        private ObservableCollection<Appointment> pendingAppointments;

        [ObservableProperty]
        private ObservableCollection<Appointment> upcomingAppointments;

        [ObservableProperty]
        private ObservableCollection<Appointment> pastAppointments;

        [ObservableProperty]
        private bool isLoading;

        public PractitionerAppointmentsViewModel(IFirebaseDataService dataService, IFirebaseAuthService authService)
        {
            _dataService = dataService;
            _authService = authService;
            InitializeCollections();
            _ = LoadAppointmentsAsync();
        }

        private void InitializeCollections()
        {
            PendingAppointments = new ObservableCollection<Appointment>();
            UpcomingAppointments = new ObservableCollection<Appointment>();
            PastAppointments = new ObservableCollection<Appointment>();
        }

        private async Task LoadAppointmentsAsync()
        {
            try
            {
                IsLoading = true;
                var userId = await _authService.GetCurrentUserIdAsync();
                var appointments = await _dataService.GetPractitionerAppointmentsAsync(userId);

                UpdateAppointmentCollections(appointments);
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
                await _dataService.UpdateAppointmentStatusAsync(
                    appointment.Id, AppointmentStatus.Approved);
                await LoadAppointmentsAsync();
                await Shell.Current.DisplayAlert("Success", "Appointment approved", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to approve appointment", "OK");
            }
        }

        [RelayCommand]
        private async Task RejectAppointment(Appointment appointment)
        {
            var reason = await Shell.Current.DisplayPromptAsync(
                "Reject Appointment",
                "Please provide a reason for rejection:",
                "Submit",
                "Cancel");

            if (!string.IsNullOrEmpty(reason))
            {
                try
                {
                    appointment.Notes = reason;
                    await _dataService.UpdateAppointmentStatusAsync(
                        appointment.Id, AppointmentStatus.Rejected);
                    await LoadAppointmentsAsync();
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Error", "Failed to reject appointment", "OK");
                }
            }
        }
    }

}
