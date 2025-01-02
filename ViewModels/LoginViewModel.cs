using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Interfaces;
using CommuniZEN.Models;

namespace CommuniZEN.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IFirebaseAuthService _authService;
        private readonly IFirebaseDataService _dataService;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanLogin))]
        private string emailInput = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanLogin))]
        private string passwordInput = string.Empty;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanLogin))]
        private bool isLoading;

        public bool CanLogin => !IsLoading &&
                              !string.IsNullOrWhiteSpace(EmailInput) &&
                              !string.IsNullOrWhiteSpace(PasswordInput);

        public LoginViewModel(IFirebaseAuthService authService, IFirebaseDataService dataService)
        {
            _authService = authService;
            _dataService = dataService;
        }

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var authResult = await _authService.SignInWithEmailAndPasswordAsync(EmailInput, PasswordInput);
                
                // Get or create user profile
                var userProfile = await _dataService.GetUserProfileAsync(authResult.User.Uid);

                if (userProfile == null)
                {
                    // Create default profile
                    userProfile = new UserProfile
                    {
                        Email = EmailInput,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        Role = UserRole.User // Default role
                    };

                    await _dataService.CreateUserProfileAsync(authResult.User.Uid, new RegisterRequest
                    {
                        Email = EmailInput,
                        Role = UserRole.User
                    });
                }

                // Navigate based on role
                if (userProfile.Role == UserRole.Practitioner)
                {
                    await Shell.Current.GoToAsync("//practitionerdashboard");
                }
                else
                {
                    await Shell.Current.GoToAsync("//mainpage");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message.ToLower();
                if (errorMessage.Contains("password") || errorMessage.Contains("user"))
                {
                    ErrorMessage = "Invalid email or password.";
                }
                else if (errorMessage.Contains("network"))
                {
                    ErrorMessage = "Network error. Please check your connection.";
                }
                else
                {
                    ErrorMessage = "Login failed. Please try again.";
                }

                await Application.Current.MainPage.DisplayAlert("Login Error", ErrorMessage, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoginWithGoogleAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                await Application.Current.MainPage.DisplayAlert(
                    "Coming Soon",
                    "Google sign-in will be available in the next update!",
                    "OK");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Google sign-in is currently unavailable.";
                await Application.Current.MainPage.DisplayAlert(
                    "Sign In Error",
                    ErrorMessage,
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task NavigateToRegisterAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("//register");
            }
            catch (Exception)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Navigation Error",
                    "Unable to navigate to registration. Please try again.",
                    "OK");
            }
        }

        [RelayCommand]
        private async Task ForgotPasswordAsync()
        {
            if (string.IsNullOrWhiteSpace(EmailInput))
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Password Reset",
                    "Please enter your email address to receive reset instructions.",
                    "OK");
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                await _authService.SendPasswordResetEmailAsync(EmailInput);

                await Application.Current.MainPage.DisplayAlert(
                    "Password Reset",
                    "Reset instructions have been sent to your email.",
                    "OK");
            }
            catch (Exception)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Reset Error",
                    "Unable to send reset email. Please verify your email address.",
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void ClearInputs()
        {
            EmailInput = string.Empty;
            PasswordInput = string.Empty;
            ErrorMessage = null;
        }
    }
}