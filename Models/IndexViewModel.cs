namespace MerchantSampleApp.Models;

public class IndexViewModel
{
    // Display/Result properties
    public string? OrderToken { get; set; }
    public string? Error { get; set; }
    public string? CheckoutScript { get; set; }
    
    // Form input properties (matching UI fields)
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public decimal Amount { get; set; } = 4.00m;
    public string Currency { get; set; } = "INR";
    public string Mode { get; set; } = "popup";
    
    // Internal computed property (used by controller logic)
    public bool PrefillUser { get; set; } = false;
}

