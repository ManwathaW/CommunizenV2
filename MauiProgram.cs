using CommunityToolkit.Maui;
using CommuniZEN.Converters;
using CommuniZEN.Interfaces;
using CommuniZEN.Services;
using CommuniZEN.ViewModels;
using CommuniZEN.Views;
using Firebase.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;
using System.Diagnostics;

namespace CommuniZEN
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // Add configuration first
            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .UseMauiMaps()

                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                });

            // Initialize Firebase Configuration
            try
            {
                FirebaseConfig.Initialize(builder.Configuration);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Firebase Configuration Error: {ex.Message}");
            }

            // Register Firebase Client first
            builder.Services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();
            builder.Services.AddSingleton<FirebaseClient>(provider =>
                new FirebaseClient(FirebaseConfig.RealtimeDatabaseUrl));

#if DEBUG
            const string openAiKey = "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";
            builder.Logging.AddDebug();
            builder.Services.AddTransient<Controls.AudioVisualizer>();
            builder.Services.AddTransient<JournalPage>();
            builder.Services.AddTransient<DailyAffirmationsPage>();
            builder.Services.AddTransient<DailyAffirmationsViewModel>();
            builder.Services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();
            builder.Services.AddTransient<PractitionerAppointmentsViewModel>();
            builder.Services.AddSingleton(AudioManager.Current);
            builder.Services.AddLogging(configure => configure.AddDebug());
            builder.Services.AddSingleton<IChatbotService>(_ =>
            new OpenAIChatbotService(openAiKey));
#endif

            // Register core services first
            RegisterCoreServices(builder.Services);

            // Register feature services
            RegisterFeatureServices(builder.Services);

            // Register ViewModels
            RegisterViewModels(builder.Services);

            // Register Pages
            RegisterPages(builder.Services);

            return builder.Build();
        }

        private static void RegisterCoreServices(IServiceCollection services)
        {
            services.AddSingleton<IGeolocation>(Geolocation.Default);
            services.AddSingleton<IGeocoding>(Geocoding.Default);
            services.AddSingleton(AudioManager.Current);
            services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();
            services.AddSingleton<IFirebaseDataService, FirebaseDataService>();
        }

        private static void RegisterFeatureServices(IServiceCollection services)
        {
            var realtimeDatabaseUrl = "https://communizen-c112-default-rtdb.asia-southeast1.firebasedatabase.app/";
            services.AddSingleton<FirebaseClient>(provider =>
                new FirebaseClient(realtimeDatabaseUrl));

            // Chat service registration
            services.AddSingleton<IChatService>(sp =>
            {
                try
                {
                    var firebaseClient = sp.GetRequiredService<FirebaseClient>();
                    var authService = sp.GetRequiredService<IFirebaseAuthService>();
                    var userId = authService.GetCurrentUserIdAsync().Result;

                    if (string.IsNullOrEmpty(userId))
                    {
                        throw new InvalidOperationException("No user is currently signed in");
                    }

                    return new FirebaseChatService(
                        firebaseClient,
                        userId,
                        "user",
                        "User"
                    );
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Chat service initialization failed: {ex.Message}");
                    throw;
                }
            });

            // Register Converters
            services.AddSingleton<MessageBackgroundConverter>();
            services.AddSingleton<MessageAlignmentConverter>();
            services.AddSingleton<MessageTextColorConverter>();
            services.AddSingleton<StringToBoolConverter>();
            services.AddSingleton<IntToBoolConverter>();
            services.AddSingleton<IsNullConverter>();
            services.AddSingleton<IsNotNullConverter>();
            services.AddSingleton<DateTimeConverter>();
            services.AddSingleton<InverseBoolConverter>();
            services.AddSingleton<TextTypeConverter>();
            services.AddSingleton<AudioTypeConverter>();
            services.AddSingleton<InvertedBoolConverter>();
            services.AddSingleton<AudioPlayButtonConverter>();
            services.AddSingleton<TimeSpanToStringConverter>();

        }

        private static void RegisterViewModels(IServiceCollection services)
        {
            services.AddTransient<LoginViewModel>();
            services.AddTransient<MainPageViewModel>();
            services.AddTransient<ChatbotintroViewModel>();
            services.AddTransient<ChatbotViewModel>();
            services.AddTransient<PractitionerDashboardViewModel>();
            services.AddTransient<BookingsViewModel>();
            services.AddTransient<MapPickerViewModel>();
            services.AddTransient<PractitionerAppointmentsViewModel>();
            services.AddTransient<DailyAffirmationsViewModel>();
            services.AddTransient<ClientAppointmentsViewModel>();
            services.AddSingleton<ChatViewModel>();

            // Journal ViewModel registration with proper dependencies
            services.AddTransient<JournalViewModel>(sp =>
            {
                var audioManager = sp.GetRequiredService<IAudioManager>();
                var firebaseService = sp.GetRequiredService<IFirebaseDataService>();
                var authService = sp.GetRequiredService<IFirebaseAuthService>();

                return new JournalViewModel(
                    audioManager,
                    firebaseService,
                    authService
                );
            });
        }

        private static void RegisterPages(IServiceCollection services)
        {
            services.AddTransient<LoginPage>();
            services.AddTransient<MainPage>();
            services.AddTransient<ChatbotIntro>();
            services.AddTransient<ChatbotPage>();
            services.AddTransient<MapPage>();
            services.AddTransient<PractitionerDashboardPage>();
            services.AddTransient<BookingsPage>();
            services.AddTransient<PractitionerAppointmentsPage>();
            services.AddTransient<ChatPage>();
            services.AddTransient<JournalPage>();
            services.AddTransient<DailyAffirmationsPage>();
            services.AddTransient<ClientAppointmentsPage>();
        }
    }
}