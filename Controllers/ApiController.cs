using Microsoft.AspNetCore.Mvc;
using Nimbbl.Sdk.Rest.Api;
using Nimbbl.Sdk.Rest.Common;
using Nimbbl.Sdk.Rest.Log;
using MerchantSampleApp.Services;
using System.Text;
using System.Text.Json;

namespace MerchantSampleApp.Controllers;

/// <summary>
/// API endpoints for checkout response decryption and transaction enquiry
/// </summary>
[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly NimbblApi _api;
    private readonly NimbblConfiguration _config;

    public ApiController(NimbblApi api, NimbblConfiguration config)
    {
        _api = api;
        _config = config;
    }

    /// <summary>
    /// Checkout response handler - decrypts encrypted_response from popup callback
    /// </summary>
    [HttpPost("checkout-response")]
    public async Task<IActionResult> CheckoutResponse()
    {
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var raw = await reader.ReadToEndAsync();
            var body = JsonSerializer.Deserialize<Dictionary<string, object?>>(raw) ?? new Dictionary<string, object?>();

            var result = new Dictionary<string, object?>
            {
                ["received"] = true
            };

            if (body.TryGetValue("encrypted_response", out var encryptedResponseObj) && encryptedResponseObj is string encryptedResponse)
            {
                var accessSecret = _config.AccessSecret;
                var enc = new Encryption(accessSecret);
                var decrypted = enc.Decrypt(encryptedResponse, true);
                result["decrypted"] = decrypted;

                var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(decrypted);
                if (parsed != null)
                {
                    result["parsed"] = parsed;
                }
            }
            else
            {
                if (body.ContainsKey("payload"))
                {
                    result["parsed"] = body;
                }
                else if (body.ContainsKey("status"))
                {
                    result["parsed"] = new Dictionary<string, object?> { ["payload"] = body };
                }
                else
                {
                    result["parsed"] = body;
                }
            }

            if (body.TryGetValue("attributes", out var attributesObj) && attributesObj is JsonElement attributes && attributes.ValueKind == JsonValueKind.Object)
            {
                var accessSecret = _config.AccessSecret;
                var verificationResult = Util.VerifySignature(attributes, accessSecret);
                if (verificationResult.Success)
                {
                    result["signature_valid"] = true;
                    result["signature_message"] = verificationResult.Message;
                }
                else
                {
                    result["signature_valid"] = false;
                    if (!string.IsNullOrEmpty(verificationResult.Message))
                    {
                        result["signature_error"] = verificationResult.Message;
                    }
                }
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.GetInstance().Exception($"Checkout response error: {ex.Message}", ex);
            return StatusCode(500, new { error = "Failed to process checkout response." });
        }
    }

    /// <summary>
    /// Transaction enquiry endpoint
    /// </summary>
    [HttpPost("transaction-enquiry")]
    public async Task<IActionResult> TransactionEnquiry()
    {
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var raw = await reader.ReadToEndAsync();
            var body = JsonSerializer.Deserialize<Dictionary<string, object?>>(raw) ?? new Dictionary<string, object?>();

            var transactionId = body.TryGetValue("transaction_id", out var tid) ? tid?.ToString() 
                : (body.TryGetValue("nimbbl_transaction_id", out var ntid) ? ntid?.ToString() : null);
            var merchantToken = body.TryGetValue("merchant_token", out var mt) ? mt?.ToString() : null;

            if (string.IsNullOrWhiteSpace(transactionId))
            {
                return BadRequest(new { error = ErrorMessages.TransactionIdRequired });
            }

            if (string.IsNullOrWhiteSpace(merchantToken))
            {
                return BadRequest(new { error = ErrorMessages.MerchantTokenRequired });
            }

            _api.SetBearerToken(merchantToken);

            var request = new Dictionary<string, object?>
            {
                ["transaction_id"] = transactionId
            };

            var resp = await _api.Transactions().TransactionEnquiryAsync(request);

            return Ok(resp);
        }
        catch (Exception ex)
        {
            Logger.GetInstance().Exception($"Transaction enquiry error: {ex.Message}", ex);
            return StatusCode(500, new { error = "Failed to perform transaction enquiry." });
        }
    }
}

