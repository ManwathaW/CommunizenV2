﻿using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using CommuniZEN.ViewModels;
using CommuniZEN.Interfaces;
using CommuniZEN.Services;
using CommuniZEN.Views;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Firebase.Database;



namespace CommuniZEN
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .UseMauiMaps()
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionToolkit()
                .ConfigureMauiHandlers(handlers =>
                {
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                });

#if DEBUG

            builder.Services.AddSingleton<FirebaseClient>(serviceProvider =>
          new FirebaseClient(
              "https://communizen-c112-default-rtdb.asia-southeast1.firebasedatabase.app/",
              new FirebaseOptions
              {
                  AuthTokenAsyncFactory = () => Task.FromResult(FirebaseConfig.ApiKey)
              }));



            builder.Logging.AddDebug();
    		builder.Services.AddLogging(configure => configure.AddDebug());
#endif
            // Register Services
            builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);
            builder.Services.AddSingleton<IGeocoding>(Geocoding.Default);
            builder.Services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();
            builder.Services.AddSingleton<IFirebaseDataService, FirebaseDataService>();
            builder.Services.AddTransient<ClientAppointmentsPage>();
            builder.Services.AddTransient<PractitionerAppointmentsPage>();
            builder.Services.AddTransient<PractitionerProfilePage>();

            // Register ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<MainPageViewModel>();
            builder.Services.AddTransient<ChatbotintroViewModel>();
            builder.Services.AddTransient<ChatbotViewModel>();
            builder.Services.AddTransient<PractitionerDashboardViewModel>();
            builder.Services.AddTransient<BookingsViewModel>();
            builder.Services.AddTransient<MapPickerViewModel>();
            builder.Services.AddTransient<PractitionerProfileViewModel>();
            builder.Services.AddTransient<ClientAppointmentsViewModel>();
            builder.Services.AddTransient<PractitionerAppointmentsViewModel>();
            builder.Services.AddTransient<PractitionerProfileViewModel>();

            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<ChatbotIntro>();
            builder.Services.AddTransient<ChatbotPage>();
            builder.Services.AddTransient<MapPage>();
            builder.Services.AddTransient<PractitionerDashboardPage>();
            builder.Services.AddTransient<BookingsPage>();
            builder.Services.AddTransient<MapPage>();
            builder.Services.AddTransient<PractitionerAppointmentsPage>();
            builder.Services.AddTransient<PractitionerProfilePage>();



            return builder.Build();
        }
    }
}
