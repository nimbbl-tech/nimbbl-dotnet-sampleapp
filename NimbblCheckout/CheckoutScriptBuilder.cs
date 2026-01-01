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
        var apiEndpoint = $"{baseUrl}/api/checkout-response";
        var successUrl = $"{baseUrl}{_urlHelper.Action("PaymentSuccess", "Home")}";
        var failedUrl = $"{baseUrl}{_urlHelper.Action("PaymentFailed", "Home")}";

        return $@"async function(response) {{
  try {{
    console.log('Popup callback received:', response);
    
    if (!response) {{
      console.error('Empty response received');
      window.location.href = '{failedUrl}?error=1';
      return;
    }}
    
    let decodedResponse = response;
    
    if (response && response.encrypted_response) {{
      try {{
        const res = await fetch('{apiEndpoint}', {{
          method: 'POST',
          headers: {{ 'Content-Type': 'application/json' }},
          body: JSON.stringify(response)
        }});
        
        if (!res.ok) {{
          throw new Error('Decryption request failed: ' + res.status);
        }}
        
        const data = await res.json();
        
        if (data.parsed) {{
          decodedResponse = data.parsed;
        }} else if (data.decrypted) {{
          decodedResponse = JSON.parse(data.decrypted);
          if (!decodedResponse.payload && decodedResponse.status) {{
            decodedResponse = {{ payload: decodedResponse }};
          }}
        }} else if (data.error) {{
          console.error('Decryption error:', data.error);
          window.location.href = '{failedUrl}?error=1';
          return;
        }}
      }} catch (e) {{
        console.error('Failed to decrypt response', e);
        window.location.href = '{failedUrl}?error=1';
        return;
      }}
    }}
    
    if (!decodedResponse.payload) {{
      if (decodedResponse.status) {{
        decodedResponse = {{ payload: decodedResponse }};
      }} else {{
        console.error('Invalid response format: missing payload');
        window.location.href = '{failedUrl}?error=1';
        return;
      }}
    }}
    
    const payload = decodedResponse.payload || decodedResponse;
    const status = payload.status || decodedResponse.status || 'failed';
    const orderId = payload.nimbbl_order_id || payload.order_id || '';
    const transactionId = payload.nimbbl_transaction_id || payload.transaction_id || '';
    const message = payload.message || '';
    
    const responseJson = JSON.stringify(decodedResponse);
    const encodedResponse = btoa(responseJson);
    
    if (status === 'success' || status === 'succeeded') {{
      console.log('Payment successful, redirecting to success page. Order ID:', orderId);
      window.location.href = '{successUrl}?response=' + encodeURIComponent(encodedResponse) +
        (orderId ? '&order_id=' + encodeURIComponent(orderId) : '') +
        (transactionId ? '&transaction_id=' + encodeURIComponent(transactionId) : '') +
        (message ? '&message=' + encodeURIComponent(message) : '');
    }} else {{
      console.log('Payment failed or unknown status, redirecting to failed page. Status:', status);
      window.location.href = '{failedUrl}?response=' + encodeURIComponent(encodedResponse) +
        (orderId ? '&order_id=' + encodeURIComponent(orderId) : '') +
        (transactionId ? '&transaction_id=' + encodeURIComponent(transactionId) : '') +
        (message ? '&message=' + encodeURIComponent(message) : '') +
        '&status=' + encodeURIComponent(status || 'failed');
    }}
  }} catch (e) {{
    console.error('Failed to handle callback', e);
    console.error('Error details:', e.message, e.stack);
    window.location.href = '{failedUrl}?error=1';
  }}
}}";
    }
}

