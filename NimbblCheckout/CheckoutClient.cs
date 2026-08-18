using System;
using System.Collections.Generic;
using System.Text.Json;
using NimbblDotnetSampleapp.Services;

namespace NimbblDotnetSampleapp.NimbblCheckout;

/// <summary>
/// Options for simplified checkout launch
/// </summary>
public class CheckoutLaunchOptions
{
    /// <summary>
    /// Order token from create order
    /// </summary>
    public string OrderToken { get; set; } = string.Empty;

    /// <summary>
    /// Payment mode code (e.g., "net_banking", "wallet", "card", "upi", "emi", or "allpayment")
    /// </summary>
    public string? PaymentModeCode { get; set; }

    /// <summary>
    /// Sub-payment mode (bank_code, wallet_code, payment_flow, or emi_code based on payment mode)
    /// </summary>
    public string? SubPaymentMode { get; set; }

    /// <summary>
    /// Checkout mode: "popup" or "redirect" (default: "popup")
    /// </summary>
    public string Mode { get; set; } = CheckoutConstants.DefaultMode;

    /// <summary>
    /// Base URL for building callback URLs (e.g., "https://example.com" or "http://localhost:5000")
    /// Required for redirect mode, optional for popup mode
    /// </summary>
    public string? CallbackBaseUrl { get; set; }

    /// <summary>
    /// Optional callback handler function (JavaScript code as string) for popup mode.
    /// If not provided, sample app should handle the callback logic.
    /// </summary>
    public string? CallbackHandler { get; set; }
}

/// <summary>
/// Helper class for launching Nimbbl checkout in the Merchant Sample App.
/// Builds launcher URLs and inline checkout scripts for quick embedding.
/// </summary>
public class CheckoutClient
{
    /// <summary>
    /// Generate inline JavaScript to launch checkout directly on the page.
    /// </summary>
    /// <param name="options">Checkout launch options</param>
    /// <returns>HTML script tag with inline JavaScript to launch checkout</returns>
    public string LaunchCheckout(CheckoutLaunchOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.OrderToken))
            throw new ArgumentException("OrderToken is required", nameof(options));

        if (options.Mode == CheckoutConstants.ModePopup && string.IsNullOrWhiteSpace(options.CallbackHandler))
            throw new ArgumentException("CallbackHandler is required when mode is popup", nameof(options));

        return RenderInlineLauncher(options);
    }

    private string RenderInlineLauncher(CheckoutLaunchOptions options)
    {
        var checkoutConfig = new Dictionary<string, object?>
        {
            [CheckoutConstants.ConfigKeyToken] = options.OrderToken
        };

        // Always forward apiHost/checkoutHost to the JS SDK when configured (parity with the PHP
        // sample app, which always passes them). Without these the SDK defaults to PRODUCTION hosts
        // (sonic.nimbbl.tech + api.nimbbl.tech), so a UAT/QA order token would open on the prod
        // checkout and fail. For UAT (apipp) set NIMBBL_CHECKOUT_HOST=https://sonicpp.nimbbl.tech.
        var apiHost = Environment.GetEnvironmentVariable("NIMBBL_API_HOST");
        var checkoutHost = Environment.GetEnvironmentVariable("NIMBBL_CHECKOUT_HOST");

        if (!string.IsNullOrWhiteSpace(apiHost))
            checkoutConfig[CheckoutConstants.ConfigKeyApiHost] = apiHost;

        if (!string.IsNullOrWhiteSpace(checkoutHost))
            checkoutConfig[CheckoutConstants.ConfigKeyCheckoutHost] = checkoutHost;

        var checkoutConfigJson = JsonSerializer.Serialize(checkoutConfig, new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        var jsOptions = new Dictionary<string, object?>();

        if (options.Mode == CheckoutConstants.ModeRedirect && !string.IsNullOrWhiteSpace(options.CallbackBaseUrl))
        {
            jsOptions[CheckoutConstants.OptionKeyCallbackUrl] = $"{options.CallbackBaseUrl.TrimEnd('/')}/payment-callback";
        }

        if (!string.IsNullOrWhiteSpace(options.PaymentModeCode))
        {
            jsOptions[CheckoutConstants.OptionKeyPaymentModeCode] = options.PaymentModeCode;
            if (!string.IsNullOrWhiteSpace(options.SubPaymentMode))
                jsOptions[CheckoutConstants.OptionKeySubPaymentMode] = options.SubPaymentMode;
        }

        var optionsJson = JsonSerializer.Serialize(jsOptions, new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        var callbackHandlerCode = string.Empty;
        if (options.Mode == CheckoutConstants.ModePopup && !string.IsNullOrWhiteSpace(options.CallbackHandler))
        {
            callbackHandlerCode = $@"
options.callback_handler = {options.CallbackHandler};";
        }

        return $@"<script type=""module"">
import Checkout from ""https://cdn.jsdelivr.net/npm/nimbbl_sonic@latest/+esm"";
const checkout = new Checkout({checkoutConfigJson});
const options = {optionsJson} || {{}};{callbackHandlerCode}
checkout.open(options);
</script>";
    }
}

