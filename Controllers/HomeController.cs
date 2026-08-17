using Microsoft.AspNetCore.Mvc; 

using Nimbbl.Sdk.Rest;
using Nimbbl.Sdk.Rest.Common;
using Nimbbl.Sdk.Rest.Log;
using NimbblDotnetSampleapp.Models;
using NimbblDotnetSampleapp.NimbblCheckout;
using NimbblDotnetSampleapp.Services;
using System.Text;
using System.Text.Json;

namespace NimbblDotnetSampleapp.Controllers;

public class HomeController : Controller
{
    private readonly NimbblApi _api;
    private readonly NimbblConfiguration _config;

    public HomeController(NimbblConfiguration config)
    {
        _config = config;
        
        // Build baseUrl from apiHost if provided, otherwise use default
        string? baseUrl = null;
        if (!string.IsNullOrWhiteSpace(config.ApiHost))
        {
            baseUrl = $"{config.ApiHost.TrimEnd('/')}/api/";
        }
        
        _api = new NimbblApi(
            config.AccessKey,
            config.AccessSecret,
            config.ApiHost,
            null,
            config.EncryptPayload
        );
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new IndexViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Index([FromForm] double amount, [FromForm] string currency = "INR", [FromForm] string mode = "popup")
    {
        var logger = Logger.GetInstance();
        try
        {
            // Set up nimbbl order
            var orderData = new Dictionary<string, object>
            {
                {"amount_before_tax", amount },
                {"total_amount", amount },
                { "currency", currency },
                { "invoice_id", "INV-" + Guid.NewGuid().ToString().Substring(0, 8) },
                { "order_id", "ORD-" + Guid.NewGuid().ToString().Substring(0, 8) },
                { "user", new Dictionary<string, string>
                    {
                        { "mobile_number", "9999999999" },
                        { "email", "test@example.com" },
                        { "first_name", "Test" },
                        { "last_name", "User" }
                    }
                }
            };

            var orderResponse = await _api.Orders().CreateOrderAsync(orderData);
            
            if (orderResponse.ValueKind == JsonValueKind.Object && orderResponse.TryGetProperty("order_id", out var idProp))
            {
                var orderId = idProp.GetString();
                
                // Extract order token from response
                string? orderToken = null;
                if (orderResponse.TryGetProperty("token", out var tokenProp) && tokenProp.ValueKind == JsonValueKind.String)
                {
                    orderToken = tokenProp.GetString();
                }
                
                // Validate and normalize mode (default to popup if invalid)
                var checkoutMode = (mode == "redirect" || mode == "popup") ? mode : "popup";
                
                // For popup mode, return JSON with checkout script to execute on same page
                if (checkoutMode == "popup" && !string.IsNullOrEmpty(orderToken))
                {
                    var scriptBuilder = new CheckoutScriptBuilder(Url, Request);
                    var checkoutScript = scriptBuilder.GenerateCheckoutScript(orderToken, checkoutMode);
                    return Json(new { 
                        success = true, 
                        checkoutScript = checkoutScript,
                        orderId = orderId 
                    });
                }
                
                // For redirect mode, return Checkout view
                if (!string.IsNullOrEmpty(orderToken))
                {
                    var scriptBuilder = new CheckoutScriptBuilder(Url, Request);
                    var checkoutScript = scriptBuilder.GenerateCheckoutScript(orderToken, checkoutMode);
                    var model = new IndexViewModel { CheckoutScript = checkoutScript };
                    return View(model);
                }
            }
            
            return View(new IndexViewModel { Error = "Failed to create order. Please try again." });
        }
        catch (Exception ex)
        {
            logger.ExceptionWithCaller($"Checkout error: {ex.Message}", ex);
            return View(new IndexViewModel { Error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Checkout([FromForm] double amount, [FromForm] string currency = "INR", [FromForm] string mode = "popup")
    {
        var logger = Logger.GetInstance();
        try
        {
            // Set up nimbbl order
            var orderData = new Dictionary<string, object>
            {
                {"amount_before_tax", amount },
                  {"total_amount", amount },
                { "currency", currency },
                { "invoice_id", "INV-" + Guid.NewGuid().ToString().Substring(0, 8) },
                { "order_id", "ORD-" + Guid.NewGuid().ToString().Substring(0, 8) },
                { "user", new Dictionary<string, string>
                    {
                        { "mobile_number", "9999999999" },
                        { "email", "test@example.com" },
                        { "first_name", "Test" },
                        { "last_name", "User" }
                    }
                }
            };

            var orderResponse = await _api.Orders().CreateOrderAsync(orderData);
            
            if (orderResponse.ValueKind == JsonValueKind.Object && orderResponse.TryGetProperty("order_id", out var idProp))
            {
                var orderId = idProp.GetString();
                ViewBag.OrderId = orderId;
                ViewBag.AccessKey = _config.AccessKey;
                
                // Extract order token from response
                string? orderToken = null;
                if (orderResponse.TryGetProperty("token", out var tokenProp) && tokenProp.ValueKind == JsonValueKind.String)
                {
                    orderToken = tokenProp.GetString();
                }
                
                // Validate and normalize mode (default to popup if invalid)
                var checkoutMode = (mode == "redirect" || mode == "popup") ? mode : "popup";
                
                // For popup mode, return JSON with checkout script to execute on same page
                if (checkoutMode == "popup" && !string.IsNullOrEmpty(orderToken))
                {
                    var scriptBuilder = new CheckoutScriptBuilder(Url, Request);
                    var checkoutScript = scriptBuilder.GenerateCheckoutScript(orderToken, checkoutMode);
                    return Json(new { 
                        success = true, 
                        checkoutScript = checkoutScript,
                        orderId = orderId 
                    });
                }
                
                // For redirect mode, return Checkout view
                if (!string.IsNullOrEmpty(orderToken))
                {
                    var scriptBuilder = new CheckoutScriptBuilder(Url, Request);
                    var checkoutScript = scriptBuilder.GenerateCheckoutScript(orderToken, checkoutMode);
                    ViewBag.CheckoutScript = checkoutScript;
                }
                
                return View("Checkout");
            }
            
            return View("Error");
        }
        catch (Exception ex)
        {
            logger.ExceptionWithCaller($"Checkout error: {ex.Message}", ex);
            return View("Error");
        }
    }

    [Route("payment-callback")]
    [HttpGet]
    public async Task<IActionResult> PaymentCallback()
    {
        var responseParam = Request.Query["response"].ToString();

        if (string.IsNullOrWhiteSpace(responseParam))
        {
            return RedirectToAction("Index");
        }

        try
        {
            var logger = Logger.GetInstance();
            logger.InfoWithCaller("Payment callback received - Parsing response");

            var accessSecret = _config.AccessSecret;

            // Version-aware verification: handles v4 signed/encrypted callbacks and legacy responses,
            // unwraps the checkout envelope, and returns the verified event payload.
            var result = SignatureVerifier.VerifyCallback(responseParam, accessSecret);
            if (!result.Success || !result.Payload.HasValue)
            {
                logger.ErrorWithCaller($"Payment callback - Signature verification failed: {result.Message}");
                throw new Exception(ErrorMessages.InvalidResponseFormat);
            }

            var root = result.Payload.Value;
            var (orderId, transactionId, status, message) = PaymentResponseParser.ExtractPaymentFields(root);

            logger.InfoWithCaller($"Payment callback - Parsed status: {status}, Order: {orderId}, Transaction: {transactionId}");

            // Check for success statuses (both "success" and "succeeded" indicate success)
            if (status == NimbblCheckout.CheckoutConstants.PaymentStatusSucceeded || 
                status == NimbblCheckout.CheckoutConstants.PaymentStatusSuccess)
            {
                return RedirectToAction("PaymentSuccess", new { 
                    response = responseParam,
                    order_id = orderId, 
                    transaction_id = transactionId, 
                    message = message 
                });
            }
            else
            {
                return RedirectToAction("PaymentFailed", new { 
                    response = responseParam,
                    order_id = orderId, 
                    transaction_id = transactionId, 
                    message = message, 
                    status = status 
                });
            }
        }
        catch (Exception ex)
        {
            Logger.GetInstance().ExceptionWithCaller($"Payment callback error: {ex.Message}", ex);
            return RedirectToAction("PaymentFailed", new { error = "1" });
        }
    }

    /// <summary>
    /// Popup callback handler (Option B): POST /payment-callback
    /// Accepts either encrypted_response (when encryption enabled) or plain callback payload.
    /// Performs signature verification and returns "parsed" JSON to the browser callback handler.
    /// </summary>
    [Route("payment-callback")]
    [HttpPost]
    public async Task<IActionResult> PaymentCallbackPost()
    {
        var logger = Logger.GetInstance();
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var raw = await reader.ReadToEndAsync();
            logger.InfoWithCaller("PaymentCallback(POST) received");
            logger.DebugWithCaller($"PaymentCallback(POST) raw response: {raw}");

            if (string.IsNullOrWhiteSpace(raw))
                return BadRequest(new { error = "Empty callback body." });

            var accessSecret = _config.AccessSecret;

            // Version-aware verification (v4 signed/encrypted + legacy), returns the verified event payload.
            var result = SignatureVerifier.VerifyCallback(raw, accessSecret);
            if (!result.Success || !result.Payload.HasValue)
            {
                logger.ErrorWithCaller($"PaymentCallback(POST) - Signature verification failed: {result.Message}");
                return BadRequest(new { error = "Invalid signature or response format." });
            }
            // Return the verified event payload directly as JSON
            return Ok(result.Payload.Value);
        }
        catch (Exception ex)
        {
            logger.ExceptionWithCaller($"PaymentCallback(POST) error: {ex.Message}", ex);
            return StatusCode(500, new { error = "Failed to process popup callback." });
        }
    }

    public IActionResult PaymentSuccess(string response, string order_id, string transaction_id, string message)
    {
        ViewBag.Response = response;
        ViewBag.OrderId = order_id ?? string.Empty;
        ViewBag.TransactionId = transaction_id ?? string.Empty;
        ViewBag.Message = message ?? "Payment successful!";
        ViewBag.FormattedAmount = string.Empty;
        ViewBag.Currency = string.Empty;
        ViewBag.PaymentMode = string.Empty;
        ViewBag.UserName = string.Empty;

        // Try to extract additional details from the response if available
        if (!string.IsNullOrWhiteSpace(response))
        {
            try
            {
                var accessSecret = _config.AccessSecret;

                // Version-aware verification returns the verified event payload (v4 + legacy).
                var result = SignatureVerifier.VerifyCallback(response, accessSecret);
                JsonElement payload = result.Payload ?? default;

                // Extract transaction details
                if (payload.TryGetProperty(JsonKeys.Transaction, out var txn) && txn.ValueKind == JsonValueKind.Object)
                {
                    var amount = JsonUtils.TryGetDouble(txn, JsonKeys.TransactionAmount);
                    if (amount.HasValue)
                    {
                        ViewBag.FormattedAmount = amount.Value.ToString("F2");
                    }
                    
                    var currency = JsonUtils.TryGetString(txn, JsonKeys.TransactionCurrency);
                    if (!string.IsNullOrEmpty(currency))
                    {
                        ViewBag.Currency = currency;
                    }
                    
                    var paymentMode = JsonUtils.TryGetString(txn, JsonKeys.PaymentMode);
                    if (!string.IsNullOrEmpty(paymentMode))
                    {
                        ViewBag.PaymentMode = paymentMode;
                    }
                }

                // Extract user details
                if (payload.TryGetProperty(JsonKeys.User, out var user) && user.ValueKind == JsonValueKind.Object)
                {
                    var userName = JsonUtils.TryGetString(user, JsonKeys.Name);
                    if (!string.IsNullOrEmpty(userName))
                    {
                        ViewBag.UserName = userName;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().ExceptionWithCaller($"Error parsing response in PaymentSuccess: {ex.Message}", ex);
                // Continue with default values
            }
        }

        return View();
    }

    public IActionResult PaymentFailed(string response, string order_id, string transaction_id, string message, string status, string error)
    {
        var model = new Models.PaymentFailedViewModel
        {
            OrderId = order_id ?? string.Empty,
            TransactionId = transaction_id ?? string.Empty,
            Message = message ?? "Payment failed. Please try again.",
            Status = status ?? "failed"
        };
        
        ViewBag.Response = response;
        ViewBag.OrderId = order_id;
        ViewBag.TransactionId = transaction_id;
        ViewBag.Message = message;
        ViewBag.Status = status;
        
        return View(model);
    }

    public IActionResult Error()
    {
        return View();
    }

}
