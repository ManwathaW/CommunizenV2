using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Models
{
    public class JournalEntry
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public JournalEntryType Type { get; set; }
    }
    public enum JournalEntryType
    {
        Text,
        Audio
    }
}
