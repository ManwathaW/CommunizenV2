using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Models
{
    public class DayAvailability
    {
        public DateTime Date { get; set; }
        public List<TimeSlot> TimeSlots { get; set; }

        public DayAvailability()
        {
            TimeSlots = new List<TimeSlot>();
        }
    }
}
