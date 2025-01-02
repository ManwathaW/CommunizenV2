using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Models
{
    public class UserSession
    {
        public DateTime LastLogin { get; set; }
        public string DeviceInfo { get; set; }
        public string LoginIp { get; set; }
        public string TokenId { get; set; }
    }
}
