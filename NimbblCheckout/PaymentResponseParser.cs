using System;
using System.Text;
using System.Text.Json;
using Nimbbl.Sdk.Rest.Common;
using Nimbbl.Sdk.Rest.Log;
using NimbblDotnetSampleapp.Services;

namespace NimbblDotnetSampleapp.NimbblCheckout;

/// <summary>
/// Service for parsing and decrypting payment responses from Nimbbl checkout
/// </summary>
public static class PaymentResponseParser
{
    /// <summary>
    /// Parse a base64-encoded response string from payment callback
    /// </summary>
    public static ParsedResponse? ParseBase64Response(string base64Response, bool verifySignature = false)
    {
        if (string.IsNullOrWhiteSpace(base64Response))
            return null;

        try
        {
            var decodedBytes = Convert.FromBase64String(base64Response);
            var decodedString = Encoding.UTF8.GetString(decodedBytes);
            return ParseResponse(decodedString, verifySignature);
        }
        catch (Exception ex)
        {
            Logger.GetInstance().ExceptionWithCaller($"Failed to decode base64 response: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// Parse a JSON response string (already decoded)
    /// </summary>
    public static ParsedResponse? ParseResponse(string jsonResponse, bool verifySignature = false)
    {
        if (string.IsNullOrWhiteSpace(jsonResponse))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            // Handle encrypted response
            if (root.TryGetProperty("encrypted_response", out var encryptedProp) && encryptedProp.ValueKind == JsonValueKind.String)
            {
                var encryptedResponse = encryptedProp.GetString();
                if (!string.IsNullOrWhiteSpace(encryptedResponse))
                {
                    var accessSecret = NimbblConfiguration.Instance.AccessSecret;
                    var enc = new Encryption(accessSecret);
                    var decrypted = enc.Decrypt(encryptedResponse!, true);
                    using var decryptedDoc = JsonDocument.Parse(decrypted);
                    root = decryptedDoc.RootElement;
                }
            }

            // Extract payload
            JsonElement payload;
            if (root.TryGetProperty("payload", out var payloadProp))
            {
                payload = payloadProp;
            }
            else
            {
                payload = root;
            }

            var parsed = new ParsedResponse();

            // Extract order and transaction IDs
            parsed.OrderId = TryGetString(payload, "nimbbl_order_id") 
                ?? TryGetString(payload, "order_id")
                ?? TryGetString(root, "nimbbl_order_id")
                ?? TryGetString(root, "order_id");

            parsed.TransactionId = TryGetString(payload, "nimbbl_transaction_id")
                ?? TryGetString(payload, "transaction_id")
                ?? TryGetString(root, "nimbbl_transaction_id")
                ?? TryGetString(root, "transaction_id");

            // Extract status
            parsed.Status = TryGetString(payload, "status") ?? TryGetString(root, "status") ?? "unknown";

            // Extract message
            parsed.Message = TryGetString(payload, "message") ?? TryGetString(root, "message") ?? string.Empty;

            // Extract reason
            parsed.Reason = TryGetString(payload, "reason") ?? TryGetString(root, "reason") ?? string.Empty;

            // Extract amount and currency
            parsed.Amount = TryGetDouble(payload, "amount") ?? TryGetDouble(root, "amount");
            parsed.Currency = TryGetString(payload, "currency") ?? TryGetString(root, "currency") ?? string.Empty;

            // Extract payment mode
            parsed.PaymentMode = TryGetString(payload, "payment_mode") ?? TryGetString(root, "payment_mode") ?? string.Empty;

            // Extract user name
            if (payload.TryGetProperty("user", out var userProp) && userProp.ValueKind == JsonValueKind.Object)
            {
                parsed.UserName = TryGetString(userProp, "name") 
                    ?? $"{TryGetString(userProp, "first_name")} {TryGetString(userProp, "last_name")}".Trim();
            }

            // Verify signature if requested
            if (verifySignature)
            {
                if (root.TryGetProperty("attributes", out var attributesProp) && attributesProp.ValueKind == JsonValueKind.Object)
                {
                    var accessSecret = NimbblConfiguration.Instance.AccessSecret;
                    var verificationResult = Util.VerifySignature(attributesProp, accessSecret);
                    parsed.SignatureValid = verificationResult.Success;
                    parsed.SignatureMessage = verificationResult.Message;
                }
                else
                {
                    parsed.SignatureValid = false;
                    parsed.SignatureMessage = "No attributes found for signature verification";
                }
            }

            return parsed;
        }
        catch (Exception ex)
        {
            Logger.GetInstance().ExceptionWithCaller($"Failed to parse payment response: {ex.Message}", ex);
            return null;
        }
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static double? TryGetDouble(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetDouble();
            if (prop.ValueKind == JsonValueKind.String && double.TryParse(prop.GetString(), out var result))
                return result;
        }
        return null;
    }
}

/// <summary>
/// Parsed payment response data
/// </summary>
public class ParsedResponse
{
    public string? OrderId { get; set; }
    public string? TransactionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public double? Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentMode { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public bool SignatureValid { get; set; }
    public string? SignatureMessage { get; set; }
}

