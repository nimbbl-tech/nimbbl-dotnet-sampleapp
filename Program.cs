using Nimbbl.Sdk.Rest.Api;
using Nimbbl.Sdk.Rest.Common;
using Nimbbl.Sdk.Rest.Extensions;
using NimbblDotnetSampleapp.Services;

// Load environment variables from .env file if it exists
EnvLoader.LoadEnvFile();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var accessKey = Environment.GetEnvironmentVariable("NIMBBL_ACCESS_KEY") 
    ?? throw new InvalidOperationException(ErrorMessages.AccessKeyRequired);
var accessSecret = Environment.GetEnvironmentVariable("NIMBBL_ACCESS_SECRET") 
    ?? throw new InvalidOperationException(ErrorMessages.AccessSecretRequiredEnv);

// Only read optional parameters in development mode
var isDevelopment = builder.Environment.IsDevelopment();
string? apiHost = null;
bool? enableLogging = null;
bool? debugLogging = null;
string? logFilePath = null;

if (isDevelopment)
{
    apiHost = Environment.GetEnvironmentVariable("NIMBBL_API_HOST");
    if (bool.TryParse(Environment.GetEnvironmentVariable("NIMBBL_ENABLE_LOGGING"), out var enableLog))
        enableLogging = enableLog;
    if (bool.TryParse(Environment.GetEnvironmentVariable("NIMBBL_DEBUG_LOGGING"), out var debugLog))
        debugLogging = debugLog;
    logFilePath = Environment.GetEnvironmentVariable("NIMBBL_LOG_FILE");
}

builder.Services.AddNimbbl(
    accessKey: accessKey,
    accessSecret: accessSecret,
    apiHost: apiHost,
    enableLogging: enableLogging,
    debugLogging: debugLogging,
    logFilePath: logFilePath);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseSession();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();
