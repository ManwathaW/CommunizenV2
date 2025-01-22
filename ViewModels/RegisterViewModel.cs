using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Models;
using CommuniZEN.Views;
using CommuniZEN.Interfaces;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommuniZEN.ViewModels
{
    public partial class RegisterViewModel : ObservableObject
    {
        private readonly IFirebaseAuthService _authService;
        private readonly IFirebaseDataService _dataService;

        private readonly Dictionary<string, UserRole> _roleMapping = new()
       {
           { "Patient/Client", UserRole.User },
           { "Healthcare Provider", UserRole.Practitioner }
       };

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private string fullName = string.Empty;



        [ObservableProperty]
        private UserRole selectedRole = UserRole.User;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPractitionerFieldsVisible))]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string selectedRoleDisplay = "Patient/Client";

        [ObservableProperty]
        private string? licenseNumber;

        [ObservableProperty]
        private string? specialization;

        [ObservableProperty]
        private bool isPractitionerFieldsVisible;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private bool isRegistering;

        public ObservableCollection<string> AvailableRoles { get; } = new ObservableCollection<string>();

        public RegisterViewModel(IFirebaseAuthService authService, IFirebaseDataService dataService)
        {
            _authService = authService;
            _dataService = dataService;
            AvailableRoles.Add("Patient/Client");
            AvailableRoles.Add("Healthcare Provider");
        }

        partial void OnSelectedRoleDisplayChanged(string oldValue, string newValue)
        {
            if (_roleMapping.TryGetValue(newValue, out var role))
            {
                SelectedRole = role;
                IsPractitionerFieldsVisible = role == UserRole.Practitioner;
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) ||
                string.IsNullOrWhiteSpace(ConfirmPassword) ||
                string.IsNullOrWhiteSpace(FullName))
            {
                ErrorMessage = "Please fill in all required fields.";
                return false;
            }

            if (!IsValidEmail(Email))
            {
                ErrorMessage = "Please enter a valid email address.";
                return false;
            }

            if (Password.Length < 6)
            {
                ErrorMessage = "Password must be at least 6 characters long.";
                return false;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return false;
            }

            if (SelectedRole == UserRole.Practitioner &&
                (string.IsNullOrWhiteSpace(LicenseNumber) || string.IsNullOrWhiteSpace(Specialization)))
            {
                ErrorMessage = "Please fill in all practitioner fields.";
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            if (IsRegistering) return;

            try
            {
                // Add this debug message
                ErrorMessage = "Starting registration process...";

                if (!ValidateInput())
                {
                    // Add this debug message
                    ErrorMessage = "Validation failed: " + ErrorMessage;
                    return;
                }

                IsRegistering = true;
                ErrorMessage = "Validating passed, creating user...";

                // Create user with Firebase Authentication
                var authResult = await _authService.RegisterWithEmailAndPasswordAsync(Email, Password);

                ErrorMessage = "User created, creating profile...";

                // Create user profile in Firebase Database
                var userProfile = new RegisterRequest
                {
                    Email = Email,
                    FullName = FullName,
                  
                    Role = SelectedRole,
                    LicenseNumber = LicenseNumber,
                    Specialization = Specialization,
                    CreatedAt = DateTime.UtcNow
                };

                await _dataService.CreateUserProfileAsync(authResult.User.Uid, userProfile);

                ErrorMessage = "Registration successful, navigating to login...";
                await Shell.Current.GoToAsync("///login");
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message.Contains("EMAIL_EXISTS")
                    ? "This email is already registered."
                    : $"Registration failed: {ex.Message}";
            }
            finally
            {
                IsRegistering = false;
            }
        }

        [RelayCommand]
        private async Task NavigateToLoginAsync()
        {
            try
            {
                
                await Shell.Current.GoToAsync("login");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Navigation failed: {ex.Message}";
            }
        }
    }
}