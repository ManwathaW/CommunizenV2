using Microsoft.Extensions.Configuration;
using System.Diagnostics;

public static class FirebaseConfig
{
    public static string ApiKey { get; private set; }
    public static string AuthDomain { get; private set; }
    public static string ProjectId { get; private set; }
    public static string StorageBucket { get; private set; }
    public static string DatabaseUrl { get; private set; }
    public static string MessagingSenderId { get; private set; }
    public static string AppId { get; private set; }
    public static string RealtimeDatabaseUrl { get; private set; }

    public static void Initialize(IConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        ApiKey = configuration["Firebase:ApiKey"];
        AuthDomain = configuration["Firebase:AuthDomain"];
        ProjectId = configuration["Firebase:ProjectId"];
        StorageBucket = configuration["Firebase:StorageBucket"];
        DatabaseUrl = configuration["Firebase:DatabaseUrl"];
        MessagingSenderId = configuration["Firebase:MessagingSenderId"];
        AppId = configuration["Firebase:AppId"];
        // Get RealtimeDatabaseUrl from config or fallback to DatabaseUrl
        RealtimeDatabaseUrl = configuration["Firebase:RealtimeDatabaseUrl"] ?? DatabaseUrl;

        ValidateConfiguration();
    }

    private static void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(ApiKey))
            throw new InvalidOperationException("Firebase API Key is not configured");

        if (string.IsNullOrEmpty(AuthDomain))
            throw new InvalidOperationException("Firebase Auth Domain is not configured");

        // Validate and format database URLs
        if (string.IsNullOrEmpty(DatabaseUrl))
            throw new InvalidOperationException("Firebase Database URL is not configured");

        if (string.IsNullOrEmpty(RealtimeDatabaseUrl))
            RealtimeDatabaseUrl = DatabaseUrl;

        // Ensure URLs end with forward slash
        if (!DatabaseUrl.EndsWith("/"))
            DatabaseUrl += "/";

        if (!RealtimeDatabaseUrl.EndsWith("/"))
            RealtimeDatabaseUrl += "/";

        Debug.WriteLine($"Firebase Configuration Loaded:");
        Debug.WriteLine($"RealtimeDatabaseUrl: {RealtimeDatabaseUrl}");
        Debug.WriteLine($"DatabaseUrl: {DatabaseUrl}");
    }
}