using Microsoft.AspNetCore.Mvc;
using Nimbbl.Sdk.Rest.Common;
using Nimbbl.Sdk.Rest.Log;
using MerchantSampleApp.Services;
using System.Text;
using System.Text.Json;

namespace MerchantSampleApp.Controllers;

[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly NimbblConfiguration _config;

    public WebhookController(NimbblConfiguration config)
    {
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var raw = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(raw))
            {
                return BadRequest(new { error = ErrorMessages.WebhookPayloadEmpty });
            }

            var accessSecret = _config.AccessSecret;

            var parsed = Util.VerifyAndParseWebhook(raw, accessSecret, out var parsedElement);

            if (!parsed.Success)
            {
                return BadRequest(new { error = parsed.Message });
            }

            ProcessWebhookEvent(parsedElement);

            var eventType = parsedElement.TryGetProperty("event_type", out var et) ? et.GetString() : "unknown";
            return Ok(new { received = true, event_type = eventType });
        }
        catch (Exception ex)
        {
            Logger.GetInstance().Exception($"Webhook error: {ex.Message}", ex);
            return BadRequest(new { error = "Failed to process webhook." });
        }
    }

    private void ProcessWebhookEvent(JsonElement eventData)
    {
        var eventType = eventData.TryGetProperty("event_type", out var et) ? et.GetString() : "unknown";
        
        Logger.GetInstance().Info($"Processing webhook event: {eventType}");

        switch (eventType)
        {
            case "order.paid":
                HandleOrderPaid(eventData);
                break;
            case "order.failed":
                HandleOrderFailed(eventData);
                break;
            case "transaction.created":
                HandleTransactionCreated(eventData);
                break;
            case "transaction.completed":
                HandleTransactionCompleted(eventData);
                break;
            case "transaction.failed":
                HandleTransactionFailed(eventData);
                break;
            case "refund.initiated":
                HandleRefundInitiated(eventData);
                break;
            case "refund.completed":
                HandleRefundCompleted(eventData);
                break;
            default:
                Logger.GetInstance().Info($"Unhandled webhook event type: {eventType}");
                break;
        }
    }

    private void HandleOrderPaid(JsonElement eventData)
    {
        Logger.GetInstance().Info("Order paid event received");
    }

    private void HandleOrderFailed(JsonElement eventData)
    {
        Logger.GetInstance().Info("Order failed event received");
    }

    private void HandleTransactionCreated(JsonElement eventData)
    {
        Logger.GetInstance().Info("Transaction created event received");
    }

    private void HandleTransactionCompleted(JsonElement eventData)
    {
        Logger.GetInstance().Info("Transaction completed event received");
    }

    private void HandleTransactionFailed(JsonElement eventData)
    {
        Logger.GetInstance().Info("Transaction failed event received");
    }

    private void HandleRefundInitiated(JsonElement eventData)
    {
        Logger.GetInstance().Info("Refund initiated event received");
    }

    private void HandleRefundCompleted(JsonElement eventData)
    {
        Logger.GetInstance().Info("Refund completed event received");
    }
}

