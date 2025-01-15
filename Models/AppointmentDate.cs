using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Models
{
    public class AppointmentDate
    {
        public DateTime Date { get; set; }
        public List<TimeSlot> TimeSlots { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsSelected { get; set; }

        public AppointmentDate()
        {
            TimeSlots = new List<TimeSlot>();
        }
    }
}