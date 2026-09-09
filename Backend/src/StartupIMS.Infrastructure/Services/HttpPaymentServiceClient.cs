using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using StartupIMS.Shared.DTOs;
using StartupIMS.Shared.Settings;

namespace StartupIMS.Infrastructure.Services;

public class HttpPaymentServiceClient : IPaymentServiceClient
{
    private readonly HttpClient _http;
    private readonly PaymentServiceSettings _settings;

    public HttpPaymentServiceClient(HttpClient http, IOptions<PaymentServiceSettings> settings)
    {
        _settings = settings.Value;
        _http = http;
        _http.BaseAddress = new Uri(_settings.BaseUrl);
    }

    public async Task<InitiatePaymentResponse> InitiatePaymentAsync(int fundingRequestId, decimal amount, string currency)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/initiate")
        {
            Content = JsonContent.Create(new InitiatePaymentRequest(fundingRequestId, amount, currency))
        };
        request.Headers.Add("X-Service-Key", _settings.OutboundApiKey);

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<InitiatePaymentResponse>();
        return result ?? throw new InvalidOperationException("Payment service returned an empty response.");
    }
}
