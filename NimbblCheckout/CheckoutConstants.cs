namespace MerchantSampleApp.NimbblCheckout;

/// <summary>
/// Constants for Nimbbl Checkout configuration
/// </summary>
public static class CheckoutConstants
{
    // Payment Mode Codes
    public const string PaymentModeAll = "allpayment";
    public const string PaymentModeNetBanking = "net_banking";
    public const string PaymentModeWallet = "wallet";
    public const string PaymentModeUpi = "upi";
    public const string PaymentModeEmi = "emi";

    // Checkout Modes
    public const string ModePopup = "popup";
    public const string ModeRedirect = "redirect";
    public const string DefaultMode = ModePopup;

    // Option Keys
    public const string OptionKeyCallbackUrl = "callback_url";
    public const string OptionKeyPaymentModeCode = "payment_mode_code";
    public const string OptionKeySubPaymentMode = "sub_payment_mode";

    // Config Keys
    public const string ConfigKeyToken = "token";
    public const string ConfigKeyApiHost = "apiHost";
    public const string ConfigKeyCheckoutHost = "checkoutHost";

    // Payment Status
    public const string PaymentStatusSuccess = "success";
    public const string PaymentStatusSucceeded = "succeeded";
    public const string PaymentStatusFailed = "failed";
}

