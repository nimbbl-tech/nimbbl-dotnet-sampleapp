using Nimbbl.Sdk.Rest.Api;
using Nimbbl.Sdk.Rest.Common;
using System.Text.Json;

namespace MerchantSampleApp.WebFrameworkExamples;

/// <summary>
/// Example: ASP.NET Core integration with Nimbbl V3 API
/// </summary>
public class ASPNETCoreExample
{
    private readonly NimbblApi _api;

    public ASPNETCoreExample(NimbblApi api)
    {
        _api = api;
    }

    public async Task<string> CreateOrderAndLaunchCheckout()
    {
        // Step 1: Generate merchant token
        var tokenResponse = await _api.Auth().GenerateTokenAsync();
        var token = tokenResponse.TryGetProperty("token", out var tokenProp) && tokenProp.ValueKind == JsonValueKind.String
            ? tokenProp.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new Exception("Failed to generate merchant token");
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
            throw new Exception("Order token not returned");
        }

        // Step 5: Launch checkout (return script to inject in view)
        return GenerateCheckoutScript(orderToken!);
    }

    private string GenerateCheckoutScript(string orderToken)
    {
        return $@"<script type=""module"">
import Checkout from ""https://cdn.jsdelivr.net/npm/nimbbl_sonic@latest"";
const checkout = new Checkout({{ token: ""{orderToken}"" }});
checkout.open({{}});
</script>";
    }
}

