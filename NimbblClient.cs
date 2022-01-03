using System;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Text;
using System.Security.Cryptography;
using 

namespace nimbbl.checkout
{
    /// <summary>
    /// This class is used as main class to interact with Nimbbl APIs using RestClient
    /// </summary>
    public class NimbblClient : IDisposable
    {
        public string BaseUrl { get; private set; }
        public string AccessKey { get; private set; }
        public string SecretKey { get; private set; }
        private RestClient _restClient;
        public TokenResponse Token { get; private set; }
        public Order Task{ get; private set; } 

        //public Transaction Task{ get; private set; }

        public NimbblClient(string baseUrl, string accessKey, string secretKey)
        {
            this.BaseUrl = baseUrl;
            this.AccessKey = accessKey;
            this.SecretKey = secretKey;

            _restClient = new RestClient(baseUrl);
        }

        /// <summary>
        /// Generates the Authentication Token 
        /// </summary>
        /// <returns></returns>
        public async Task<TokenResponse> GenerateToken()
        {
            var request = new TokenRequest() { AccessKey = AccessKey, AccessSecret = SecretKey };
            try
            {
                Token = await _restClient.PostAsync<TokenResponse>(_url_generate_token, request);
                _restClient.SetBearerToken(Token.Token);
                return Token;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Creates a Nimbbl Order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public async Task<Order> CreateOrder(Order order)
        {
            try
            {
                var createdOrder = await _restClient.PostAsync<Order>(_url_create_order, order);
                return createdOrder;
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public async Task<Order> FetchOrder(string id)
        {
            try
            {
                var fetchOrder = await _restClient.PostAsync<Order>(_url_fetch_order, id);
                return fetchOrder;
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public async Task<Order> All(string id)
        {
            List<Entity> entities = base.All(options);
            List<Order> orders = new List<Order>();
            foreach (Entity entity in entities)
            {
                orders.Add(entity as Order);
            }
            return orders;
        }

        public async Task<Transaction> FetchTransactionbyID(string id)
        {
            try
            {
                var fetchTransaction = await _restClient.PostAsync<Transaction>(_url_fetchTransaction_byID, id);
                return fetchTransaction;
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public async Task<Transaction> FetchTransactionbyOrderID(string id)
        {
            try
            {
                var fetchTransaction = await _restClient.PostAsync<Transaction>(_url_fetchTransaction_byOrderID, id);
                return fetchTransaction;
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public bool ValidateSignature(string invoiceId, string transactionId, string signature)
        {
            string generatedSignature = HmacSHA256(this.SecretKey, String.Format("{0}|{1}", invoiceId, transactionId));
            return signature.Equals(generatedSignature);
        }

        public void Dispose()
        {
            _restClient?.Dispose();
        }

        static string HmacSHA256(string key, string data)
        {
            string hash;
            ASCIIEncoding encoder = new ASCIIEncoding();
            Byte[] code = encoder.GetBytes(key);
            using (HMACSHA256 hmac = new HMACSHA256(code))
            {
                Byte[] hmBytes = hmac.ComputeHash(encoder.GetBytes(data));
                hash = ToHexString(hmBytes);
            }
            return hash;
        }
        static string ToHexString(byte[] array)
        {
            StringBuilder hex = new StringBuilder(array.Length * 2);
            foreach (byte b in array)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }
        private const string _url_generate_token = "/api/v2/generate-token";
        private const string _url_create_order = "/api/v2/create-order";

        private const string _url_fetch_order = "/api/v2/get-order/:order_id";

        private const string _url_fetchAll_order = "/api/v2/";

        private const string _url_fetchTransaction_byID = "/api/v2/fetch-transaction/:transaction_id";

        private const string _url_fetchTransaction_byOrderID = "/api/v2/order/fetch-transactions/:order_id";

    }
}