using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommuniZEN.Models;

namespace CommuniZEN.Interfaces
{
    public interface IFirebaseAuthService
    {
        Task<FirebaseAuthResult> RegisterWithEmailAndPasswordAsync(string email, string password);
        Task<FirebaseAuthResult> SignInWithEmailAndPasswordAsync(string email, string password);
        Task SignOutAsync();
        Task<string> GetCurrentUserIdAsync();
        Task SendPasswordResetEmailAsync(string email);
    }
}
