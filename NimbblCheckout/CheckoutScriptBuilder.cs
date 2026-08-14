using Microsoft.AspNetCore.Mvc;
using Nimbbl.Sdk.Rest.Common;
using NimbblDotnetSampleapp.Models;

namespace NimbblDotnetSampleapp.NimbblCheckout;

/// <summary>
/// Helper class for building checkout scripts and callback handlers
/// </summary>
public class CheckoutScriptBuilder
{
    private readonly IUrlHelper _urlHelper;
    private readonly HttpRequest _request;

    public CheckoutScriptBuilder(IUrlHelper urlHelper, HttpRequest request)
    {
        _urlHelper = urlHelper;
        _request = request;
    }

    /// <summary>
    /// Generate checkout script for the given order token and mode
    /// </summary>
    public string GenerateCheckoutScript(string orderToken, string mode)
    {
        if (string.IsNullOrWhiteSpace(orderToken))
            return string.Empty;

        var callbackBaseUrl = BuildCallbackBaseUrl();
        var launchOptions = new CheckoutLaunchOptions
        {
            OrderToken = orderToken,
            Mode = mode ?? CheckoutConstants.DefaultMode,
            CallbackBaseUrl = callbackBaseUrl
        };

        if (launchOptions.Mode == CheckoutConstants.ModePopup)
        {
            launchOptions.CallbackHandler = BuildPopupCallbackHandler(callbackBaseUrl);
        }

        return new CheckoutClient().LaunchCheckout(launchOptions);
    }

    /// <summary>
    /// Build the base URL for callbacks
    /// </summary>
    private string BuildCallbackBaseUrl()
    {
        return $"{_request.Scheme}://{_request.Host.Value}";
    }

    /// <summary>
    /// Build the JavaScript callback handler for popup mode
    /// </summary>
    private string BuildPopupCallbackHandler(string callbackBaseUrl)
    {
        var baseUrl = string.IsNullOrWhiteSpace(callbackBaseUrl) 
            ? $"{_request.Scheme}://{_request.Host.Value}" 
            : callbackBaseUrl.TrimEnd('/');
        // Option B: use /payment-callback for both redirect (GET) and popup (POST)
        var apiEndpoint = $"{baseUrl}/payment-callback";
        var successUrl = $"{baseUrl}{_urlHelper.Action("PaymentSuccess", "Home")}";
        var failedUrl = $"{baseUrl}{_urlHelper.Action("PaymentFailed", "Home")}";

        return $@"async function(response) {{
  try {{
    if (window.__nimbbl_callback_handled) {{
      return;
    }}
    if (window.__nimbbl_callback_processing) {{
      return;
    }}
    window.__nimbbl_callback_processing = true;
    if (!response) {{
      window.location.href = '{failedUrl}?error=1';
      return;
    }}
    
    let decodedResponse = response;
    try {{
      const res = await fetch('{apiEndpoint}', {{
        method: 'POST',
        headers: {{ 'Content-Type': 'application/json' }},
        body: JSON.stringify(response)
      }});

      if (!res.ok) {{
        throw new Error('CheckoutResponse request failed: ' + res.status);
      }}

      const data = await res.json();
      // Backend returns the parsed root directly, not wrapped in {{parsed: ...}}
      if (data) {{
        decodedResponse = data;
      }} else {{
        window.location.href = '{failedUrl}?error=1';
        return;
      }}
    }} catch (e) {{
      window.location.href = '{failedUrl}?error=1';
      return;
    }}
    
    // If another callback already redirected while we were awaiting the server, stop here.
    if (window.__nimbbl_callback_handled) {{
      return;
    }}

    if (!decodedResponse.payload) {{
      if (decodedResponse.status) {{
        decodedResponse = {{ payload: decodedResponse }};
      }} else {{
        window.location.href = '{failedUrl}?error=1';
        return;
      }}
    }}
    
    // Extract payload (matches PaymentResponseParser logic)
    const payload = decodedResponse.payload || decodedResponse;
    
    // Extract Order ID (matches PaymentResponseParser: only nimbbl_order_id, no fallback to order_id)
    const orderId = payload.nimbbl_order_id || '';
    
    // Extract Transaction ID only from inside transaction object (no fallback)
    let transactionId = null;
    if (payload.transaction && payload.transaction.transaction_id) {{
      transactionId = payload.transaction.transaction_id;
    }}
    
    // Extract Status (matches PaymentResponseParser: transaction.status first, then payload.status, default to 'unknown')
    // Always use transaction.status if available (this is the authoritative status)
    let status = null;
    if (payload.transaction && payload.transaction.status) {{
      status = payload.transaction.status;
    }}
    // Only fall back to payload.status if transaction.status was not found
    if (!status) {{
      status = payload.status || decodedResponse.status || 'unknown';
    }}
    
    // Extract Message (matches PaymentResponseParser: payload.message first, then root.message, default to empty)
    const message = payload.message || decodedResponse.message || '';
    
    const responseJson = JSON.stringify(decodedResponse);
    const encodedResponse = btoa(responseJson);
    
    // Check for success statuses (both ""success"" and ""succeeded"" indicate success)
    if (status === 'succeeded' || status === 'success') {{
      window.__nimbbl_callback_handled = true;
      let url = '{successUrl}?response=' + encodeURIComponent(encodedResponse);
      if (orderId) url += '&order_id=' + encodeURIComponent(orderId);
      if (transactionId) url += '&transaction_id=' + encodeURIComponent(transactionId);
      if (message) url += '&message=' + encodeURIComponent(message);
      window.location.href = url;
    }} else {{
      window.__nimbbl_callback_handled = true;
      let url = '{failedUrl}?response=' + encodeURIComponent(encodedResponse);
      if (orderId) url += '&order_id=' + encodeURIComponent(orderId);
      if (transactionId) url += '&transaction_id=' + encodeURIComponent(transactionId);
      if (message) url += '&message=' + encodeURIComponent(message);
      url += '&status=' + encodeURIComponent(status || 'failed');
      window.location.href = url;
    }}
  }} catch (e) {{
    window.location.href = '{failedUrl}?error=1';
  }} finally {{
    // If we didn't redirect, allow subsequent events to try.
    if (!window.__nimbbl_callback_handled) {{
      window.__nimbbl_callback_processing = false;
    }}
  }}
}}";
    }
}

