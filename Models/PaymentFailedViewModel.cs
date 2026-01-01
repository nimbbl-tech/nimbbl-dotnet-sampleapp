namespace NimbblDotnetSampleapp.Models;

public class PaymentFailedViewModel
{
    public string OrderId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Message { get; set; } = "Payment failed. Please try again.";
    public string Status { get; set; } = "failed";
    public string Reason { get; set; } = string.Empty;
    public double? Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentMode { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FormattedAmount { get; set; } = string.Empty;
}

