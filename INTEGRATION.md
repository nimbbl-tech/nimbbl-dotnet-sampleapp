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
8. [NimbblCheckout Folder Structure](#nimbblcheckout-folder-structure)
9. [Webhook Handling](#webhook-handling)
10. [Error Handling](#error-handling)

## Prerequisites

- .NET 8.0 SDK or later
- Nimbbl merchant account with Access Key and Access Secret
- ASP.NET Core application

## Installation

You can install the Nimbbl .NET SDK in two ways:

### Option 1: Install from NuGet (Recommended)

Install the SDK from NuGet package manager:

**Using .NET CLI:**

```bash
dotnet add package Nimbbl.Sdk.Rest
```

**Using Package Manager Console:**

```powershell
Install-Package Nimbbl.Sdk.Rest
```

**Using PackageReference in .csproj:**

```xml
<ItemGroup>
  <PackageReference Include="Nimbbl.Sdk.Rest" Version="1.3.5-rc3" />
</ItemGroup>
```

**Note:** Replace `1.3.5-rc3` with the latest version available on [NuGet](https://www.nuget.org/packages/Nimbbl.Sdk.Rest). You can also omit the version to use the latest available version.

**Using Package Manager UI:**

1. Right-click on your project in Visual Studio
2. Select "Manage NuGet Packages"
3. Search for "Nimbbl.Sdk.Rest"
4. Click "Install"

### Option 2: Install from Source Code

If you want to use the source code directly or make custom modifications:

1. **Clone the repository:**

   ```bash
   git clone https://github.com/nimbbl-tech/nimbbl-dotnet-sdk.git
   cd nimbbl-dotnet-sdk
   ```

2. **Add project reference in your .csproj:**

   ```xml
   <ItemGroup>
     <ProjectReference Include="path/to/Nimbbl.Sdk.Rest/Nimbbl.Sdk.Rest.csproj" />
   </ItemGroup>
   ```

   Or if the SDK is in a sibling directory:

   ```xml
   <ItemGroup>
     <ProjectReference Include="..\nimbbl-dotnet-sdk\Nimbbl.Sdk.Rest\Nimbbl.Sdk.Rest.csproj" />
   </ItemGroup>
   ```

3. **Build the SDK:**

   ```bash
   cd Nimbbl.Sdk.Rest
   dotnet build
   ```

**Note:** When using source code, ensure you have the same .NET version (8.0 or later) as the SDK project.

## Configuration

### 1. Environment Variables

Set the following environment variables:

```bash
NIMBBL_ACCESS_KEY=your_access_key
NIMBBL_ACCESS_SECRET=your_access_secret
```

### 2. Register SDK in Program.cs

```csharp
using Nimbbl.Sdk.Rest.Extensions;
using NimbblDotnetSampleapp.Services;

var builder = WebApplication.CreateBuilder(args);

var accessKey = Environment.GetEnvironmentVariable("NIMBBL_ACCESS_KEY")
    ?? throw new InvalidOperationException("NIMBBL_ACCESS_KEY is required");
var accessSecret = Environment.GetEnvironmentVariable("NIMBBL_ACCESS_SECRET")
    ?? throw new InvalidOperationException("NIMBBL_ACCESS_SECRET is required");

builder.Services.AddNimbbl(
    accessKey: accessKey,
    accessSecret: accessSecret);
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
    ["total_amount"] = 100.0,
    ["amount_before_tax"] = 100.0,
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
            ["rate"] = 100.0,
            ["total_amount"] = 100.0,
            ["amount_before_tax"] = 100.0,
            ["tax"] = 0
        }
    }
};

// For redirect mode, add callback URL
if (mode == "redirect")
{
    orderRequest["callback_url"] = "your_shared_callback_url";
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

The sample app provides helper classes in the `NimbblCheckout` folder to simplify checkout integration:

- **`CheckoutClient`** - Generates JavaScript code to launch checkout
- **`CheckoutScriptBuilder`** - Builds checkout scripts with callback handlers
- **`PaymentResponseParser`** - Parses and decrypts payment responses
- **`CheckoutConstants`** - Constants for checkout configuration

#### Using CheckoutScriptBuilder (Recommended)

The `CheckoutScriptBuilder` class simplifies checkout script generation for both popup and redirect modes:

**Popup Mode:**

```csharp
using NimbblDotnetSampleapp.NimbblCheckout;

public class HomeController : Controller
{
    public async Task<IActionResult> CreateOrder()
    {
        // ... create order and get orderToken ...
        
        var scriptBuilder = new CheckoutScriptBuilder(Url, Request);
        var checkoutScript = scriptBuilder.GenerateCheckoutScript(orderToken, "popup");
        
        // Pass to view model
        model.CheckoutScript = checkoutScript;
        return View(model);
    }
}
```

**Redirect Mode:**

```csharp
public async Task<IActionResult> CreateOrder()
{
    // ... create order and get orderToken ...
    // Note: For redirect mode, add callback_url when creating the order
    
    var scriptBuilder = new CheckoutScriptBuilder(Url, Request);
    var checkoutScript = scriptBuilder.GenerateCheckoutScript(orderToken, "redirect");
    
    // Pass to view model
    model.CheckoutScript = checkoutScript;
    return View(model);
}
```

#### Using CheckoutClient Directly

For more control, use `CheckoutClient` directly:

**Popup Mode:**

```csharp
using NimbblDotnetSampleapp.NimbblCheckout;

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

**Redirect Mode:**

In redirect mode, the user is redirected to Nimbbl's checkout page. After payment, they are redirected back to your callback URL.

```csharp
// Step 1: Create order with callback_url (required for redirect mode)
// Important: callback_url must be publicly accessible (not localhost)
var orderRequest = new Dictionary<string, object?>
{
    // ... order details ...
    ["callback_url"] = $"{Request.Scheme}://{Request.Host.Value}/payment-callback"
};

var order = await _api.Orders().CreateOrderAsync(orderRequest);
var orderToken = order.TryGetProperty("token", out var ot) ? ot.GetString() : null;

// Step 2: Generate checkout script for redirect mode
var checkoutClient = new CheckoutClient();
var launchOptions = new CheckoutLaunchOptions
{
    OrderToken = orderToken!,
    Mode = CheckoutConstants.ModeRedirect,
    CallbackBaseUrl = $"{Request.Scheme}://{Request.Host.Value}"
};

var checkoutScript = checkoutClient.LaunchCheckout(launchOptions);

// Step 3: Render script in view - this will redirect user to Nimbbl checkout
@Html.Raw(Model.CheckoutScript)
```

**How Redirect Mode Works:**

1. User clicks "Pay Now" button
2. JavaScript executes and redirects user to Nimbbl checkout page
3. User completes payment on Nimbbl's page
4. Nimbbl redirects back to your `callback_url` with payment response
5. Your `PaymentCallback` action handles the response and redirects to success/failure page

**Important:** The `callback_url` must be publicly accessible (not `localhost`).

#### CheckoutLaunchOptions

The `CheckoutLaunchOptions` class provides the following options:

```csharp
public class CheckoutLaunchOptions
{
    public string OrderToken { get; set; }              // Required: Order token from create order
    public string? PaymentModeCode { get; set; }       // "net_banking", "wallet", "card", "upi", "emi", or "allpayment"
    public string? SubPaymentMode { get; set; }        // Bank code, wallet code, etc.
    public string Mode { get; set; }                    // "popup" or "redirect" (default: "popup")
    public string? CallbackBaseUrl { get; set; }        // Required for redirect mode
    public string? CallbackHandler { get; set; }       // Required for popup mode (JavaScript function as string)
}
```

#### CheckoutConstants

Use constants from `CheckoutConstants` for consistent values:

```csharp
using static NimbblDotnetSampleapp.NimbblCheckout.CheckoutConstants;

// Payment modes
var mode = PaymentModeNetBanking;  // "net_banking"
var mode = PaymentModeWallet;      // "wallet"
var mode = PaymentModeUpi;         // "upi"

// Checkout modes
var checkoutMode = ModePopup;      // "popup"
var checkoutMode = ModeRedirect;   // "redirect"

// Payment status
if (status == PaymentStatusSuccess || status == PaymentStatusSucceeded) {
    // Handle success
}
```

## Handle Payment Callback

### Step 4: Payment Callback Handler

The `PaymentResponseParser` class handles parsing and decrypting payment responses from Nimbbl checkout. This is used for **redirect mode** callbacks.

#### For Redirect Mode

In redirect mode, Nimbbl redirects the user back to your `callback_url` with a `response` query parameter containing the payment result.

```csharp
using NimbblDotnetSampleapp.NimbblCheckout;

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
        // Parse and optionally verify signature
        var parsed = PaymentResponseParser.ParseBase64Response(responseParam, verifySignature: true);

        if (parsed == null)
        {
            throw new Exception("Invalid response format");
        }

        // Check signature validity (if verification was enabled)
        if (parsed.SignatureValid == false)
        {
            Logger.GetInstance().WarningWithCaller($"Signature verification failed: {parsed.SignatureMessage}");
            // Handle invalid signature (log, alert, etc.)
        }

        // Redirect based on payment status
        if (parsed.Status == CheckoutConstants.PaymentStatusSuccess || 
            parsed.Status == CheckoutConstants.PaymentStatusSucceeded)
        {
            return RedirectToAction("PaymentSuccess", new { 
                response = responseParam,
                order_id = parsed.OrderId,
                transaction_id = parsed.TransactionId
            });
        }
        else
        {
            return RedirectToAction("PaymentFailed", new { 
                response = responseParam,
                order_id = parsed.OrderId,
                transaction_id = parsed.TransactionId,
                status = parsed.Status
            });
        }
    }
    catch (Exception ex)
    {
        Logger.GetInstance().ExceptionWithCaller($"Payment callback error: {ex.Message}", ex);
        return RedirectToAction("PaymentFailed", new { error = "1" });
    }
}
```

#### Redirect Mode Flow

1. **User initiates payment** → Checkout script redirects to Nimbbl checkout page
2. **User completes payment** → Nimbbl processes the payment
3. **Nimbbl redirects back** → User is redirected to your `callback_url` with `?response=<base64_encoded_response>`
4. **Your callback handler** → `PaymentCallback()` action receives the response
5. **Parse response** → `PaymentResponseParser` decodes and decrypts the response
6. **Redirect to result page** → User is redirected to `PaymentSuccess` or `PaymentFailed` page

#### Configuring Callback URL for Redirect Mode

When creating an order for redirect mode, you must include the `callback_url`:

```csharp
var orderRequest = new Dictionary<string, object?>
{
    // ... other order fields ...
    
    // Required for redirect mode
    ["callback_url"] = $"{Request.Scheme}://{Request.Host.Value}/payment-callback"
};

var order = await _api.Orders().CreateOrderAsync(orderRequest);
```

**Important Notes:**

- The `callback_url` must be publicly accessible
- Use HTTPS in production
- The callback URL should point to your `PaymentCallback` action
- The response parameter is base64-encoded and may be encrypted

#### ParsedResponse Properties

The `PaymentResponseParser` returns a `ParsedResponse` object with the following properties:

```csharp
public class ParsedResponse
{
    public string? OrderId { get; set; }              // Nimbbl order ID
    public string? TransactionId { get; set; }       // Transaction ID
    public string Status { get; set; }                // Payment status: "success", "succeeded", "failed", etc.
    public string Message { get; set; }              // Status message
    public string Reason { get; set; }                // Failure reason (if failed)
    public long? Amount { get; set; }                 // Amount (may be in smallest currency unit from API response)
    public string Currency { get; set; }             // Currency code (e.g., "INR")
    public string PaymentMode { get; set; }          // Payment mode used
    public string? UserName { get; set; }            // User name from payment
    public bool SignatureValid { get; set; }        // Whether signature verification passed
    public string? SignatureMessage { get; set; }    // Signature verification message
}
```

#### Handling Encrypted Responses

The `PaymentResponseParser` automatically handles encrypted responses:

```csharp
// The parser automatically detects and decrypts encrypted_response
var parsed = PaymentResponseParser.ParseBase64Response(responseParam);

// If the response contains encrypted_response, it will be:
// 1. Detected automatically
// 2. Decrypted using your access secret
// 3. Parsed to extract payment data
```

#### For Popup Mode

In popup mode, the payment happens in a popup window, and the callback handler is executed in JavaScript.

**How Popup Mode Works:**

1. User clicks "Pay Now" button
2. JavaScript opens Nimbbl checkout in a popup window
3. User completes payment in the popup
4. Popup closes and callback handler is executed
5. Callback handler processes the response and redirects to success/failure page

The `CheckoutScriptBuilder` automatically creates a callback handler that:

1. Handles encrypted responses (decrypts via `POST /payment-callback`)
2. Extracts payment data
3. Redirects to success/failure pages

**Automatic Callback Handler (Generated by CheckoutScriptBuilder):**

The `CheckoutScriptBuilder` automatically generates a callback handler that:

- Detects encrypted responses
- Calls `POST /payment-callback` to decrypt/parse (and verify signature) if needed
- Extracts payment status, order ID, transaction ID
- Redirects to `PaymentSuccess` or `PaymentFailed` pages with response data

**Custom Callback Handler:**

You can also create a custom callback handler:

```csharp
var launchOptions = new CheckoutLaunchOptions
{
    OrderToken = orderToken!,
    Mode = CheckoutConstants.ModePopup,
    CallbackHandler = @"async function(response) {
        try {
            // Handle encrypted response
            const encryptedResponse = response?.payload?.encrypted_response;
            if (encryptedResponse) {
                const res = await fetch('/payment-callback', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        encrypted_response: encryptedResponse,
                        callback: response
                    })
                });
                const data = await res.json();
                response = data.parsed ? data.parsed : response;
            }
            
            // Extract payment data
            const payload = response.payload || response;
            const status = payload.status || 'failed';
            const orderId = payload.nimbbl_order_id || payload.order_id;
            
            // Redirect based on status
            if (status === 'success' || status === 'succeeded') {
                window.location.href = '/Home/PaymentSuccess?order_id=' + orderId;
            } else {
                window.location.href = '/Home/PaymentFailed?order_id=' + orderId;
            }
        } catch (e) {
            console.error('Callback error:', e);
            window.location.href = '/Home/PaymentFailed?error=1';
        }
    }"
};
```

#### Popup vs Redirect Mode Comparison

| Feature | Popup Mode | Redirect Mode |
|---------|-----------|---------------|
| **User Experience** | Payment in popup window | Full page redirect |
| **Callback Handler** | JavaScript function | Server-side action |
| **Callback URL** | Not required in order | Required in order (`callback_url`) |
| **Response Handling** | Client-side JavaScript + server parse (`POST /payment-callback`) | Server-side C# (`GET /payment-callback`) |
| **Encryption** | Decrypted server-side (if present) | Decrypted server-side (if present) |
| **Best For** | Seamless UX, stay on page | Simple integration, full control |

## NimbblCheckout Folder Structure

The `NimbblCheckout` folder contains helper classes that simplify checkout integration:

### CheckoutClient.cs

Generates JavaScript code to launch Nimbbl checkout. Use this when you need full control over checkout options.

**Key Methods:**

- `LaunchCheckout(CheckoutLaunchOptions options)` - Generates checkout script

**Example:**

```csharp
var client = new CheckoutClient();
var script = client.LaunchCheckout(new CheckoutLaunchOptions
{
    OrderToken = orderToken,
    Mode = "popup",
    CallbackHandler = "async function(response) { /* handle */ }"
});
```

### CheckoutScriptBuilder.cs

Simplifies checkout script generation by automatically building callback handlers and URLs for both popup and redirect modes.

**Key Methods:**

- `GenerateCheckoutScript(string orderToken, string mode)` - Generates complete checkout script
- `BuildCallbackBaseUrl()` - Builds base URL for callbacks
- `BuildPopupCallbackHandler(string callbackBaseUrl)` - Creates JavaScript callback handler for popup mode

**Example - Popup Mode:**

```csharp
var builder = new CheckoutScriptBuilder(Url, Request);
var script = builder.GenerateCheckoutScript(orderToken, "popup");
// Automatically creates callback handler for popup mode
```

**Example - Redirect Mode:**

```csharp
var builder = new CheckoutScriptBuilder(Url, Request);
var script = builder.GenerateCheckoutScript(orderToken, "redirect");
// Automatically sets callback URL for redirect mode
// Note: Make sure to include callback_url when creating the order
```

### PaymentResponseParser.cs

Parses and decrypts payment responses from Nimbbl checkout.

**Key Methods:**

- `ParseBase64Response(string base64Response, bool verifySignature)` - Parses base64-encoded response
- `ParseResponse(string jsonResponse, bool verifySignature)` - Parses JSON response string

**Features:**

- Automatic encrypted response detection and decryption
- Signature verification support
- Extracts order ID, transaction ID, status, amount, etc.

**Example:**

```csharp
var parsed = PaymentResponseParser.ParseBase64Response(responseParam, verifySignature: true);
if (parsed?.Status == CheckoutConstants.PaymentStatusSuccess) {
    // Handle success
}
```

### CheckoutConstants.cs

Contains constants for checkout configuration to avoid magic strings.

**Constants:**

- Payment modes: `PaymentModeAll`, `PaymentModeNetBanking`, `PaymentModeWallet`, `PaymentModeUpi`, `PaymentModeEmi`
- Checkout modes: `ModePopup`, `ModeRedirect`
- Payment status: `PaymentStatusSuccess`, `PaymentStatusSucceeded`, `PaymentStatusFailed`
- Configuration keys: `ConfigKeyToken`, `ConfigKeyApiHost`, `ConfigKeyCheckoutHost`

**Example:**

```csharp
using static NimbblDotnetSampleapp.NimbblCheckout.CheckoutConstants;

if (status == PaymentStatusSuccess) {
    // Handle success
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
    Logger.GetInstance().ErrorWithCaller($"Nimbbl API error: {ex.Message}");
    Logger.GetInstance().ErrorWithCaller($"Status Code: {ex.StatusCode}");
    return BadRequest(new { error = ex.Message });
}
catch (Exception ex)
{
    // Handle other exceptions
    Logger.GetInstance().ExceptionWithCaller($"Unexpected error: {ex.Message}", ex);
    return StatusCode(500, new { error = "Internal server error" });
}
```

## Framework-Specific Examples

### ASP.NET Core MVC

See `WebFrameworkExamples/ASPNETMVCExample.cs` for a complete controller example.

### ASP.NET Web Forms

For ASP.NET Web Forms integration, see the complete example in `WebFrameworkExamples/ASPNETWebForms/` folder.

#### How to Open Checkout Page from Your Existing ASP.NET Web Forms Project

To integrate Nimbbl checkout into your existing ASP.NET Web Forms project:

1. **Install Nimbbl SDK** (if not already installed):

   ```powershell
   Install-Package Nimbbl.Sdk.Rest
   ```

2. **Add Configuration to `web.config`**:

   ```xml
   <appSettings>
     <add key="NimbblAccessKey" value="your_access_key" />
     <add key="NimbblAccessSecret" value="your_access_secret" />
   </appSettings>
   ```

3. **In your Web Forms page code-behind**, use this pattern:

   **Step 1: Generate token and create order**

   ```csharp
   var api = new NimbblApi(
       ConfigurationManager.AppSettings["NimbblAccessKey"],
       ConfigurationManager.AppSettings["NimbblAccessSecret"]
   );
   
   var tokenResponse = await api.Auth().GenerateTokenAsync();
   var token = tokenResponse.TryGetProperty("token", out var tokenProp) 
       ? tokenProp.GetString() 
       : null;
   
   api.SetBearerToken(token!);
   
   var orderRequest = new Dictionary<string, object?>
   {
       ["total_amount"] = 100.0,  // Amount as double
       ["currency"] = "INR",
       ["user"] = new Dictionary<string, object?>
       {
           ["first_name"] = "John",
           ["email"] = "john@example.com",
           ["mobile_number"] = "9999999999",
           ["country_code"] = "+91"
       }
   };
   
   var order = await api.Orders().CreateOrderAsync(orderRequest);
   var orderToken = order.TryGetProperty("token", out var ot) ? ot.GetString() : null;
   ```

   **Step 2: Generate and inject checkout script**

   ```csharp
   var script = $@"<script type=""module"">
   import Checkout from ""https://cdn.jsdelivr.net/npm/nimbbl_sonic@latest"";
   const checkout = new Checkout({{ token: ""{orderToken}"" }});
   checkout.open({{}});
   </script>";
   
   checkoutScriptPlaceholder.Controls.Add(new LiteralControl(script));
   ```

4. **In your `.aspx` markup**, add a placeholder for the script:

   ```aspx
   <asp:PlaceHolder ID="checkoutScriptPlaceholder" runat="server" />
   ```

5. **For redirect mode**, add `callback_url` to order request and set it in checkout options:

   ```csharp
   orderRequest["callback_url"] = $"{Request.Url.Scheme}://{Request.Url.Authority}/PaymentCallback.aspx";
   
   // In checkout script:
   options.callback_url = '{Request.Url.Scheme}://{Request.Url.Authority}/PaymentCallback.aspx';
   ```

   **Important:** The `callback_url` must be publicly accessible (not `localhost`).

#### Detailed Examples

The following sections provide complete examples for popup and redirect modes.

#### Create Order in Code-Behind

##### Popup Mode

In popup mode, the payment happens in a popup window. The callback handler is executed in JavaScript.

```csharp
protected async void BtnPayNow_Click(object sender, EventArgs e)
{
    try
    {
        var api = GetNimbblApi();
        
        // Step 1: Generate merchant token
        var tokenResponse = await api.Auth().GenerateTokenAsync();
        var token = tokenResponse.TryGetProperty("token", out var tokenProp) 
            && tokenProp.ValueKind == JsonValueKind.String
            ? tokenProp.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(token))
        {
            lblError.Text = "Failed to generate merchant token";
            lblError.Visible = true;
            return;
        }

        // Step 2: Set bearer token
        api.SetBearerToken(token!);

        // Step 3: Create order (no callback_url needed for popup mode)
        var amount = decimal.Parse(txtAmount.Text);
        var totalAmount = (double)amount;

        var orderRequest = new Dictionary<string, object?>
        {
            ["total_amount"] = totalAmount,
            ["currency"] = ddlCurrency.SelectedValue,
            ["merchant_order_id"] = $"order_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            ["user"] = new Dictionary<string, object?>
            {
                ["first_name"] = txtUserName.Text,
                ["email"] = txtEmail.Text,
                ["mobile_number"] = txtMobile.Text,
                ["country_code"] = "+91"
            }
        };

        var order = await api.Orders().CreateOrderAsync(orderRequest);
        var orderToken = order.TryGetProperty("token", out var ot) 
            ? ot.GetString() 
            : null;

        // Step 4: Generate checkout script with callback handler for popup mode
        var callbackBaseUrl = $"{Request.Url.Scheme}://{Request.Url.Authority}";
        var script = $@"<script type=""module"">
import Checkout from ""https://cdn.jsdelivr.net/npm/nimbbl_sonic@latest"";
const checkout = new Checkout({{ token: ""{orderToken}"" }});
const options = {{}};
options.callback_handler = async function(response) {{
    try {{
        // Handle encrypted response if present
        const encryptedResponse = response?.payload?.encrypted_response;
        if (encryptedResponse) {{
            const res = await fetch('{callbackBaseUrl}/payment-callback', {{
                method: 'POST',
                headers: {{ 'Content-Type': 'application/json' }},
                body: JSON.stringify({{
                    encrypted_response: encryptedResponse,
                    callback: response
                }})
            }});
            const data = await res.json();
            response = data.parsed ? data.parsed : response;
        }}
        
        // Extract payment data
        const payload = response.payload || response;
        const status = payload.status || 'failed';
        const orderId = payload.nimbbl_order_id || payload.order_id;
        const transactionId = payload.nimbbl_transaction_id || payload.transaction_id;
        const responseParam = btoa(JSON.stringify(response));
        
        // Redirect based on status
        if (status === 'success' || status === 'succeeded') {{
            window.location.href = '{callbackBaseUrl}/PaymentSuccess.aspx?order_id=' + orderId + '&transaction_id=' + transactionId + '&response=' + encodeURIComponent(responseParam);
        }} else {{
            window.location.href = '{callbackBaseUrl}/PaymentFailed.aspx?order_id=' + orderId + '&transaction_id=' + transactionId + '&status=' + status + '&response=' + encodeURIComponent(responseParam);
        }}
    }} catch (e) {{
        console.error('Callback error:', e);
        window.location.href = '{callbackBaseUrl}/PaymentFailed.aspx?error=1';
    }}
}};
checkout.open(options);
</script>";

        // Inject script into page
        checkoutScriptPlaceholder.Controls.Add(new LiteralControl(script));
    }
    catch (Exception ex)
    {
        lblError.Text = $"Error: {ex.Message}";
        lblError.Visible = true;
    }
}
```

##### Redirect Mode

In redirect mode, the user is redirected to Nimbbl's checkout page. After payment, they are redirected back to your callback URL.

**Important:** The `callback_url` must be publicly accessible (not `localhost`).

```csharp
protected async void BtnPayNow_Click(object sender, EventArgs e)
{
    try
    {
        var api = GetNimbblApi();
        
        // Step 1: Generate merchant token
        var tokenResponse = await api.Auth().GenerateTokenAsync();
        var token = tokenResponse.TryGetProperty("token", out var tokenProp) 
            && tokenProp.ValueKind == JsonValueKind.String
            ? tokenProp.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(token))
        {
            lblError.Text = "Failed to generate merchant token";
            lblError.Visible = true;
            return;
        }

        // Step 2: Set bearer token
        api.SetBearerToken(token!);

        // Step 3: Create order with callback_url (required for redirect mode)
        // Note: callback_url must be publicly accessible (not localhost)
        var amount = decimal.Parse(txtAmount.Text);
        var totalAmount = (double)amount;

        var orderRequest = new Dictionary<string, object?>
        {
            ["total_amount"] = totalAmount,
            ["currency"] = ddlCurrency.SelectedValue,
            ["merchant_order_id"] = $"order_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            ["callback_url"] = $"{Request.Url.Scheme}://{Request.Url.Authority}/PaymentCallback.aspx",
            ["user"] = new Dictionary<string, object?>
            {
                ["first_name"] = txtUserName.Text,
                ["email"] = txtEmail.Text,
                ["mobile_number"] = txtMobile.Text,
                ["country_code"] = "+91"
            }
        };

        var order = await api.Orders().CreateOrderAsync(orderRequest);
        var orderToken = order.TryGetProperty("token", out var ot) 
            ? ot.GetString() 
            : null;

        // Step 4: Generate checkout script for redirect mode
        var callbackBaseUrl = $"{Request.Url.Scheme}://{Request.Url.Authority}";
        var script = $@"<script type=""module"">
import Checkout from ""https://cdn.jsdelivr.net/npm/nimbbl_sonic@latest"";
const checkout = new Checkout({{ token: ""{orderToken}"" }});
const options = {{}};
options.callback_url = '{callbackBaseUrl}/PaymentCallback.aspx';
checkout.open(options);
</script>";

        // Inject script into page
        checkoutScriptPlaceholder.Controls.Add(new LiteralControl(script));
    }
    catch (Exception ex)
    {
        lblError.Text = $"Error: {ex.Message}";
        lblError.Visible = true;
    }
}
```

**How Redirect Mode Works:**

1. User clicks "Pay Now" button
2. JavaScript executes and redirects user to Nimbbl checkout page
3. User completes payment on Nimbbl's page
4. Nimbbl redirects back to your `callback_url` with payment response
5. Your `PaymentCallback.aspx` page handles the response and redirects to success/failure page

**Important:** The `callback_url` must be publicly accessible (not `localhost`).

#### Handle Payment Callback in Web Forms

Create a `PaymentCallback.aspx` page:

```csharp
protected void Page_Load(object sender, EventArgs e)
{
    var responseParam = Request.QueryString["response"];

    if (string.IsNullOrWhiteSpace(responseParam))
    {
        Response.Redirect("PaymentPage.aspx");
        return;
    }

    try
    {
        // Decode base64 response
        var decodedBytes = Convert.FromBase64String(responseParam);
        var decodedString = System.Text.Encoding.UTF8.GetString(decodedBytes);
        
        // Parse JSON
        var jsonDoc = System.Text.Json.JsonDocument.Parse(decodedString);
        var root = jsonDoc.RootElement;
        
        var status = root.TryGetProperty("status", out var statusProp) 
            ? statusProp.GetString() 
            : "unknown";
        
        var orderId = root.TryGetProperty("nimbbl_order_id", out var orderProp) 
            ? orderProp.GetString() 
            : null;

        // Redirect based on status
        if (status == "success" || status == "succeeded")
        {
            Response.Redirect($"PaymentSuccess.aspx?order_id={orderId}&response={Server.UrlEncode(responseParam)}");
        }
        else
        {
            Response.Redirect($"PaymentFailed.aspx?order_id={orderId}&status={status}&response={Server.UrlEncode(responseParam)}");
        }
    }
    catch (Exception ex)
    {
        Response.Redirect("PaymentFailed.aspx?error=1");
    }
}
```

#### Complete Example Files

See the complete working example in:

- `WebFrameworkExamples/ASPNETWebForms/ASPNETWebFormsExample.aspx` - Main payment page
- `WebFrameworkExamples/ASPNETWebForms/ASPNETWebFormsExample.aspx.cs` - Code-behind
- `WebFrameworkExamples/ASPNETWebForms/PaymentCallback.aspx` - Callback handler page
- `WebFrameworkExamples/ASPNETWebForms/PaymentCallback.aspx.cs` - Callback code-behind

#### Key Differences from MVC

| Aspect | ASP.NET MVC | ASP.NET Web Forms |
|--------|-------------|-------------------|
| **Architecture** | Controller-based | Page-based with code-behind |
| **Dependency Injection** | Built-in DI support | Manual instantiation or service locator |
| **Configuration** | `appsettings.json` or environment variables | `web.config` appSettings |
| **View Rendering** | Razor views (`.cshtml`) | ASPX pages (`.aspx`) |
| **Script Injection** | `@Html.Raw()` in view | `LiteralControl` in code-behind |
| **Callback Handling** | Controller action | Page `Page_Load` event |

#### Notes for Web Forms

- **Async Event Handlers**: Use `async void` for event handlers (e.g., `BtnPayNow_Click`). This is acceptable in Web Forms but handle exceptions carefully.
- **Configuration**: Store credentials in `web.config` `<appSettings>` section.
- **Script Injection**: Use `PlaceHolder` control with `LiteralControl` to inject checkout scripts dynamically.

## API Reference

For complete API reference, see the main SDK documentation.

## Support

For issues and questions, please contact Nimbbl support or refer to the SDK documentation.

