using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    using Microsoft.Maui.Devices.Sensors;

    namespace CommuniZEN.Models
    {
        public class PracticeProfile
        {
            public string Id { get; set; }
            public string UserId { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string Bio { get; set; }
            public string PhoneNumber { get; set; }
            public string PracticeName { get; set; }
            public string Specialization { get; set; }
            public string LicenseNumber { get; set; }
            public string Location { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string ProfileImage { get; set; }
            public DateTime CreatedAt { get; set; }

            public Location Coordinates => new Location(Latitude, Longitude);
        }
    }

        