using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Models
{

    public class TypingStatus
    {
        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("isTyping")]
        public bool IsTyping { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }

}
