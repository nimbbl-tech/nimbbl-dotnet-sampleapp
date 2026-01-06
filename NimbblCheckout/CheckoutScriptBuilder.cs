using Microsoft.AspNetCore.Mvc;
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
    const encryptedResponse = response?.payload?.encrypted_response;
    try {{
      const res = await fetch('{apiEndpoint}', {{
        method: 'POST',
        headers: {{ 'Content-Type': 'application/json' }},
        body: JSON.stringify({{
          encrypted_response: encryptedResponse || null,
          callback: response
        }})
      }});

      if (!res.ok) {{
        throw new Error('CheckoutResponse request failed: ' + res.status);
      }}

      const data = await res.json();
      if (data.parsed) {{
        decodedResponse = data.parsed;
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
    
    const payload = decodedResponse.payload || decodedResponse;
    
    // Extract status - check nested transaction and order status as well
    let status = payload.status || decodedResponse.status;
    if (!status && payload.transaction && payload.transaction.status) {{
      status = payload.transaction.status;
    }}
    if (!status && payload.order && payload.order.status) {{
      status = payload.order.status;
    }}
    // Default to 'failed' only if no status found at all
    if (!status) {{
      status = 'failed';
    }}
    
    const orderId = payload.nimbbl_order_id || payload.order_id || '';
    const transactionId = payload.nimbbl_transaction_id || payload.transaction_id || '';
    const message = payload.message || '';
    
    const responseJson = JSON.stringify(decodedResponse);
    const encodedResponse = btoa(responseJson);
    
    if (status === 'success' || status === 'succeeded' || status === 'completed') {{
      window.__nimbbl_callback_handled = true;
      window.location.href = '{successUrl}?response=' + encodeURIComponent(encodedResponse) +
        (orderId ? '&order_id=' + encodeURIComponent(orderId) : '') +
        (transactionId ? '&transaction_id=' + encodeURIComponent(transactionId) : '') +
        (message ? '&message=' + encodeURIComponent(message) : '');
    }} else {{
      window.__nimbbl_callback_handled = true;
      window.location.href = '{failedUrl}?response=' + encodeURIComponent(encodedResponse) +
        (orderId ? '&order_id=' + encodeURIComponent(orderId) : '') +
        (transactionId ? '&transaction_id=' + encodeURIComponent(transactionId) : '') +
        (message ? '&message=' + encodeURIComponent(message) : '') +
        '&status=' + encodeURIComponent(status || 'failed');
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

