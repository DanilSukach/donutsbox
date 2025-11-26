using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Donutsbox.Api.Services.Payments;

public class YooKassaClient : IYooKassaClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient httpClient;
    private readonly YooKassaOptions options;
    private readonly ILogger<YooKassaClient> logger;

    public YooKassaClient(HttpClient httpClient, IOptions<YooKassaOptions> options, ILogger<YooKassaClient> logger)
    {
        this.httpClient = httpClient;
        this.options = options.Value;
        this.logger = logger;

        httpClient.BaseAddress ??= new Uri(this.options.ApiBaseUrl);

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{this.options.ShopId}:{this.options.SecretKey}"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<YooKassaPaymentResponse> CreatePaymentAsync(YooKassaCreatePaymentRequest request, string idempotenceKey, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "payments");
        httpRequest.Headers.Add("Idempotence-Key", idempotenceKey);
        httpRequest.Content = JsonContent.Create(request, options: SerializerOptions);

        var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to create YooKassa payment. Status: {StatusCode}. Body: {Body}", response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }

        var payment = JsonSerializer.Deserialize<YooKassaPaymentResponse>(body, SerializerOptions);
        return payment ?? throw new InvalidOperationException("Failed to deserialize YooKassa payment response");
    }

    public async Task<YooKassaPaymentResponse?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"payments/{paymentId}");
        var response = await httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to fetch YooKassa payment {PaymentId}. Status: {StatusCode}", paymentId, response.StatusCode);
            return null;
        }

        var payment = await response.Content.ReadFromJsonAsync<YooKassaPaymentResponse>(SerializerOptions, cancellationToken);
        return payment;
    }
}

