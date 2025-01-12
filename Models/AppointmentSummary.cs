using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Models
{

    public class AppointmentSummary
    {
        public int TotalAppointments { get; set; }
        public int PendingAppointments { get; set; }
        public int TodayAppointments { get; set; }
    }

}
