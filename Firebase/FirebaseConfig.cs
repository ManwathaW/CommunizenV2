using Microsoft.Extensions.Configuration;

public class FirebaseConfig
{
    public static string ApiKey { get; private set; }
    public static string AuthDomain { get; private set; }
    public static string ProjectId { get; private set; }
    public static string StorageBucket { get; private set; }
    public static string DatabaseUrl { get; private set; }
    public static string MessagingSenderId { get; private set; }
    public static string AppId { get; private set; }

    public static void Initialize(IConfiguration configuration)
    {
        ApiKey = configuration["Firebase:ApiKey"];
        AuthDomain = configuration["Firebase:AuthDomain"];
        ProjectId = configuration["Firebase:ProjectId"];
        StorageBucket = configuration["Firebase:StorageBucket"];
        DatabaseUrl = configuration["Firebase:DatabaseUrl"];
        MessagingSenderId = configuration["Firebase:MessagingSenderId"];
        AppId = configuration["Firebase:AppId"];

        ValidateConfiguration();
    }

    private static void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(ApiKey))
            throw new InvalidOperationException("Firebase API Key is not configured");
        if (string.IsNullOrEmpty(AuthDomain))
            throw new InvalidOperationException("Firebase Auth Domain is not configured");
        // Add other validations as needed
    }
}