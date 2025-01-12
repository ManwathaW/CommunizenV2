using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Models
{

    public class DateSlot
    {
        public string DayOfWeek { get; set; }
        public string Day { get; set; }
        public DateTime Date { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsSelected { get; set; }
    }

}
