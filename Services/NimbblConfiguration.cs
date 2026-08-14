namespace NimbblDotnetSampleapp.Services;

/// <summary>
/// Centralized configuration service for accessing environment variables
/// </summary>
public class NimbblConfiguration
{
    public string AccessKey { get; }
    public string AccessSecret { get; }
    public bool EncryptPayload { get; }
    public string? ApiHost { get; }

    public static NimbblConfiguration Instance { get; } = new NimbblConfiguration();

    private NimbblConfiguration()
    {
        // Load environment variables from .env file if it exists
        EnvLoader.LoadEnvFile();

        AccessKey = Environment.GetEnvironmentVariable("NIMBBL_ACCESS_KEY")
            ?? throw new InvalidOperationException("NIMBBL_ACCESS_KEY environment variable is not set.");
        AccessSecret = Environment.GetEnvironmentVariable("NIMBBL_ACCESS_SECRET")
            ?? throw new InvalidOperationException("NIMBBL_ACCESS_SECRET environment variable is not set.");
        
        // Parse encryption flags (default to false if not set)
        EncryptPayload = bool.TryParse(Environment.GetEnvironmentVariable("ENCRYPT_PAYLOAD"), out var encryptPayload) && encryptPayload;
        
        // Read optional API host (for development/testing environments)
        ApiHost = Environment.GetEnvironmentVariable("NIMBBL_API_HOST");
    }
}

