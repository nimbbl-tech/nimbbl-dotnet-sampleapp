using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
namespace nimbbl.checkout
{
    public class User
    {
        [JsonProperty("mobile_number")]
        public string Mobile_Number { get; set; }
        [JsonProperty("email")]
        public string Email { get; set; }
        [JsonProperty("first_name")]
        public string First_Name { get; set; }
        [JsonProperty("last_name")]
        public string Last_Name { get; set; }
    }
}