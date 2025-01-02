using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommuniZEN.Models
{
    public class FirebaseAuthResult
    {
        public FirebaseUser User { get; set; }
        public string Token { get; set; }
    }

    public class FirebaseUser
    {
        public string Uid { get; set; }
        public string Email { get; set; }
    }
}
