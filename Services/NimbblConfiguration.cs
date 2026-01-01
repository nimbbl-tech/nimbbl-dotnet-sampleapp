namespace NimbblDotnetSampleapp.Services;

/// <summary>
/// Centralized configuration service for accessing environment variables
/// </summary>
public class NimbblConfiguration
{
    public string AccessKey { get; }
    public string AccessSecret { get; }

    public static NimbblConfiguration Instance { get; } = new NimbblConfiguration();

    private NimbblConfiguration()
    {
        // Load environment variables from .env file if it exists
        EnvLoader.LoadEnvFile();

        AccessKey = Environment.GetEnvironmentVariable("NIMBBL_ACCESS_KEY")
            ?? throw new InvalidOperationException("NIMBBL_ACCESS_KEY environment variable is not set.");
        AccessSecret = Environment.GetEnvironmentVariable("NIMBBL_ACCESS_SECRET")
            ?? throw new InvalidOperationException("NIMBBL_ACCESS_SECRET environment variable is not set.");
    }
}

