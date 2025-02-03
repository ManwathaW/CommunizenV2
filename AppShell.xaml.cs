using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Font = Microsoft.Maui.Font;
using CommuniZEN.Views;
using CommuniZEN.Converters;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using CommuniZEN.Controls;
using System.Diagnostics;
using Firebase.Database;
using Firebase.Storage;
using Plugin.Maui.Audio;

namespace CommuniZEN
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();


            var firebaseClient = new FirebaseClient("your-firebase-url");
            var firebaseStorage = new FirebaseStorage("your-storage-url");
            var audioManager = AudioManager.Current;

            DependencyService.RegisterSingleton<IAudioManager>(audioManager);
            DependencyService.RegisterSingleton(new JournalViewModel(audioManager, firebaseClient, firebaseStorage)); 

            Resources["MessageAlignmentConverter"] = new MessageAlignmentConverter();
            Resources["MessageTextColorConverter"] = new MessageTextColorConverter();
            RegisterRoutes();
            this.Navigated += OnNavigated;
        }

        private void RegisterRoutes()
        {
            Routing.RegisterRoute("login", typeof(LoginPage));
            Routing.RegisterRoute("bookings", typeof(BookingsPage));
            Routing.RegisterRoute("main", typeof(MainPage));
            Routing.RegisterRoute("register", typeof(RegisterPage));
            Routing.RegisterRoute("chatbotintro", typeof(ChatbotIntro));
            Routing.RegisterRoute("practitionerdashboard", typeof(PractitionerDashboardPage));
      
            Routing.RegisterRoute("appointments", typeof(ClientAppointmentsPage));
            Routing.RegisterRoute("affirmationspage", typeof(DailyAffirmationsPage));
             Routing.RegisterRoute("appointments", typeof(ClientAppointmentsPage));
            Routing.RegisterRoute("journalpage", typeof(JournalPage));
            Routing.RegisterRoute("chatpage", typeof(ChatPage));
        }

        private void OnNavigated(object sender, ShellNavigatedEventArgs e)
        {
            if (CurrentItem is TabBar tabBar)
            {
                foreach (ShellSection item in tabBar.Items)
                {
                    bool isSelected = item == CurrentItem.CurrentItem;
                    Shell.SetTabBarBackgroundColor(item, Colors.White);
                    Shell.SetTabBarUnselectedColor(item, isSelected ? Color.FromArgb("#4B89DC") : Color.FromArgb("#95A5A6"));
                    Shell.SetTabBarTitleColor(item, isSelected ? Color.FromArgb("#4B89DC") : Color.FromArgb("#95A5A6"));
                }
            }
        }

        private void SfSegmentedControl_SelectionChanged(object sender,
            Syncfusion.Maui.Toolkit.SegmentedControl.SelectionChangedEventArgs e)
        {
            Application.Current!.UserAppTheme = e.NewIndex == 0 ? AppTheme.Light : AppTheme.Dark;
        }
    }
}