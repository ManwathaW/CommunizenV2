using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CommuniZEN.Models;

namespace CommuniZEN.Models
{
    public class Appointment : INotifyPropertyChanged
    {

        private AppointmentStatus _status;
        private string _clientName;

        public string Id { get; set; }
        public string PractitionerId { get; set; }
        public string ClientId { get; set; }
        public DateTime Date { get; set; }
        public TimeSlot TimeSlot { get; set; }
        public AppointmentStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
                    Debug.WriteLine($"Appointment {Id} status changed to: {value}");
                }
            }
        }
        public string ClientName
        {
            get => _clientName;
            set
            {
                if (_clientName != value)
                {
                    _clientName = value;
                    OnPropertyChanged(nameof(ClientName));
                }
            }
        }
        public DateTime CreatedAt { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public string PractitionerName { get; set; }
        public string PractitionerSpecialization { get; set; }
        public string PractitionerImage { get; set; }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
