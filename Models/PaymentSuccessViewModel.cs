namespace MerchantSampleApp.Models;

public class PaymentSuccessViewModel
{
    public string OrderId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Message { get; set; } = "Payment successful!";
    public bool SignatureValid { get; set; }
    public long? Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentMode { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FormattedAmount { get; set; } = string.Empty;
}

