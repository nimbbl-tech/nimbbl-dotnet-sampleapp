using Nimbbl.Sdk.Rest.Common; 

using Nimbbl.Sdk.Rest.Log;
using NimbblDotnetSampleapp.Services;
using System.Text;
using System.Text.Json;

namespace NimbblDotnetSampleapp.NimbblCheckout;

/// <summary>
/// Parser to extract fields from Nimbbl payment response
/// </summary>
public static class PaymentResponseParser
{
    /// <summary>
    /// Common logic to extract key fields from a payment response JSON
    /// </summary>
    public static (string? orderId, string? transactionId, string status, string message) ExtractPaymentFields(JsonElement root)
    {
        JsonElement payload = root;
        if (root.TryGetProperty(JsonKeys.Payload, out var p))
        {
            payload = p;
        }

        // 1. Extract Order ID (strictly following user preference)
        var orderId = JsonUtils.TryGetString(payload, JsonKeys.NimbblOrderId);

        // 2. Extract Transaction ID (strictly following user preference)
        var transactionId = JsonUtils.TryGetString(payload, JsonKeys.NimbblTransactionId)
            ?? JsonUtils.TryGetString(payload, JsonKeys.TransactionId);

        // 3. Extract Status
        // Always use transaction.status if available (this is the authoritative status)
        // Fall back to payload.status only if transaction is not present
        string? status = null;
        if (payload.TryGetProperty(JsonKeys.Transaction, out var txn) && txn.ValueKind == JsonValueKind.Object)
        {
            status = JsonUtils.TryGetString(txn, JsonKeys.Status);
        }
        
        // Only fall back to payload.status if transaction.status was not found
        if (string.IsNullOrEmpty(status))
        {
            status = JsonUtils.TryGetString(payload, JsonKeys.Status) 
                ?? "unknown";
        }
        
        // 4. Extract Message
        var message = JsonUtils.TryGetString(payload, JsonKeys.Message) 
            ?? JsonUtils.TryGetString(root, JsonKeys.Message) 
            ?? string.Empty;

        return (orderId, transactionId, status, message);
    }
}

/// <summary>
/// Parsed payment response data (Deprecated, but kept for reference if needed elsewhere)
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
