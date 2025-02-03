using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommuniZEN.Models;

namespace CommuniZEN.Models
{
    public class Appointment
    {
        public string Id { get; set; }
        public string PractitionerId { get; set; }
        public string ClientId { get; set; }
        public DateTime Date { get; set; }
        public TimeSlot TimeSlot { get; set; }
        public AppointmentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ClientName { get; set; }
        public string PractitionerName { get; set; }
    }
}
