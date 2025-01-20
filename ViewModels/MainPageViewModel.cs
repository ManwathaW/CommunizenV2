using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using CommuniZEN.Controls;
using CommuniZEN.Interfaces;
using CommuniZEN.Services;
using CommuniZEN.Models;
using Microsoft.Maui.Devices.Sensors;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Firebase.Database;
using Firebase.Database.Query;

namespace CommuniZEN.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        private readonly IFirebaseDataService _dataService;
        private readonly IFirebaseAuthService _authService;
        private readonly FirebaseClient _firebaseClient;


        [ObservableProperty]
        private string userProfileImage = "profile_placeholder.png";

        [ObservableProperty]
        private string userName;



        public MainPageViewModel(IFirebaseAuthService authService, IFirebaseDataService dataService, FirebaseClient firebaseClient)
        {
            // Get user profile and set name

            _dataService = dataService;
            _authService = authService;
            _firebaseClient = firebaseClient;
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadUserProfile();
        }


        //profile pic upload
        [RelayCommand]
        private async Task ChangeProfilePicture()
        {
            try
            {
                Debug.WriteLine("Starting profile picture change");

                if (MediaPicker.Default.IsCaptureSupported)
                {
                    FileResult photo = await MediaPicker.Default.PickPhotoAsync();

                    if (photo != null)
                    {
                        Debug.WriteLine("Photo selected");
                        using var stream = await photo.OpenReadAsync();
                        var userId = await _authService.GetCurrentUserIdAsync();

                        Debug.WriteLine($"Uploading for user: {userId}");

                        // Upload and get the image data
                        var imageData = await _dataService.UploadProfileImageAsync(userId, stream);

                        Debug.WriteLine("Image uploaded, updating profile");

                        // Update the user profile
                        var userProfile = await _dataService.GetUserProfileAsync(userId);
                        userProfile.ProfileImage = imageData;  // Store the actual image data
                        await _dataService.UpdateUserProfileAsync(userId, userProfile);

                        Debug.WriteLine("Profile updated, updating UI");

                        // Update the UI - use the actual image data
                        UserProfileImage = imageData;

                        Debug.WriteLine("Profile picture update complete");
                        await Shell.Current.DisplayAlert("Success", "Profile picture updated successfully", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ChangeProfilePicture: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to update profile picture", "OK");
            }
        }


        private async Task LoadUserProfile()
        {
            try
            {
                var userId = await _authService.GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(userId))
                {
                    Debug.WriteLine("GetCurrentUserIdAsync returned null or empty userId");
                    UserName = "Guest";
                    return;
                }

                var userProfile = await _dataService.GetUserProfileAsync(userId);
                if (userProfile == null)
                {
                    Debug.WriteLine($"GetUserProfileAsync returned null for userId: {userId}");
                    UserName = "Guest";
                    return;
                }

                // Try to get the profile image data
                try
                {
                    var imageData = await _firebaseClient
                        .Child("profile_images")
                        .Child(userId)
                        .OnceSingleAsync<Dictionary<string, string>>();

                    if (imageData != null && imageData.ContainsKey("imageData"))
                    {
                        UserProfileImage = imageData["imageData"];
                        Debug.WriteLine("Profile image loaded successfully");
                    }
                    else
                    {
                        UserProfileImage = "profile_placeholder.png";
                        Debug.WriteLine("No profile image found, using placeholder");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading profile image: {ex.Message}");
                    UserProfileImage = "profile_placeholder.png";
                }

                UserName = userProfile.FullName ?? "Guest";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadUserProfile: {ex.Message}");
                UserName = "Guest";
                UserProfileImage = "profile_placeholder.png";
            }
        }

        [RelayCommand]
        private async Task OpenChatbot()
        {
            await Shell.Current.GoToAsync("chatbotintro");
        } 
        
        [RelayCommand]
        private async Task OpenChat()
        {
            await Shell.Current.GoToAsync("chatpage");
        }

    }
}