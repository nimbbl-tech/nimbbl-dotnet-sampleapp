using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
namespace nimbbl.checkout
{
    /// <summary>
    /// Represents Order Line Items
    /// </summary>
    public class OrderLineItem
    {
        [JsonProperty("referrer_platform_sku_id")]
        public string referrer_platform_sku_id { get; set; }
        [JsonProperty("title")]
        public string title { get; set; }
        [JsonProperty("description")]
        public string description { get; set; }
        [JsonProperty("quantity")]
        public int quantity { get; set; }
        [JsonProperty("rate")]
        public double rate { get; set; }
        [JsonProperty("amount")]
        public double amount { get; set; }
        [JsonProperty("total_amount")]
        public double total_amount { get; set; }
        [JsonProperty("image_url")]
        public string image_url { get; set; }
    }
}