using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nimbbl.Sdk.Rest.Api;
using Nimbbl.Sdk.Rest.Common;
using Nimbbl.Sdk.Rest.Log;
using NimbblDotnetSampleapp.Models;
using NimbblDotnetSampleapp.NimbblCheckout;
using NimbblDotnetSampleapp.Services;
using CheckoutConstants = NimbblDotnetSampleapp.NimbblCheckout.CheckoutConstants;
using System.Text.Json;
using System.Text;

namespace NimbblDotnetSampleapp.Controllers;

[IgnoreAntiforgeryToken]
public class HomeController : Controller
{
    private readonly NimbblApi _api;
    private readonly NimbblConfiguration _config;

    private const decimal MIN_AMOUNT = 0.01m;

    public HomeController(NimbblApi api, NimbblConfiguration config)
    {
        _api = api;
        _config = config;
    }

    public IActionResult Index()
    {
        var responseParam = HttpContext.Request.Query["response"].ToString();
        if (!string.IsNullOrEmpty(responseParam))
        {
            return RedirectToAction("PaymentCallback", new { response = responseParam });
        }
        
        var model = new IndexViewModel();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Index(IndexViewModel model)
    {
        ReadFormData(model);
        
        if (!ValidateInputs(model))
        {
            return View(model);
        }

        try
        {
            await CreateOrderAsync(model);
            
            if (!string.IsNullOrWhiteSpace(model.OrderToken))
            {
                var scriptBuilder = new CheckoutScriptBuilder(Url, Request);
                model.CheckoutScript = scriptBuilder.GenerateCheckoutScript(model.OrderToken, model.Mode ?? "popup");
            }
        }
        catch (Exception ex)
        {
            model.Error = ex.Message;
            Logger.GetInstance().ErrorWithCaller($"Order creation error: {ex.Message}");
        }
        
        return View(model);
    }


    private void ReadFormData(IndexViewModel model)
    {
        model.Amount = decimal.TryParse(Request.Form["amount"], out var amt) ? amt : 4.00m;
        model.Currency = GetFormValue("currency", new[] { "INR", "USD", "EUR" }, "INR");
        model.Name = Request.Form["name"].ToString().Trim();
        model.Email = Request.Form["email"].ToString().Trim();
        model.Mobile = Request.Form["mobile"].ToString().Trim();
        model.Mode = GetFormValue("mode", new[] { "popup", "redirect" }, "popup");
        
        model.PrefillUser = !string.IsNullOrWhiteSpace(model.Name) || 
                           !string.IsNullOrWhiteSpace(model.Email) || 
                           !string.IsNullOrWhiteSpace(model.Mobile);
    }

    private string GetFormValue(string key, string[] validValues, string defaultValue)
    {
        var value = Request.Form[key].ToString();
        return !string.IsNullOrEmpty(value) && validValues.Contains(value) ? value : defaultValue;
    }

    private bool ValidateInputs(IndexViewModel model)
    {
        if (model.Amount < MIN_AMOUNT)
        {
            model.Error = $"Amount must be at least {MIN_AMOUNT}";
            return false;
        }
        
        if (!string.IsNullOrEmpty(model.Email) && !IsValidEmail(model.Email))
        {
            model.Error = "Invalid email format";
            return false;
        }
        
        if (!string.IsNullOrEmpty(model.Mobile) && !IsValidMobile(model.Mobile))
        {
            model.Error = "Invalid mobile number. Please enter 10 digits.";
            return false;
        }

        return true;
    }

    private async Task CreateOrderAsync(IndexViewModel model)
    {
        var merchantToken = await GetMerchantTokenAsync();
        _api.SetBearerToken(merchantToken);

        var totalAmount = (double)model.Amount;
        var orderRequest = BuildOrderRequest(model, totalAmount);
        
        // SDK automatically encrypts payload if ENCRYPT_PAYLOAD flag is enabled
        // Encryption is handled in Orders.CreateOrderAsync based on the flag passed during SDK initialization
        var order = await _api.Orders().CreateOrderAsync(orderRequest);
        ExtractOrderData(model, order);
    }

    private async Task<string> GetMerchantTokenAsync()
    {
        var tokenResponse = await _api.Auth().GenerateTokenAsync();
        var token = tokenResponse.TryGetProperty("token", out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
        
        if (string.IsNullOrWhiteSpace(token))
            throw new Exception(ErrorMessages.MerchantTokenUnavailable);
        
        return token!;
    }

    private Dictionary<string, object?> BuildOrderRequest(IndexViewModel model, double totalAmount)
    {
        var userFirstName = model.PrefillUser && !string.IsNullOrWhiteSpace(model.Name) ? model.Name : "John";
        var userEmail = model.PrefillUser && !string.IsNullOrWhiteSpace(model.Email) ? model.Email : "customer@example.com";
        var userMobile = model.PrefillUser && !string.IsNullOrWhiteSpace(model.Mobile) ? model.Mobile : "9876543210";

        var orderRequest = new Dictionary<string, object?>
        {
            ["total_amount"] = totalAmount,
            ["amount_before_tax"] = totalAmount,
            ["tax"] = 0,
            ["currency"] = model.Currency,
            ["name"] = userFirstName,
            ["email"] = userEmail,
            ["mobile"] = userMobile,
            ["user"] = new Dictionary<string, object?>
            {
                ["first_name"] = userFirstName,
                ["last_name"] = "Doe",
                ["email"] = userEmail,
                ["country_code"] = "+91",
                ["mobile_number"] = userMobile
            },
            ["merchant_order_id"] = $"demo_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            ["invoice_id"] = $"inv_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            ["order_line_items"] = new List<Dictionary<string, object?>>
            {
                new Dictionary<string, object?>
                {
                    ["title"] = "Paper Plane",
                    ["description"] = "Demo product for testing",
                    ["quantity"] = 1,
                    ["rate"] = totalAmount,
                    ["total_amount"] = totalAmount,
                    ["amount_before_tax"] = totalAmount,
                    ["tax"] = 0
                }
            }
        };

        if (model.Mode == "redirect")
        {
            var callbackUrl = $"{Request.Scheme}://{Request.Host.Value}/payment-callback";
            var hostEnvironment = HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
            if (hostEnvironment.IsProduction() && callbackUrl.Contains("localhost"))
            {
                throw new InvalidOperationException("callback_url cannot be localhost in production");
            }
            orderRequest["callback_url"] = callbackUrl;
        }

        return orderRequest;
    }

    private void ExtractOrderData(IndexViewModel model, JsonElement order)
    {
        model.OrderToken = order.TryGetProperty("token", out var ot) && ot.ValueKind == JsonValueKind.String
            ? ot.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(model.OrderToken))
            throw new Exception(ErrorMessages.OrderTokenNotReturned);
    }


    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return true;
        
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidMobile(string mobile)
    {
        if (string.IsNullOrEmpty(mobile))
            return true;
        
        return System.Text.RegularExpressions.Regex.IsMatch(mobile, @"^\d{10}$");
    }

    public IActionResult PaymentSuccess()
    {
        var logger = Logger.GetInstance();
        var model = new PaymentSuccessViewModel();
        var responseParam = Request.Query["response"].ToString();
        logger.InfoWithCaller("PaymentSuccess - Received callback");
        logger.DebugWithCaller($"PaymentSuccess - Raw response param length: {responseParam?.Length ?? 0}");
        var parsed = PaymentResponseParser.ParseBase64Response(responseParam, verifySignature: false);

        if (parsed != null)
        {
            model.OrderId = parsed.OrderId ?? string.Empty;
            model.TransactionId = parsed.TransactionId ?? string.Empty;
            model.Message = !string.IsNullOrEmpty(parsed.Message) ? parsed.Message : "Payment successful!";
            model.Amount = parsed.Amount;
            model.Currency = parsed.Currency;
            model.PaymentMode = parsed.PaymentMode;
            model.UserName = parsed.UserName ?? string.Empty;

            logger.InfoWithCaller($"PaymentSuccess - Parsed status: {parsed.Status}, Order: {model.OrderId}, Transaction: {model.TransactionId}");
        }
        else
        {
            model.OrderId = Request.Query["order_id"].ToString();
            model.TransactionId = Request.Query["transaction_id"].ToString();
            model.Message = Request.Query["message"].ToString();
            if (string.IsNullOrEmpty(model.Message)) model.Message = "Payment successful!";
            logger.WarningWithCaller("PaymentSuccess - Failed to parse response param; using query string fallbacks");
        }

        if (model.Amount.HasValue && !string.IsNullOrEmpty(model.Currency))
        {
            var amountInCurrency = (decimal)model.Amount.Value;
            model.FormattedAmount = amountInCurrency.ToString("#,##0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        return View(model);
    }

    public IActionResult PaymentFailed()
    {
        var logger = Logger.GetInstance();
        var model = new PaymentFailedViewModel();
        var responseParam = Request.Query["response"].ToString();
        logger.InfoWithCaller("PaymentFailed - Received callback");
        logger.DebugWithCaller($"PaymentFailed - Raw response param length: {responseParam?.Length ?? 0}");
        // UI page only; signature verification is performed in the actual callback handlers.
        var parsed = PaymentResponseParser.ParseBase64Response(responseParam, verifySignature: false);

        if (parsed != null)
        {
            model.OrderId = parsed.OrderId ?? string.Empty;
            model.TransactionId = parsed.TransactionId ?? string.Empty;
            model.Message = !string.IsNullOrEmpty(parsed.Message) ? parsed.Message : "Payment failed. Please try again.";
            model.Status = !string.IsNullOrEmpty(parsed.Status) ? parsed.Status : "failed";
            model.Reason = parsed.Reason;
            model.Amount = parsed.Amount;
            model.Currency = parsed.Currency;
            model.PaymentMode = parsed.PaymentMode;
            model.UserName = parsed.UserName ?? string.Empty;

            logger.InfoWithCaller($"PaymentFailed - Parsed status: {model.Status}, Order: {model.OrderId}, Transaction: {model.TransactionId}");
        }
        else
        {
            model.OrderId = Request.Query["order_id"].ToString();
            model.TransactionId = Request.Query["transaction_id"].ToString();
            model.Message = Request.Query["message"].ToString();
            model.Status = Request.Query["status"].ToString();
            if (string.IsNullOrEmpty(model.Message)) model.Message = "Payment failed. Please try again.";
            if (string.IsNullOrEmpty(model.Status)) model.Status = "failed";
            logger.WarningWithCaller("PaymentFailed - Failed to parse response param; using query string fallbacks");
        }

        if (model.Amount.HasValue && !string.IsNullOrEmpty(model.Currency))
        {
            var amountInCurrency = (decimal)model.Amount.Value;
            model.FormattedAmount = amountInCurrency.ToString("#,##0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        return View(model);
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
            logger.DebugWithCaller($"Payment callback raw response param: {responseParam}");

            // Log decoded JSON as well (no truncation)
            try
            {
                var decodedBytes = Convert.FromBase64String(responseParam);
                var decodedJson = System.Text.Encoding.UTF8.GetString(decodedBytes);
                logger.DebugWithCaller($"Payment callback decoded JSON: {decodedJson}");
            }
            catch (Exception decodeEx)
            {
                logger.ExceptionWithCaller($"Payment callback - Failed to decode response param: {decodeEx.Message}", decodeEx);
            }
            
            var parsed = PaymentResponseParser.ParseBase64Response(responseParam, verifySignature: true);

            if (parsed == null)
            {
                logger.ErrorWithCaller("Payment callback - Failed to parse response");
                throw new Exception(ErrorMessages.InvalidResponseFormat);
            }

            logger.InfoWithCaller($"Payment callback - Parsed status: {parsed.Status}, Order: {parsed.OrderId}, Transaction: {parsed.TransactionId}");
            if (!string.IsNullOrWhiteSpace(parsed.SignatureMessage)
                && parsed.SignatureMessage.StartsWith("Signature not present", StringComparison.OrdinalIgnoreCase))
            {
                logger.InfoWithCaller($"Payment callback - Signature verification: skipped ({parsed.SignatureMessage})");
            }
            else
            {
                logger.InfoWithCaller($"Payment callback - Signature verification: {(parsed.SignatureValid ? "valid" : "invalid")} ({parsed.SignatureMessage ?? "N/A"})");
            }

            // If status is unknown and we have an order_id, check the order status via API
            if (parsed.Status == "unknown" && !string.IsNullOrWhiteSpace(parsed.OrderId))
            {
                logger.InfoWithCaller($"Payment callback - Status is unknown, checking order status via API for order: {parsed.OrderId}");
                try
                {
                    var orderResponse = await _api.Orders().GetOrderByIdAsync(parsed.OrderId);
                    if (orderResponse.ValueKind == JsonValueKind.Object)
                    {
                        // Check order status
                        if (orderResponse.TryGetProperty("status", out var orderStatusProp) && orderStatusProp.ValueKind == JsonValueKind.String)
                        {
                            var orderStatus = orderStatusProp.GetString();
                            logger.InfoWithCaller($"Payment callback - Order status from API: {orderStatus}");
                            
                            // Check if order has successful transaction
                            if (orderResponse.TryGetProperty("transactions", out var transactionsProp) && transactionsProp.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var txn in transactionsProp.EnumerateArray())
                                {
                                    if (txn.ValueKind == JsonValueKind.Object 
                                        && txn.TryGetProperty("status", out var txnStatusProp) 
                                        && txnStatusProp.ValueKind == JsonValueKind.String)
                                    {
                                        var txnStatus = txnStatusProp.GetString();
                                        logger.InfoWithCaller($"Payment callback - Transaction status: {txnStatus}");
                                        
                                        if (txnStatus == "succeeded" || txnStatus == "success" || txnStatus == "completed")
                                        {
                                            logger.InfoWithCaller($"Payment callback - Found successful transaction, redirecting to PaymentSuccess");
                                            return RedirectToAction("PaymentSuccess", new { 
                                                response = responseParam,
                                                order_id = parsed.OrderId, 
                                                transaction_id = parsed.TransactionId, 
                                                message = parsed.Message, 
                                                signature_valid = parsed.SignatureValid ? "1" : "0" 
                                            });
                                        }
                                    }
                                }
                            }
                            
                            // If order status is completed or paid, consider it success
                            if (orderStatus == "completed" || orderStatus == "paid")
                            {
                                logger.InfoWithCaller($"Payment callback - Order status indicates success, redirecting to PaymentSuccess");
                                return RedirectToAction("PaymentSuccess", new { 
                                    response = responseParam,
                                    order_id = parsed.OrderId, 
                                    transaction_id = parsed.TransactionId, 
                                    message = parsed.Message, 
                                    signature_valid = parsed.SignatureValid ? "1" : "0" 
                                });
                            }
                        }
                    }
                }
                catch (Exception apiEx)
                {
                    logger.ExceptionWithCaller($"Payment callback - Failed to check order status via API: {apiEx.Message}", apiEx);
                    // Continue with unknown status handling
                }
            }

            // Check for success statuses: "success", "succeeded", or "completed" (for order status)
            if (parsed.Status == CheckoutConstants.PaymentStatusSuccess 
                || parsed.Status == CheckoutConstants.PaymentStatusSucceeded 
                || parsed.Status == "completed")
            {
                logger.InfoWithCaller($"Payment callback - Redirecting to PaymentSuccess (status: {parsed.Status})");
                return RedirectToAction("PaymentSuccess", new { 
                    response = responseParam,
                    order_id = parsed.OrderId, 
                    transaction_id = parsed.TransactionId, 
                    message = parsed.Message, 
                    signature_valid = parsed.SignatureValid ? "1" : "0" 
                });
            }
            else
            {
                logger.WarningWithCaller($"Payment callback - Redirecting to PaymentFailed (status: {parsed.Status})");
                return RedirectToAction("PaymentFailed", new { 
                    response = responseParam,
                    order_id = parsed.OrderId, 
                    transaction_id = parsed.TransactionId, 
                    message = parsed.Message, 
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

            if (string.IsNullOrWhiteSpace(raw))
                return BadRequest(new { error = "Empty callback body." });

            logger.DebugWithCaller($"PaymentCallback(POST) raw payload: {raw}");

            using var bodyDoc = JsonDocument.Parse(raw);
            var bodyEl = bodyDoc.RootElement;

            // Populated by popup handler
            var callbackEl = bodyEl.ValueKind == JsonValueKind.Object
                && bodyEl.TryGetProperty("callback", out var cbProp)
                ? cbProp
                : default;

            string? encryptedResponse = null;
            if (bodyEl.ValueKind == JsonValueKind.Object
                && bodyEl.TryGetProperty("encrypted_response", out var encProp)
                && encProp.ValueKind == JsonValueKind.String)
            {
                encryptedResponse = encProp.GetString();
            }

            var result = new Dictionary<string, object?>
            {
                ["received"] = true
            };

            JsonElement parsedEl;
            string? eventType = null;

            // Auto-detect handling (no flag):
            // - If encrypted_response is present: decrypt it.
            // - Else: use callback as-is.
            if (!string.IsNullOrWhiteSpace(encryptedResponse))
            {
                logger.InfoWithCaller("PaymentCallback(POST) decrypting payload");
                var enc = new Encryption(_config.AccessSecret);
                var decrypted = enc.Decrypt(encryptedResponse!, true);
                logger.DebugWithCaller($"PaymentCallback(POST) decrypted payload: {decrypted}");

                using var decryptedDoc = JsonDocument.Parse(decrypted);
                parsedEl = decryptedDoc.RootElement.Clone();
            }
            else
            {
                if (callbackEl.ValueKind == JsonValueKind.Undefined || callbackEl.ValueKind == JsonValueKind.Null)
                    return BadRequest(new { error = "Missing callback payload." });

                parsedEl = callbackEl.Clone();
            }

            // Extract event type for client-side handling (avoid double redirects on close events)
            if (parsedEl.ValueKind == JsonValueKind.Object
                && parsedEl.TryGetProperty("event_type", out var et)
                && et.ValueKind == JsonValueKind.String)
            {
                eventType = et.GetString();
            }
            result["event_type"] = eventType;
            result["parsed"] = parsedEl;

            // Signature verification (prefer payload object when present)
            var verifyTarget = parsedEl.ValueKind == JsonValueKind.Object
                && parsedEl.TryGetProperty("payload", out var payloadEl)
                && payloadEl.ValueKind == JsonValueKind.Object
                    ? payloadEl
                    : parsedEl;

            if (!HasAnySignature(verifyTarget))
            {
                result["signature_verification_skipped"] = true;
                result["signature_valid"] = false;
                result["signature_message"] = "Signature not present; skipped verification";
                logger.InfoWithCaller($"PaymentCallback(POST) signature verification: skipped (signature not present) [event_type={eventType ?? "N/A"}]");
            }
            else
            {
                var verificationResult = Util.VerifySignature(verifyTarget, _config.AccessSecret);
                result["signature_valid"] = verificationResult.Success;
                result["signature_message"] = verificationResult.Message;
                if (!verificationResult.Success && !string.IsNullOrWhiteSpace(verificationResult.Message))
                    result["signature_error"] = verificationResult.Message;

                if (verificationResult.Success)
                    logger.InfoWithCaller($"PaymentCallback(POST) signature verification: valid ({verificationResult.Message}) [event_type={eventType ?? "N/A"}]");
                else
                    logger.WarningWithCaller($"PaymentCallback(POST) signature verification: invalid ({verificationResult.Message}) [event_type={eventType ?? "N/A"}]");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.ExceptionWithCaller($"PaymentCallback(POST) error: {ex.Message}", ex);
            return StatusCode(500, new { error = "Failed to process popup callback." });
        }
    }

    public IActionResult Error()
    {
        return View();
    }

    private static bool HasAnySignature(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return false;

        static string? GetStr(JsonElement el, string name)
            => el.ValueKind == JsonValueKind.Object && el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
                ? p.GetString()
                : null;

        // Direct fields
        if (!string.IsNullOrWhiteSpace(GetStr(element, "nimbbl_signature"))) return true;
        if (!string.IsNullOrWhiteSpace(GetStr(element, "signature"))) return true;

        // transaction.{nimbbl_signature|signature}
        if (element.TryGetProperty("transaction", out var txn) && txn.ValueKind == JsonValueKind.Object)
        {
            if (!string.IsNullOrWhiteSpace(GetStr(txn, "nimbbl_signature"))) return true;
            if (!string.IsNullOrWhiteSpace(GetStr(txn, "signature"))) return true;
        }

        // order.nimbbl_signature
        if (element.TryGetProperty("order", out var order) && order.ValueKind == JsonValueKind.Object)
        {
            if (!string.IsNullOrWhiteSpace(GetStr(order, "nimbbl_signature"))) return true;
        }

        return false;
    }
}
