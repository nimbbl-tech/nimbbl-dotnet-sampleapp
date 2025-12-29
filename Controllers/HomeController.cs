using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nimbbl.Sdk.Rest.Api;
using Nimbbl.Sdk.Rest.Common;
using Nimbbl.Sdk.Rest.Log;
using MerchantSampleApp.Models;
using MerchantSampleApp.NimbblCheckout;
using MerchantSampleApp.Services;
using CheckoutConstants = MerchantSampleApp.NimbblCheckout.CheckoutConstants;
using System.Text.Json;

namespace MerchantSampleApp.Controllers;

[IgnoreAntiforgeryToken]
public class HomeController : Controller
{
    private readonly NimbblApi _api;

    private const decimal MIN_AMOUNT = 0.01m;

    public HomeController(NimbblApi api)
    {
        _api = api;
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
            Logger.GetInstance().Error($"Order creation error: {ex.Message}");
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

        var totalAmount = (int)(model.Amount * 100);
        var orderRequest = BuildOrderRequest(model, totalAmount);
        
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

    private Dictionary<string, object?> BuildOrderRequest(IndexViewModel model, int totalAmount)
    {
        var userFirstName = model.PrefillUser && !string.IsNullOrWhiteSpace(model.Name) ? model.Name : "John";
        var userEmail = model.PrefillUser && !string.IsNullOrWhiteSpace(model.Email) ? model.Email : "customer@example.com";
        var userMobile = model.PrefillUser && !string.IsNullOrWhiteSpace(model.Mobile) ? model.Mobile : "9876543210";

        var orderRequest = new Dictionary<string, object?>
        {
            ["amount"] = totalAmount,
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
                    ["amount"] = totalAmount,
                    ["total_amount"] = totalAmount,
                    ["amount_before_tax"] = totalAmount,
                    ["tax"] = 0
                }
            }
        };

        if (model.Mode == "redirect")
        {
            orderRequest["callback_url"] = $"{Request.Scheme}://{Request.Host.Value}/payment-callback";
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
        var model = new PaymentSuccessViewModel();
        var responseParam = Request.Query["response"].ToString();
        var parsed = PaymentResponseParser.ParseBase64Response(responseParam);

        if (parsed != null)
        {
            model.OrderId = parsed.OrderId ?? string.Empty;
            model.TransactionId = parsed.TransactionId ?? string.Empty;
            model.Message = !string.IsNullOrEmpty(parsed.Message) ? parsed.Message : "Payment successful!";
            model.Amount = parsed.Amount;
            model.Currency = parsed.Currency;
            model.PaymentMode = parsed.PaymentMode;
            model.UserName = parsed.UserName ?? string.Empty;
        }
        else
        {
            model.OrderId = Request.Query["order_id"].ToString();
            model.TransactionId = Request.Query["transaction_id"].ToString();
            model.Message = Request.Query["message"].ToString();
            if (string.IsNullOrEmpty(model.Message)) model.Message = "Payment successful!";
        }

        if (model.Amount.HasValue && !string.IsNullOrEmpty(model.Currency))
        {
            var amountInCurrency = (decimal)model.Amount.Value / 100;
            model.FormattedAmount = amountInCurrency.ToString("#,##0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        return View(model);
    }

    public IActionResult PaymentFailed()
    {
        var model = new PaymentFailedViewModel();
        var responseParam = Request.Query["response"].ToString();
        var parsed = PaymentResponseParser.ParseBase64Response(responseParam);

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
        }
        else
        {
            model.OrderId = Request.Query["order_id"].ToString();
            model.TransactionId = Request.Query["transaction_id"].ToString();
            model.Message = Request.Query["message"].ToString();
            model.Status = Request.Query["status"].ToString();
            if (string.IsNullOrEmpty(model.Message)) model.Message = "Payment failed. Please try again.";
            if (string.IsNullOrEmpty(model.Status)) model.Status = "failed";
        }

        if (model.Amount.HasValue && !string.IsNullOrEmpty(model.Currency))
        {
            var amountInCurrency = (decimal)model.Amount.Value / 100;
            model.FormattedAmount = amountInCurrency.ToString("#,##0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        return View(model);
    }

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
                throw new Exception(ErrorMessages.InvalidResponseFormat);
            }

            if (parsed.Status == CheckoutConstants.PaymentStatusSuccess || parsed.Status == CheckoutConstants.PaymentStatusSucceeded)
            {
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
            Logger.GetInstance().Exception($"Payment callback error: {ex.Message}", ex);
            return RedirectToAction("PaymentFailed", new { error = "1" });
        }
    }

    public IActionResult Error()
    {
        return View();
    }
}
