using Microsoft.AspNetCore.Mvc;
using Nimbbl.Sdk.Rest.Common;
using Nimbbl.Sdk.Rest.Log;
using NimbblDotnetSampleapp.NimbblCheckout;
using NimbblDotnetSampleapp.Services;
using System.Text;
using System.Text.Json;

namespace NimbblDotnetSampleapp.Controllers;

[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly NimbblConfiguration _config;
    private readonly Nimbbl.Sdk.Rest.NimbblApi _api;

    public WebhookController(NimbblConfiguration config)
    {
        _config = config;
        
        // Build baseUrl from apiHost if provided, otherwise use default
        string? baseUrl = null;
        if (!string.IsNullOrWhiteSpace(config.ApiHost))
        {
            baseUrl = $"{config.ApiHost.TrimEnd('/')}/api/";
        }
        
        _api = new Nimbbl.Sdk.Rest.NimbblApi(
            config.AccessKey,
            config.AccessSecret,
            config.ApiHost,
            null,
            config.EncryptPayload
        );
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        var logger = Logger.GetInstance();
        logger.InfoWithCaller("Webhook received - Starting processing");
        
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var raw = await reader.ReadToEndAsync();

            logger.DebugWithCaller($"Webhook payload received - Length: {raw?.Length ?? 0} characters");
            // Print the full raw webhook body for debugging/verification.
            // NOTE: This can be large (especially when encrypted_response is present).
            logger.DebugWithCaller($"Webhook raw payload: {raw}");

            if (string.IsNullOrWhiteSpace(raw))
            {
                logger.ErrorWithCaller("Webhook payload is empty");
                return BadRequest(new { error = ErrorMessages.WebhookPayloadEmpty });
            }

            var accessSecret = _config.AccessSecret;
            var root = PayloadHelperUtils.Parse(raw, accessSecret);

            if (!SignatureVerifier.VerifySignature(root, accessSecret))
            {
                logger.ErrorWithCaller("Webhook signature verification failed.");
                return BadRequest(new { error = "Invalid signature or payload" });
            }

            var eventType = JsonUtils.TryGetString(root, JsonKeys.EventType) ?? "unknown";

            var paymentFields = PaymentResponseParser.ExtractPaymentFields(root);
            var orderId = paymentFields.orderId;
            var transactionId = paymentFields.transactionId;
            
            ProcessWebhookEvent(root, orderId, transactionId);

            logger.InfoWithCaller($"Webhook processed successfully - Event type: {eventType}");
            return Ok(new Dictionary<string, object?> { 
                [JsonKeys.Received] = true, 
                [JsonKeys.EventType] = eventType 
            });
        }
        catch (Exception ex)
        {
            Logger.GetInstance().ExceptionWithCaller($"Webhook error: {ex.Message}", ex);
            return BadRequest(new { error = "Failed to process webhook." });
        }
    }

    private void ProcessWebhookEvent(JsonElement eventData, string? orderId, string? transactionId)
    {
        var logger = Logger.GetInstance();
        var eventType = eventData.TryGetProperty("event_type", out var et) ? et.GetString() : "";

        logger.InfoWithCaller($"Processing webhook event - Type: {eventType}, Order: {orderId ?? "N/A"}, Transaction: {transactionId ?? "N/A"}");

        switch (eventType)
        {
            case "payment_success":
                HandlePaymentSuccess(orderId, transactionId, eventData);
                break;

            case "payment_failed":
                HandlePaymentFailed(orderId, transactionId, eventData);
                break;

            case "payment_reversing":
                HandlePaymentReversing(orderId, transactionId, eventData);
                break;

            case "payment_reversal_failed":
                HandlePaymentReversalFailed(orderId, transactionId, eventData);
                break;

            case "payment_reversed":
                HandlePaymentReversed(orderId, transactionId, eventData);
                break;

            case "refund_success":
                HandleRefundSuccess(orderId, transactionId, eventData);
                break;

            case "refund_failed":
                HandleRefundFailed(orderId, transactionId, eventData);
                break;

            case "refund_pending":
                HandleRefundPending(orderId, transactionId, eventData);
                break;

            default:
                logger.WarningWithCaller($"Unknown webhook event type: {eventType ?? "N/A"} - Ignoring");
                break;
        }
    }

    /// <summary>
    /// Handle payment success event
    /// TODO: Implement your business logic here
    /// </summary>
    private void HandlePaymentSuccess(string? orderId, string? transactionId, JsonElement data)
    {
        Logger.GetInstance().InfoWithCaller($"Payment successful - Order: {orderId ?? "N/A"}, Transaction: {transactionId ?? "N/A"}");

        // TODO: Add your business logic here
        // Examples:
        // - Update order status in database to 'paid'
        // - Send confirmation email to customer
        // - Trigger order fulfillment
        // - Update inventory
        // - Send notification to admin
    }

    /// <summary>
    /// Handle payment failed event
    /// TODO: Implement your business logic here
    /// </summary>
    private void HandlePaymentFailed(string? orderId, string? transactionId, JsonElement data)
    {
        Logger.GetInstance().ErrorWithCaller($"Payment failed - Order: {orderId ?? "N/A"}, Transaction: {transactionId ?? "N/A"}");

        // Extract failure reason from transaction object
        string? failureReason = null;
        if (data.TryGetProperty("transaction", out var transaction))
        {
            if (transaction.TryGetProperty("nimbbl_merchant_message", out var merchantMsg))
                failureReason = merchantMsg.GetString();
            else if (transaction.TryGetProperty("nimbbl_error_code", out var errorCode))
                failureReason = errorCode.GetString();
            else if (transaction.TryGetProperty("nimbbl_consumer_message", out var consumerMsg))
                failureReason = consumerMsg.GetString();
        }

        if (string.IsNullOrWhiteSpace(failureReason) && data.TryGetProperty("message", out var msg))
        {
            failureReason = msg.GetString();
        }

        if (!string.IsNullOrWhiteSpace(failureReason))
        {
            Logger.GetInstance().ErrorWithCaller($"Failure reason: {failureReason}");
        }

        // TODO: Add your business logic here
        // Examples:
        // - Update order status to 'payment_failed'
        // - Send notification to customer
        // - Log failure reason
    }

    /// <summary>
    /// Handle payment reversing event
    /// TODO: Implement your business logic here
    /// </summary>
    private void HandlePaymentReversing(string? orderId, string? transactionId, JsonElement data)
    {
        Logger.GetInstance().WarningWithCaller($"Payment reversing - Order: {orderId ?? "N/A"}, Transaction: {transactionId ?? "N/A"}");
        // TODO: Implement your business logic
    }

    /// <summary>
    /// Handle payment reversal failed event
    /// TODO: Implement your business logic here
    /// </summary>
    private void HandlePaymentReversalFailed(string? orderId, string? transactionId, JsonElement data)
    {
        Logger.GetInstance().ErrorWithCaller($"Payment reversal failed - Order: {orderId ?? "N/A"}, Transaction: {transactionId ?? "N/A"}");
        // TODO: Implement your business logic
    }

    /// <summary>
    /// Handle payment reversed event
    /// TODO: Implement your business logic here
    /// </summary>
    private void HandlePaymentReversed(string? orderId, string? transactionId, JsonElement data)
    {
        Logger.GetInstance().InfoWithCaller($"Payment reversed - Order: {orderId ?? "N/A"}, Transaction: {transactionId ?? "N/A"}");
        // TODO: Implement your business logic
    }

    /// <summary>
    /// Handle refund success event
    /// TODO: Implement your business logic here
    /// </summary>
    private void HandleRefundSuccess(string? orderId, string? transactionId, JsonElement data)
    {
        string? refundId = null;
        decimal refundAmount = 0;

        if (data.TryGetProperty("nimbbl_refund_id", out var refundIdProp))
        {
            refundId = refundIdProp.GetString();
        }
        else if (data.TryGetProperty("refund_transaction_id", out var refundTxnIdProp))
        {
            refundId = refundTxnIdProp.GetString();
        }
        else if (data.TryGetProperty("transaction", out var txn) && txn.TryGetProperty("transaction_id", out var txnId))
        {
            refundId = txnId.GetString();
        }

        if (data.TryGetProperty("transaction", out var transaction))
        {
            if (transaction.TryGetProperty("refund_amount", out var refundAmtProp))
            {
                refundAmount = refundAmtProp.GetDecimal();
            }
        }
        else if (data.TryGetProperty("refund_amount", out var refundAmtProp2))
        {
            refundAmount = refundAmtProp2.GetDecimal();
        }

        Logger.GetInstance().InfoWithCaller($"Refund successful - Order: {orderId ?? "N/A"}, Refund: {refundId ?? "N/A"}, Amount: {refundAmount}");
        // TODO: Implement your business logic
    }

    /// <summary>
    /// Handle refund failed event
    /// TODO: Implement your business logic here
    /// </summary>
    private void HandleRefundFailed(string? orderId, string? transactionId, JsonElement data)
    {
        string? txnId = transactionId;
        if (string.IsNullOrWhiteSpace(txnId))
        {
            if (data.TryGetProperty("refund_transaction_id", out var refundTxnIdProp))
            {
                txnId = refundTxnIdProp.GetString();
            }
            else if (data.TryGetProperty("transaction", out var txn) && txn.TryGetProperty("transaction_id", out var txnIdProp))
            {
                txnId = txnIdProp.GetString();
            }
        }

        Logger.GetInstance().ErrorWithCaller($"Refund failed - Order: {orderId ?? "N/A"}, Transaction: {txnId ?? "N/A"}");
        // TODO: Implement your business logic
    }

    /// <summary>
    /// Handle refund pending event
    /// TODO: Implement your business logic here
    /// </summary>
    private void HandleRefundPending(string? orderId, string? transactionId, JsonElement data)
    {
        string? txnId = transactionId;
        if (string.IsNullOrWhiteSpace(txnId))
        {
            if (data.TryGetProperty("refund_transaction_id", out var refundTxnIdProp))
            {
                txnId = refundTxnIdProp.GetString();
            }
            else if (data.TryGetProperty("transaction", out var txn) && txn.TryGetProperty("transaction_id", out var txnIdProp))
            {
                txnId = txnIdProp.GetString();
            }
        }

        Logger.GetInstance().WarningWithCaller($"Refund pending - Order: {orderId ?? "N/A"}, Transaction: {txnId ?? "N/A"}");
        // TODO: Implement your business logic
    }
}

