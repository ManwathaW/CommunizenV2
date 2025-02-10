using CommuniZEN.Converters;
using CommuniZEN.Views;
using Plugin.Maui.Audio;


namespace CommuniZEN
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntry), (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
                handler.PlatformView.Background = null;
#elif IOS || MACCATALYST
                handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
                handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
#elif WINDOWS
                handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
#endif
            });

            var audioManager = AudioManager.Current;

            DependencyService.RegisterSingleton<IAudioManager>(audioManager);


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