using AgroSolutions.Functions.Models;

namespace AgroSolutions.Functions.Interfaces;

public interface IApiClientService
{
    Task<TResponse?> GetAsync<TResponse>(string url, string? token = null, IDictionary<string, string>? headers = null);
    Task SendAsync<T>(string url, T body, string? token = null, IDictionary<string, string>? headers = null);
    Task SendAsync<T>(string url, T body, TracingContext tracingContext, string? token = null, IDictionary<string, string>? headers = null);
}
