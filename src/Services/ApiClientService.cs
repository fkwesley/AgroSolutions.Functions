using System.Text;
using System.Text.Json;
using AgroSolutions.Functions.Interfaces;
using AgroSolutions.Functions.Models;

namespace AgroSolutions.Functions.Services;

public class ApiClientService : IApiClientService
{
    private readonly HttpClient _httpClient;

    public ApiClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TResponse?> GetAsync<TResponse>(string url, string? token = null, IDictionary<string, string>? headers = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        if (headers != null)
        {
            foreach (var header in headers)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(json);
    }

    public Task SendAsync<T>(string url, T body, string? token = null, IDictionary<string, string>? headers = null)
    {
        return SendInternalAsync(url, body, token, headers);
    }

    public Task SendAsync<T>(string url, T body, TracingContext tracingContext, string? token = null, IDictionary<string, string>? headers = null)
    {
        var allHeaders = new Dictionary<string, string>(headers ?? new Dictionary<string, string>());

        if (!string.IsNullOrEmpty(tracingContext.CorrelationId))
            allHeaders.TryAdd("x-correlation-id", tracingContext.CorrelationId);

        // traceparent is automatically injected by the Elastic APM agent's HttpClient instrumentation
        return SendInternalAsync(url, body, token, allHeaders);
    }

    private async Task SendInternalAsync<T>(string url, T body, string? token, IDictionary<string, string>? headers)
    {
        var json = JsonSerializer.Serialize(body);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        if (headers != null)
        {
            foreach (var header in headers)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
