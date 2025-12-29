# Nimbbl .NET SDK Integration Guide

This guide explains how to integrate Nimbbl V3 API into your ASP.NET Core application.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Installation](#installation)
3. [Configuration](#configuration)
4. [Basic Integration](#basic-integration)
5. [Create Order](#create-order)
6. [Launch Checkout](#launch-checkout)
7. [Handle Payment Callback](#handle-payment-callback)
8. [Webhook Handling](#webhook-handling)
9. [Error Handling](#error-handling)

## Prerequisites

- .NET 8.0 SDK or later
- Nimbbl merchant account with Access Key and Access Secret
- ASP.NET Core application

## Installation

Add the Nimbbl SDK to your project:

```xml
<ItemGroup>
  <ProjectReference Include="..\Nimbbl.Sdk.Rest\Nimbbl.Sdk.Rest.csproj" />
</ItemGroup>
```

## Configuration

### 1. Environment Variables

Set the following environment variables:

```bash
NIMBBL_ACCESS_KEY=your_access_key
NIMBBL_ACCESS_SECRET=your_access_secret
NIMBBL_API_HOST=https://api.nimbbl.tech  # Optional, defaults to production
NIMBBL_CHECKOUT_HOST=https://checkout.nimbbl.tech  # Optional
NIMBBL_ENABLE_LOGGING=true  # Optional
NIMBBL_DEBUG_LOGGING=false  # Optional
NIMBBL_LOG_FILE=logs/nimbbl.log  # Optional
```

### 2. Register SDK in Program.cs

```csharp
using Nimbbl.Sdk.Rest.Extensions;
using MerchantSampleApp.Services;

var builder = WebApplication.CreateBuilder(args);

var accessKey = Environment.GetEnvironmentVariable("NIMBBL_ACCESS_KEY")
    ?? throw new InvalidOperationException("NIMBBL_ACCESS_KEY is required");
var accessSecret = Environment.GetEnvironmentVariable("NIMBBL_ACCESS_SECRET")
    ?? throw new InvalidOperationException("NIMBBL_ACCESS_SECRET is required");

builder.Services.AddNimbbl(
    accessKey: accessKey,
    accessSecret: accessSecret,
    apiHost: Environment.GetEnvironmentVariable("NIMBBL_API_HOST"),
    enableLogging: bool.TryParse(Environment.GetEnvironmentVariable("NIMBBL_ENABLE_LOGGING"), out var enableLog) ? enableLog : null,
    debugLogging: bool.TryParse(Environment.GetEnvironmentVariable("NIMBBL_DEBUG_LOGGING"), out var debugLog) ? debugLog : null,
    logFilePath: Environment.GetEnvironmentVariable("NIMBBL_LOG_FILE"));
```

## Basic Integration

### Step 1: Generate Merchant Token

```csharp
public class HomeController : Controller
{
    private readonly NimbblApi _api;

    public HomeController(NimbblApi api)
    {
        _api = api;
    }

    public async Task<IActionResult> CreateOrder()
    {
        // Generate merchant token
        var tokenResponse = await _api.Auth().GenerateTokenAsync();
        var token = tokenResponse.TryGetProperty("token", out var tokenProp) 
            && tokenProp.ValueKind == JsonValueKind.String
            ? tokenProp.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest("Failed to generate token");
        }

        // Set bearer token for subsequent requests
        _api.SetBearerToken(token!);
        
        // Continue with order creation...
    }
}
```

## Create Order

### Step 2: Create Order

```csharp
var orderRequest = new Dictionary<string, object?>
{
    ["amount"] = 10000,  // Amount in paise (100.00 INR)
    ["total_amount"] = 10000,
    ["amount_before_tax"] = 10000,
    ["tax"] = 0,
    ["currency"] = "INR",
    ["merchant_order_id"] = $"order_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
    ["invoice_id"] = $"inv_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
    ["user"] = new Dictionary<string, object?>
    {
        ["first_name"] = "John",
        ["last_name"] = "Doe",
        ["email"] = "john.doe@example.com",
        ["mobile_number"] = "9876543210",
        ["country_code"] = "+91"
    },
    ["order_line_items"] = new List<Dictionary<string, object?>>
    {
        new Dictionary<string, object?>
        {
            ["title"] = "Product Name",
            ["description"] = "Product Description",
            ["quantity"] = 1,
            ["rate"] = 10000,
            ["amount"] = 10000,
            ["total_amount"] = 10000
        }
    }
};

// For redirect mode, add callback URL
if (mode == "redirect")
{
    orderRequest["callback_url"] = $"{Request.Scheme}://{Request.Host.Value}/payment-callback";
}

var order = await _api.Orders().CreateOrderAsync(orderRequest);

// Extract order token
var orderToken = order.TryGetProperty("token", out var ot) 
    && ot.ValueKind == JsonValueKind.String
    ? ot.GetString()
    : null;
```

## Launch Checkout

### Step 3: Launch Checkout

#### Popup Mode

```csharp
using MerchantSampleApp.NimbblCheckout;

var checkoutClient = new CheckoutClient();
var launchOptions = new CheckoutLaunchOptions
{
    OrderToken = orderToken!,
    Mode = "popup",
    CallbackHandler = @"async function(response) {
        // Handle callback
        console.log('Payment response:', response);
    }"
};

var checkoutScript = checkoutClient.LaunchCheckout(launchOptions);

// In your view:
@Html.Raw(Model.CheckoutScript)
```

#### Redirect Mode

```csharp
var launchOptions = new CheckoutLaunchOptions
{
    OrderToken = orderToken!,
    Mode = "redirect",
    CallbackBaseUrl = $"{Request.Scheme}://{Request.Host.Value}"
};

var checkoutScript = checkoutClient.LaunchCheckout(launchOptions);
```

## Handle Payment Callback

### Step 4: Payment Callback Handler

#### For Redirect Mode

```csharp
[Route("payment-callback")]
[HttpGet]
public IActionResult PaymentCallback()
{
    var responseParam = Request.Query["response"].ToString();

    if (string.IsNullOrWhiteSpace(responseParam))
    {
        return RedirectToAction("Index");
    }

    try
    {
        var parsed = PaymentResponseParser.ParseBase64Response(responseParam, verifySignature: true);

        if (parsed == null)
        {
            throw new Exception("Invalid response format");
        }

        if (parsed.Status == "success" || parsed.Status == "succeeded")
        {
            return RedirectToAction("PaymentSuccess", new { response = responseParam });
        }
        else
        {
            return RedirectToAction("PaymentFailed", new { response = responseParam });
        }
    }
    catch (Exception ex)
    {
        Logger.GetInstance().Exception($"Payment callback error: {ex.Message}", ex);
        return RedirectToAction("PaymentFailed", new { error = "1" });
    }
}
```

#### For Popup Mode

The callback handler is executed in JavaScript. You can post the response to your backend:

```javascript
async function(response) {
    try {
        const res = await fetch('/api/checkout-response', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(response)
        });
        const data = await res.json();
        // Handle response
    } catch (e) {
        console.error('Callback error:', e);
    }
}
```

## Webhook Handling

### Step 5: Webhook Endpoint

```csharp
[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly NimbblConfiguration _config;

    public WebhookController(NimbblConfiguration config)
    {
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var raw = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(raw))
        {
            return BadRequest(new { error = "Empty webhook payload" });
        }

        var accessSecret = _config.AccessSecret;
        var parsed = Util.VerifyAndParseWebhook(raw, accessSecret, out var parsedElement);

        if (!parsed.Success)
        {
            return BadRequest(new { error = parsed.Message });
        }

        // Process webhook event
        var eventType = parsedElement.TryGetProperty("event_type", out var et) 
            ? et.GetString() 
            : "unknown";

        switch (eventType)
        {
            case "order.paid":
                // Handle order paid
                break;
            case "transaction.completed":
                // Handle transaction completed
                break;
            // ... other event types
        }

        return Ok(new { received = true, event_type = eventType });
    }
}
```

## Error Handling

### Exception Handling

```csharp
try
{
    var order = await _api.Orders().CreateOrderAsync(orderRequest);
}
catch (NimbblException ex)
{
    // Handle Nimbbl-specific exceptions
    Logger.GetInstance().Error($"Nimbbl API error: {ex.Message}");
    Logger.GetInstance().Error($"Status Code: {ex.StatusCode}");
    return BadRequest(new { error = ex.Message });
}
catch (Exception ex)
{
    // Handle other exceptions
    Logger.GetInstance().Exception($"Unexpected error: {ex.Message}", ex);
    return StatusCode(500, new { error = "Internal server error" });
}
```

## Complete Example

See `WebFrameworkExamples/ASPNETMVCExample.cs` for a complete controller example.

## API Reference

For complete API reference, see the main SDK documentation.

## Support

For issues and questions, please contact Nimbbl support or refer to the SDK documentation.

