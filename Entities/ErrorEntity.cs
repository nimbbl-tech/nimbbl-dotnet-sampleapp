using System;
using Newtonsoft.Json;

namespace nimbbl.checkout{
    /// <summary>
    /// Represents the error message returned by the Nimbbl Api
    /// </summary>
    public class ErrorEntity{
        [JsonProperty("code")]
        public int Code { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}