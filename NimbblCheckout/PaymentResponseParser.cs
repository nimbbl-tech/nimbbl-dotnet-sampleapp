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
    private const string SignatureNotPresentMessage = "Signature not present; skipped verification";
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
            var logger = Logger.GetInstance();

            // Decrypt if encrypted_response is present either at root or inside payload
            try
            {
                string? encryptedResponse = null;
                if (root.TryGetProperty("encrypted_response", out var encryptedProp) && encryptedProp.ValueKind == JsonValueKind.String)
                    encryptedResponse = encryptedProp.GetString();
                else if (payload.ValueKind == JsonValueKind.Object
                         && payload.TryGetProperty("encrypted_response", out var payloadEncProp)
                         && payloadEncProp.ValueKind == JsonValueKind.String)
                    encryptedResponse = payloadEncProp.GetString();

                if (!string.IsNullOrWhiteSpace(encryptedResponse))
                {
                    var accessSecret = NimbblConfiguration.Instance.AccessSecret;
                    var enc = new Encryption(accessSecret);
                    var decryptedJson = enc.Decrypt(encryptedResponse!, true);
                    using var decryptedDoc = JsonDocument.Parse(decryptedJson);
                    // Important: clone so JsonElement isn't tied to disposed JsonDocument
                    root = decryptedDoc.RootElement.Clone();

                    // Re-bind payload after decryption
                    if (root.TryGetProperty("payload", out var decryptedPayloadProp))
                        payload = decryptedPayloadProp;
                    else
                        payload = root;
                }
            }
            catch (Exception ex)
            {
                logger.ExceptionWithCaller($"PaymentResponseParser - Decryption failed: {ex.Message}", ex);
                // Continue without decrypting; status may remain unknown
            }

            // Extract order and transaction IDs
            parsed.OrderId = TryGetString(payload, "nimbbl_order_id") 
                ?? TryGetString(payload, "order_id")
                ?? TryGetString(root, "nimbbl_order_id")
                ?? TryGetString(root, "order_id");

            parsed.TransactionId = TryGetString(payload, "nimbbl_transaction_id")
                ?? TryGetString(payload, "transaction_id")
                ?? TryGetString(root, "nimbbl_transaction_id")
                ?? TryGetString(root, "transaction_id");

            // Extract status - check nested transaction and order status as well
            var payloadStatus = TryGetString(payload, "status");
            var transactionStatus = payload.TryGetProperty("transaction", out var txnProp) && txnProp.ValueKind == JsonValueKind.Object 
                ? TryGetString(txnProp, "status") 
                : null;
            var orderStatus = payload.TryGetProperty("order", out var orderProp) && orderProp.ValueKind == JsonValueKind.Object 
                ? TryGetString(orderProp, "status") 
                : null;
            var rootStatus = TryGetString(root, "status");
            
            parsed.Status = payloadStatus 
                ?? transactionStatus
                ?? orderStatus
                ?? rootStatus
                ?? "unknown";

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
                var accessSecret = NimbblConfiguration.Instance.AccessSecret;
                // Redirect / checkout payloads typically include "transaction" and "order" at the payload level.
                // Webhook payloads use "attributes". Support both.
                if (root.TryGetProperty("attributes", out var attributesProp) && attributesProp.ValueKind == JsonValueKind.Object)
                {
                    if (!HasAnySignature(attributesProp))
                    {
                        parsed.SignatureValid = false;
                        parsed.SignatureMessage = SignatureNotPresentMessage;
                    }
                    else
                    {
                        var verificationResult = Util.VerifySignature(attributesProp, accessSecret);
                        parsed.SignatureValid = verificationResult.Success;
                        parsed.SignatureMessage = verificationResult.Message;
                    }
                }
                else
                {
                    if (!HasAnySignature(payload))
                    {
                        parsed.SignatureValid = false;
                        parsed.SignatureMessage = SignatureNotPresentMessage;
                    }
                    else
                    {
                        var verificationResult = Util.VerifySignature(payload, accessSecret);
                        parsed.SignatureValid = verificationResult.Success;
                        parsed.SignatureMessage = verificationResult.Message;
                    }
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

    private static bool HasAnySignature(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return false;

        // Common direct fields
        if (!string.IsNullOrWhiteSpace(TryGetString(element, "nimbbl_signature"))) return true;
        if (!string.IsNullOrWhiteSpace(TryGetString(element, "signature"))) return true;

        // Nested transaction fields
        if (element.TryGetProperty("transaction", out var txn) && txn.ValueKind == JsonValueKind.Object)
        {
            if (!string.IsNullOrWhiteSpace(TryGetString(txn, "nimbbl_signature"))) return true;
            if (!string.IsNullOrWhiteSpace(TryGetString(txn, "signature"))) return true;
        }

        // Nested order fields
        if (element.TryGetProperty("order", out var order) && order.ValueKind == JsonValueKind.Object)
        {
            if (!string.IsNullOrWhiteSpace(TryGetString(order, "nimbbl_signature"))) return true;
        }

        return false;
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

