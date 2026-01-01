using System;
using System.IO;

namespace NimbblDotnetSampleapp.Services;

/// <summary>
/// Utility for loading environment variables from .env files
/// </summary>
public static class EnvLoader
{
    /// <summary>
    /// Load environment variables from .env file in the current directory
    /// </summary>
    public static void LoadEnvFile(string? envFilePath = null)
    {
        envFilePath ??= Path.Combine(Directory.GetCurrentDirectory(), ".env");

        if (!File.Exists(envFilePath))
            return;

        foreach (var line in File.ReadAllLines(envFilePath))
        {
            var trimmedLine = line.Trim();
            
            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;

            var parts = trimmedLine.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim().Trim('"').Trim('\'');
                
                // Only set if not already set (environment variables take precedence)
                if (Environment.GetEnvironmentVariable(key) == null)
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }
    }
}

