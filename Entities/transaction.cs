using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace nimbbl.checkout
{
    /// <summary>
    /// Represents an Transaction entity
    /// </summary>
    public class Transaction
    {
        [JsonProperty("transaction_id")]
        public string Transaction_Id { get; set; }
        [JsonProperty("transaction_date")]
        public string Transaction_Date { get; set; }
        [JsonProperty("amount_before_tax")]
        public float? Amount_Before_Tax { get; set; }
        [JsonProperty("tax")]
        public float Tax { get; set; }
        [JsonProperty("total_amount")]
        public float Total_Amount { get; set; }
        [JsonProperty("referrer_platform")]
        public string Referrer_Platform { get; set; }
        [JsonProperty("invoice_id")]
        public string Invoice_Id { get; set; }
        [JsonProperty("currency")]
        public string Currency { get; set; }
        [JsonProperty("order_from_ip")]
        public string Order_From_Ip { get; set; }
        [JsonProperty("device_user_agent")]
        public string Device_User_Agent { get; set; }
        [JsonProperty("user")]
        public User User { get; set; }
        [JsonProperty("shipping_address")]
        public Address Shipping_Address { get; set; }
        [JsonProperty("order_line_items", NullValueHandling = NullValueHandling.Ignore)]
        public OrderLineItem[] Order_Line_Items { get; set; }
    }
}