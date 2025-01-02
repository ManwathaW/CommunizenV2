using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Models
{
    public class Practitioner
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Specialization { get; set; }
        public string ProfileImage { get; set; }
        public string Location { get; set; }
        public Location Coordinates { get; set; }
    }
}
