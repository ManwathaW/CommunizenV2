using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommuniZEN.Controls;
using CommuniZEN.Interfaces;
using CommuniZEN.Services;
using CommuniZEN.Models;
using Microsoft.Maui.Devices.Sensors;
using System.Collections.ObjectModel;

namespace CommuniZEN.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        private readonly IFirebaseDataService _dataService;
        private readonly IFirebaseDataService _authService;



        [ObservableProperty]
        private string userProfileImage = "profile_placeholder.png";

        [ObservableProperty]
        private string userName;

        public MainPageViewModel(IFirebaseAuthService authService)
        {
            // Get user profile and set name
            LoadUserProfile();
        }

        private async Task LoadUserProfile()
        {
            try
            {
                var userId = await _authService.GetCurrentUserIdAsync();
                var userProfile = await _dataService.GetUserProfileAsync(userId);
                UserName = userProfile?.FullName ?? "User";
            }
            catch
            {
                UserName = "User";
            }
        }


        [RelayCommand]
        private async Task OpenChatbot()
        {
            await Shell.Current.GoToAsync("chatbotintro");
        }


    }
}