using Microsoft.Extensions.Configuration;
using System.Text;
using CommuniZEN.ViewModels;
using CommuniZEN.Interfaces;
using CommuniZEN.Services;
using CommuniZEN.Views;


namespace CommuniZEN
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            ConfigureServices();


            Routing.RegisterRoute("login", typeof(LoginPage));
            Routing.RegisterRoute("register", typeof(RegisterPage));
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Create configuration
            var configJson = @"{""Firebase"":{""ApiKey"":""AIzaSyCjQAkyGjbTQKjI9tBvzT4JQH-ST86o7EM"",""ProjectId"":""communizen-c112"",""StorageBucket"":""communizen-c112.firebasestorage.app"",""AuthDomain"":""communizen-c112.firebaseapp.com"",""DatabaseUrl"":""https://communizen-c112.firebaseio.com"",""MessagingSenderId"":""343938337235"",""AppId"":""1:343938337235:android:5546cb5b89330781148726""}}";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(configJson));

            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();

            FirebaseConfig.Initialize(config);

            // Register services and viewmodel
            services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();
            services.AddSingleton<IFirebaseDataService, FirebaseDataService>();
            services.AddTransient<RegisterViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<MainPageViewModel>();
            services.AddTransient<MapPageViewModel>();
            services.AddTransient<ChatbotintroViewModel>();
            //Pages
            services.AddTransient<LoginPage>(); 
            services.AddTransient<MapPage>();
            services.AddTransient<ChatbotIntro>();
            services.AddTransient<ChatbotPage>();
            services.AddTransient<RegisterPage>();
            services.AddTransient<MainPage>();
            services.AddTransient<PractitionerDashboardPage>();

            //Routing
            Routing.RegisterRoute("chatbotpage", typeof(ChatbotPage));
            Routing.RegisterRoute("map", typeof(MapPage));
            Routing.RegisterRoute("chatbotintro", typeof(ChatbotIntro));
            Routing.RegisterRoute("login", typeof(LoginPage));
            Routing.RegisterRoute("register", typeof(RegisterPage));
            Routing.RegisterRoute("mainpage", typeof(MainPage));
            Routing.RegisterRoute("practitionerdashboard", typeof(PractitionerDashboardPage));

            ServiceProvider = services.BuildServiceProvider();

            MainPage = new AppShell();
        }

        public static IServiceProvider ServiceProvider { get; private set; }
    }
}