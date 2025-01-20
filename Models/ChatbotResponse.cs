using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Models
{
    public class ChatbotResponse
    {

        public string Answer { get; set; }       
        public string Confidence { get; set; }     
        public List<string> References { get; set; }

    }
}
