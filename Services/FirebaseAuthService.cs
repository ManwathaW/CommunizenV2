using Firebase.Auth;
using Firebase.Auth.Providers;
using CommuniZEN.Interfaces;
using CommuniZEN.Models;

using System;
using System.Threading.Tasks;
using System.Security.Authentication;

namespace CommuniZEN.Services
{
    public class FirebaseAuthService : IFirebaseAuthService
    {
        private readonly FirebaseAuthClient _authClient;
        private UserCredential? _currentUserCredential;
        public event EventHandler<AuthStateChangedEventArgs>? UserAuthStateChanged;

        public FirebaseAuthService()
        {
            try
            {
                ValidateFirebaseConfig();

                var config = new FirebaseAuthConfig
                {
                    ApiKey = FirebaseConfig.ApiKey,
                    AuthDomain = FirebaseConfig.AuthDomain,
                    Providers = new FirebaseAuthProvider[]
                    {
                        new EmailProvider()
                    }
                };

                _authClient = new FirebaseAuthClient(config);
                _authClient.AuthStateChanged += OnAuthStateChanged;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize Firebase Authentication: {ex.Message}");
            }
        }

        private void ValidateFirebaseConfig()
        {
            if (string.IsNullOrEmpty(FirebaseConfig.ApiKey))
                throw new InvalidOperationException("Firebase API Key is not configured");
            if (string.IsNullOrEmpty(FirebaseConfig.AuthDomain))
                throw new InvalidOperationException("Firebase Auth Domain is not configured");
        }

        private void OnAuthStateChanged(object? sender, UserEventArgs e)
        {
            UserAuthStateChanged?.Invoke(this, new AuthStateChangedEventArgs
            {
                IsAuthenticated = e.User != null,
                User = e.User != null ? new FirebaseUser
                {
                    Uid = e.User.Uid,
                    Email = e.User.Info.Email
                } : null
            });
        }

        public async Task<FirebaseAuthResult> RegisterWithEmailAndPasswordAsync(string email, string password)
        {
            try
            {
                ValidateCredentials(email, password);

                var userCredential = await _authClient.CreateUserWithEmailAndPasswordAsync(email, password);
                _currentUserCredential = userCredential;

                return new FirebaseAuthResult
                {
                    User = new FirebaseUser
                    {
                        Uid = userCredential.User.Uid,
                        Email = userCredential.User.Info.Email
                    },
                    Token = await userCredential.User.GetIdTokenAsync()
                };
            }
            catch (FirebaseAuthException ex)
            {
                // Add specific error handling for common cases
                string errorMessage = ex.Reason switch
                {
                    AuthErrorReason.OperationNotAllowed => "Email/Password sign-in is not enabled. Please contact support.",
                    AuthErrorReason.EmailExists => "This email is already registered.",
                    AuthErrorReason.WeakPassword => "Password should be at least 6 characters long.",
                    _ => $"Registration failed: {ex.Message}"
                };
                throw new Exception(errorMessage);
            }
        }

        public async Task<FirebaseAuthResult> SignInWithEmailAndPasswordAsync(string email, string password)
        {
            try
            {
                ValidateCredentials(email, password);

                var userCredential = await _authClient.SignInWithEmailAndPasswordAsync(email, password);
                _currentUserCredential = userCredential;

                return new FirebaseAuthResult
                {
                    User = new FirebaseUser
                    {
                        Uid = userCredential.User.Uid,
                        Email = userCredential.User.Info.Email
                    },
                    Token = await userCredential.User.GetIdTokenAsync()
                };
            }
            catch (FirebaseAuthException ex)
            {
                throw HandleFirebaseAuthException(ex, "sign-in");
            }
        }

        public Task SignOutAsync()
        {
            try
            {
                if (_currentUserCredential != null)
                {
                    _currentUserCredential = null;
                    UserAuthStateChanged?.Invoke(this, new AuthStateChangedEventArgs
                    {
                        IsAuthenticated = false,
                        User = null
                    });
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to sign out: {ex.Message}");
            }
        }

        public Task<string> GetCurrentUserIdAsync()
        {
            try
            {
                var user = _authClient.User;
                if (user == null)
                {
                    throw new AuthenticationException("No user is currently signed in.");
                }
                return Task.FromResult(user.Uid);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get current user ID: {ex.Message}");
            }
        }

        public Task<bool> IsUserSignedInAsync()
        {
            return Task.FromResult(_authClient.User != null);
        }

        public Task<bool> IsEmailVerifiedAsync()
        {
            var user = _authClient.User;
            return Task.FromResult(user?.Info.IsEmailVerified ?? false);
        }

        public async Task<string> GetUserTokenAsync()
        {
            if (_currentUserCredential?.User == null)
                throw new AuthenticationException("No user is currently signed in.");

            return await _currentUserCredential.User.GetIdTokenAsync();
        }

        public async Task SendPasswordResetEmailAsync(string email)
        {
            try
            {
                await _authClient.ResetEmailPasswordAsync(email);
            }
            catch (FirebaseAuthException ex)
            {
                throw HandleFirebaseAuthException(ex, "password reset");
            }
        }

        private void ValidateCredentials(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty");
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty");
            if (password.Length < 6)
                throw new ArgumentException("Password must be at least 6 characters long");
        }

        private Exception HandleFirebaseAuthException(FirebaseAuthException ex, string operation)
        {
            string message = ex.Message;

            // Custom error messages based on error codes
            if (ex.Message.Contains("EMAIL_EXISTS"))
                message = "This email is already registered.";
            else if (ex.Message.Contains("INVALID_EMAIL"))
                message = "The email address is invalid.";
            else if (ex.Message.Contains("WEAK_PASSWORD"))
                message = "The password is too weak. It must be at least 6 characters long.";
            else if (ex.Message.Contains("WRONG_PASSWORD"))
                message = "Invalid email or password.";
            else if (ex.Message.Contains("USER_NOT_FOUND"))
                message = "No user found with this email address.";
            else if (ex.Message.Contains("TOO_MANY_ATTEMPTS_TRY_LATER"))
                message = "Too many failed attempts. Please try again later.";

            return new AuthenticationException(message);
        }
    }

    public class AuthStateChangedEventArgs : EventArgs
    {
        public bool IsAuthenticated { get; set; }
        public FirebaseUser? User { get; set; }
    }
}