using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
namespace nimbbl.checkout
{
    public class Address
    {
        [JsonProperty("street")]
        public string Street { get; set; }
        [JsonProperty("area")]
        public string Area { get; set; }
        [JsonProperty("city")]
        public string City { get; set; }
        [JsonProperty("state")]
        public string State { get; set; }
        [JsonProperty("pincode")]
        public string Pincode { get; set; }
        [JsonProperty("address_type")]
        public string Address_Type { get; set; }
    }
}