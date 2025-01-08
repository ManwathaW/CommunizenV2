using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Models
{
    public class UserProfile
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public UserRole Role { get; set; }
        public string? LicenseNumber { get; set; }
        public string? Specialization { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdated { get; set; }
        public bool IsActive { get; set; }
        public string ProfileImage { get; set; }
        public string Location { get; set; } = "Location not specified"; // Added this
        public Dictionary<string, object> Settings { get; set; } = new();
    }
}
