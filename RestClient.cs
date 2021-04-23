using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
//You need to install package Newtonsoft.Json > https://www.nuget.org/packages/Newtonsoft.Json/
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace nimbbl.checkout
{
    /// <summary>
    /// This is a Restful client, all restful methods should be part of this class.
    /// Author : Sumit Kate
    /// </summary>
    internal class RestClient : IDisposable
    {
        private static TimeSpan _timeout;
        private static HttpClient _httpClient;
        private static string _baseUrl;
        private const string ClientUserAgent = "Nimbble-dotnet-sdk-v1.0";
        private const string MediaTypeJson = "application/json";

        internal RestClient(string baseUrl, TimeSpan? timeout = null)
        {
            _baseUrl = NormalizeBaseUrl(baseUrl);
            _timeout = timeout ?? TimeSpan.FromSeconds(90);
        }

        /// <summary>
        /// POST call
        /// </summary>
        /// <param name="url"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        internal async Task<string> PostAsync(string url, object input)
        {
            try
            {
                EnsureHttpClientCreated();

                using (var requestContent = new StringContent(ConvertToJsonString(input), Encoding.UTF8, MediaTypeJson))
                {
                    using (var response = await _httpClient.PostAsync(url, requestContent))
                    {
                        try
                        {
                            // In case of non Success status codes this throws exception
                            var respMessage = response.EnsureSuccessStatusCode(); 
                        }
                        catch (HttpRequestException)
                        {
                            // TODO Need to handle the failure cases coming from the APIs.
                            throw;
                        }
                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }
            catch (HttpRequestException)
            {
                // TODO Need to handle the generic http exceptions such as network error
                throw;
            }
        }

        /// <summary>
        /// POST call
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        internal async Task<TResult> PostAsync<TResult>(string url, object input) where TResult : class, new()
        {
            var strResponse = await PostAsync(url, input);

            return JsonConvert.DeserializeObject<TResult>(strResponse, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        /// <summary>
        /// GET Restful call returns the deserialized object
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        internal async Task<TResult> GetAsync<TResult>(string url) where TResult : class, new()
        {
            var strResponse = await GetAsync(url);

            return JsonConvert.DeserializeObject<TResult>(strResponse, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
        }

        /// <summary>
        /// GET Restful call returns the response as string
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal async Task<string> GetAsync(string url)
        {
            EnsureHttpClientCreated();

            using (var response = await _httpClient.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        /// <summary>
        /// This is used to set the Bearer Token
        /// </summary>
        /// <param name="token"></param>
        internal void SetBearerToken(string token)
        {
            EnsureHttpClientCreated();
            //remove existing token
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// IDisposable implementation
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }


        /// <summary>
        /// This will create the Instance of HttpClient with required headers and settings.
        /// </summary>
        private static void CreateHttpClient()
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            };

            _httpClient = new HttpClient(httpClientHandler, false)
            {
                Timeout = _timeout
            };

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(ClientUserAgent);

            if (!string.IsNullOrWhiteSpace(_baseUrl))
            {
                _httpClient.BaseAddress = new Uri(_baseUrl);
            }

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeJson));
        }

        /// <summary>
        /// This method ensures HttpClient is always created before using for any Rest Operation
        /// </summary>
        private static void EnsureHttpClientCreated()
        {
            if (_httpClient == null)
            {
                CreateHttpClient();
            }
        }

        /// <summary>
        /// Serializes a Object into a Json String using Newtonsoft.Json library
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static string ConvertToJsonString(object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }

            return JsonConvert.SerializeObject(obj);
        }

        /// <summary>
        /// This will append the trailing slash to the base url if it is missing.
        /// E.g. https://www.example.com will be normalized to https://www.example.com/
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string NormalizeBaseUrl(string url)
        {
            return url.EndsWith("/") ? url : url + "/";
        }
    }
}