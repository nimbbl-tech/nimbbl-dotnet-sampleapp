using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
namespace nimbbl.checkout
{
    /// <summary>
    /// Token response entity
    /// </summary>
    public class TokenResponse
    {
        [JsonProperty("token")]
        public string Token { get; set; }
        [JsonProperty("expires_at")]
        public DateTime Expires_At { get; set; }
        [JsonProperty("auth_principal")]
        public AuthPrincipal AuthPrincipal { get; set; }
    }
    public class AuthPrincipal
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("access_key")]
        public string Access_Key { get; set; }
        [JsonProperty("sub_merchant_id")]
        public int Sub_Merchant_Id { get; set; }
    }

    public class TokenRequest
    {
        [JsonProperty("access_key")]
        public string AccessKey { get; set; }
        [JsonProperty("access_secret")]
        public string AccessSecret { get; set; }

    }

}