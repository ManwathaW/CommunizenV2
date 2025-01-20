using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Models
{

    public class Availability
    {
        public string Id { get; set; }
        public string PractitionerId { get; set; }
        public List<string> AvailableDays { get; set; }
        public List<string> TimeSlots { get; set; }  // Keep as strings
        public List<DayAvailability> DailySchedule { get; set; }

        public Availability()
        {
            AvailableDays = new List<string>();
            TimeSlots = new List<string>();  // Keep as strings
            DailySchedule = new List<DayAvailability>();
        }
    }
}