using Microsoft.AspNetCore.Mvc;
using Nimbbl.Sdk.Rest.Api;
using Nimbbl.Sdk.Rest.Common;
using System.Text.Json;

namespace MerchantSampleApp.WebFrameworkExamples;

/// <summary>
/// Example: ASP.NET MVC Controller integration with Nimbbl V3 API
/// </summary>
public class ASPNETMVCExample : Controller
{
    private readonly NimbblApi _api;

    public ASPNETMVCExample(NimbblApi api)
    {
        _api = api;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder()
    {
        try
        {
            // Step 1: Generate merchant token
            var tokenResponse = await _api.Auth().GenerateTokenAsync();
            var token = tokenResponse.TryGetProperty("token", out var tokenProp) && tokenProp.ValueKind == JsonValueKind.String
                ? tokenProp.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { error = "Failed to generate merchant token" });
            }

            // Step 2: Set bearer token for subsequent requests
            _api.SetBearerToken(token!);

            // Step 3: Create order
            var orderRequest = new Dictionary<string, object?>
            {
                ["amount"] = 10000,
                ["total_amount"] = 10000,
                ["amount_before_tax"] = 10000,
                ["tax"] = 0,
                ["currency"] = "INR",
                ["merchant_order_id"] = $"order_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                ["invoice_id"] = $"inv_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                ["callback_url"] = $"{Request.Scheme}://{Request.Host.Value}/payment-callback",
                ["user"] = new Dictionary<string, object?>
                {
                    ["first_name"] = "John",
                    ["last_name"] = "Doe",
                    ["email"] = "john.doe@example.com",
                    ["mobile_number"] = "9876543210",
                    ["country_code"] = "+91"
                },
                ["order_line_items"] = new List<Dictionary<string, object?>>
                {
                    new Dictionary<string, object?>
                    {
                        ["title"] = "Product Name",
                        ["description"] = "Product Description",
                        ["quantity"] = 1,
                        ["rate"] = 10000,
                        ["amount"] = 10000,
                        ["total_amount"] = 10000
                    }
                }
            };

            var order = await _api.Orders().CreateOrderAsync(orderRequest);

            // Step 4: Extract order token
            var orderToken = order.TryGetProperty("token", out var ot) && ot.ValueKind == JsonValueKind.String
                ? ot.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(orderToken))
            {
                return BadRequest(new { error = "Order token not returned" });
            }

            // Step 5: Return order token to view
            return View(new { OrderToken = orderToken });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult PaymentCallback()
    {
        var responseParam = Request.Query["response"].ToString();
        
        if (string.IsNullOrWhiteSpace(responseParam))
        {
            return RedirectToAction("Index");
        }

        // Parse and handle payment response
        // Redirect to success/failure page based on status
        return RedirectToAction("PaymentSuccess", new { response = responseParam });
    }
}

