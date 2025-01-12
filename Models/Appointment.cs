using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Models
{
    public class Appointment
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string PractitionerId { get; set; }
        public string PractitionerUserId { get; set; }
        public string PractitionerName { get; set; }
        public DateTime Date { get; set; }
        public string TimeSlot { get; set; }
        public AppointmentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; }
    }

    public enum AppointmentStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled,
        Completed
    }
}
