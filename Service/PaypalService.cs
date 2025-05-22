using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PayPalCheckoutSdk.Payments;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Transactions;
using web.Models;
using web.Utility;

namespace Service
{
    public class PaypalService : IPaypalService
    {
        private readonly HttpClient _httpClient;
        private readonly PaypalSettings _paypalSettings;
        private const string BaseUrl = "https://api-m.sandbox.paypal.com";
        private string _accessToken;
        private DateTime _tokenExpiration;

        public PaypalService(HttpClient httpClient, IOptions<PaypalSettings> paypalSettings)
        {
            _httpClient = httpClient;
            _paypalSettings = paypalSettings.Value;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            // Check if we have a valid token already
            if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiration > DateTime.UtcNow)
            {
                return _accessToken;
            }

            var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_paypalSettings.ClientId}:{_paypalSettings.Secret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            var content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await _httpClient.PostAsync($"{BaseUrl}/v1/oauth2/token", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Get token and set expiration
            _accessToken = tokenResponse.GetProperty("access_token").GetString();
            int expiresIn = tokenResponse.GetProperty("expires_in").GetInt32();
            _tokenExpiration = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Buffer of 60 seconds

            return _accessToken;
        }

        public async Task<PaypalOrderResponse> CreateOrderAsync(OrderHeader orderHeader, string returnUrl, string cancelUrl)
        {
            var accessToken = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            decimal exchangeRate = 25934m;

            // Chuyển tổng tiền VND sang USD
            decimal orderTotalUSD = (decimal)orderHeader.OrderTotal / exchangeRate;
            var orderRequest = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                new
                {
                    reference_id = orderHeader.Id.ToString(),
                    description = $"Order #{orderHeader.Id}",
                    amount = new
                    {
                        currency_code = "USD",
                        value = orderTotalUSD.ToString("0.00")
                    }
                }
            },
                application_context = new
                {
                    return_url = returnUrl,
                    cancel_url = cancelUrl,
                    brand_name = "RentalMusic",
                    landing_page = "BILLING",
                    user_action = "PAY_NOW",
                    shipping_preference = "NO_SHIPPING"
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(orderRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl}/v2/checkout/orders", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("PayPal API Status: " + response.StatusCode);
                    Console.WriteLine("PayPal API Content: " + responseContent);
                    throw new Exception($"Failed to create PayPal order: {responseContent}");
                }

                var orderResponse = System.Text.Json.JsonSerializer.Deserialize<PaypalOrderResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (orderResponse == null || orderResponse.Links == null)
                {
                    Console.WriteLine("Deserialized PayPal order response is null or missing links.");
                    throw new Exception("Invalid response from PayPal when creating order.");
                }

                return orderResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception when creating PayPal order: " + ex.Message);
                throw;
            }
        }

        public async Task<PaypalOrderCaptureResponse> CaptureOrderAsync(string paypalOrderId)
        {
            var accessToken = await GetAccessTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/v2/checkout/orders/{paypalOrderId}/capture");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // ⚠️ Đây là phần quan trọng để nhận đúng capture_id
            request.Headers.Add("Prefer", "return=representation");

            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"PayPal Capture Error: {content}");
            }

            var captureResponse = System.Text.Json.JsonSerializer.Deserialize<PaypalOrderCaptureResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            Console.WriteLine("Capture JSON: " + content);
            Console.WriteLine("ID: " + captureResponse?.Id);
            Console.WriteLine("Payer email: " + captureResponse?.Payer?.Email);
            Console.WriteLine("Purchase units count: " + captureResponse?.PurchaseUnits?.Count);

            return captureResponse;
        }

        public async Task<PayPalRefundResponse> RefundCaptureAsync(string captureId)
        {
            var accessToken = await GetAccessTokenAsync();

            var refundEndpoint = $"{BaseUrl}/v2/payments/captures/{captureId}/refund";

            var request = new HttpRequestMessage(HttpMethod.Post, refundEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var refundResponse = JsonSerializer.Deserialize<PayPalRefundResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return refundResponse;
            }

            throw new Exception($"Refund failed: {await response.Content.ReadAsStringAsync()}");
        }

    }

    public interface IPaypalService
    {
        Task<string> GetAccessTokenAsync();
        Task<PaypalOrderResponse> CreateOrderAsync(OrderHeader orderHeader, string returnUrl, string cancelUrl);
        Task<PaypalOrderCaptureResponse> CaptureOrderAsync(string paypalOrderId);
        Task<PayPalRefundResponse> RefundCaptureAsync(string captureId);
    }
 

public class PayPalRefundResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    public class PaypalOrderResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("links")]
        public List<PaypalLink> Links { get; set; }
    }

    public class PaypalOrderCaptureResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("payer")]
        public PaypalPayer Payer { get; set; }

        [JsonPropertyName("purchase_units")]
        public List<PaypalPurchaseUnitCapture> PurchaseUnits { get; set; }
    }

    public class PaypalLink
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }

        [JsonPropertyName("rel")]
        public string Rel { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; }
    }

    public class PaypalPayer
    {
        [JsonPropertyName("name")]
        public PaypalPayerName Name { get; set; }

        [JsonPropertyName("email_address")]
        public string Email { get; set; }

        [JsonPropertyName("payer_id")]
        public string PayerId { get; set; }
    }

    public class PaypalPayerName
    {
        [JsonPropertyName("given_name")]
        public string GivenName { get; set; }

        [JsonPropertyName("surname")]
        public string Surname { get; set; }
    }

    public class PaypalPurchaseUnitCapture
    {
        [JsonPropertyName("reference_id")]
        public string ReferenceId { get; set; }

        [JsonPropertyName("payments")]
        public PaypalPayments Payments { get; set; }
    }

    public class PaypalPayments
    {
        [JsonPropertyName("captures")]
        public List<PaypalCapture> Captures { get; set; }
    }

    public class PaypalCapture
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("amount")]
        public PaypalAmount Amount { get; set; }
    }

    public class PaypalAmount
    {
        [JsonPropertyName("currency_code")]
        public string CurrencyCode { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }
    }

}
