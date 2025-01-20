using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Models
{
    public class UserStatus
    {
        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("isOnline")]
        public bool IsOnline { get; set; }

        [JsonProperty("lastSeen")]
        public DateTime LastSeen { get; set; }
    }
}
