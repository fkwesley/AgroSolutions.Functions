using AgroSolutions.Functions.Interfaces;
using AgroSolutions.Functions.Logging;
using AgroSolutions.Functions.Models;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace AgroSolutions.Functions.Functions;

public class CollectSensorDataFunction
{
    private readonly ILogger<CollectSensorDataFunction> _logger;
    private readonly IApiClientService _apiClientService;
    private readonly ServiceInfoEnricher _serviceInfoEnricher;
    private readonly string _fieldsApiUrl;
    private readonly string _fieldsApiToken;
    private readonly string _alertEmailTo;
    private readonly string _functionServiceName;

    public CollectSensorDataFunction(
        ILogger<CollectSensorDataFunction> logger,
        IApiClientService apiClientService,
        ServiceInfoEnricher serviceInfoEnricher,
        IConfiguration configuration)
    {
        _logger = logger;
        _apiClientService = apiClientService;
        _serviceInfoEnricher = serviceInfoEnricher;
        _fieldsApiUrl = configuration["FieldsApi:Url"]
            ?? throw new InvalidOperationException("FieldsApi:Url is not configured.");
        _fieldsApiToken = configuration["FieldsApi:Token"]
            ?? throw new InvalidOperationException("FieldsApi:Token is not configured.");
        _alertEmailTo = configuration["CollectSensorData:AlertEmailTo"]
            ?? throw new InvalidOperationException("CollectSensorData:AlertEmailTo is not configured.");
        _functionServiceName = configuration["CollectSensorData:ServiceNames"] ?? "func-agro-collect-data";
    }

    [Function("CollectSensorData")]
    [ServiceBusOutput("sensor-data-received-queue", Connection = "ServiceBusConnection")]
    public async Task<string[]> CollectSensorData([TimerTrigger("0 0 0,12 * * *")] TimerInfo timerInfo)
    {                                                                   // Executa diariamente às 00:00 e 12:00
        _serviceInfoEnricher.SetServiceName(_functionServiceName);

        var transaction = Agent.Tracer.StartTransaction(
            "CollectSensorData",
            ApiConstants.TypeMessaging);

        try
        {
            transaction.SetLabel("FunctionServiceName", _functionServiceName);

            _logger.LogInformation("CollectSensorData triggered at {Time}", DateTime.UtcNow);

            // ===============================
            // FETCH ACTIVE FIELDS SPAN
            // ===============================
            var fields = await transaction.CaptureSpan(
                "Fetch Active Fields",
                ApiConstants.TypeExternal,
                async () =>
                {
                    var allFields = await _apiClientService.GetAsync<List<FieldResponse>>(_fieldsApiUrl, _fieldsApiToken);
                    var activeFields = allFields?.Where(f => f.IsActive).ToList();

                    Agent.Tracer.CurrentSpan?.SetLabel("TotalFields", allFields?.Count ?? 0);
                    Agent.Tracer.CurrentSpan?.SetLabel("ActiveFields", activeFields?.Count ?? 0);

                    return activeFields;
                },
                ApiConstants.SubtypeHttp);

            if (fields is not { Count: > 0 })
            {
                _logger.LogWarning("No active fields returned from {Url}", _fieldsApiUrl);
                return [];
            }

            _logger.LogInformation("Retrieved {Count} active field(s) from API", fields.Count);

            // ===============================
            // COLLECT WEATHER DATA SPAN
            // ===============================
            var messages = await transaction.CaptureSpan(
                "Collect Weather Data for Fields",
                ApiConstants.TypeExternal,
                async () =>
                {
                    var result = new List<string>(fields.Count);

                    foreach (var field in fields)
                    {
                        var (airTemperature, precipitation) = await GetWeatherDataAsync(field.Latitude, field.Longitude);

                        // SoilMoisture é mockado (não há API pública gratuita disponível)
                        var soilMoisture = Math.Round((decimal)(Random.Shared.NextDouble() * 60 + 20), 2);

                        var sensorData = new SensorDataRequest
                        {
                            FieldId = field.Id,
                            SoilMoisture = soilMoisture,
                            AirTemperature = airTemperature,
                            Precipitation = precipitation,
                            CollectedAt = DateTime.UtcNow,
                            AlertEmailTo = _alertEmailTo
                        };

                        result.Add(JsonSerializer.Serialize(sensorData));

                        _logger.LogInformation(
                            "Sensor data collected for FieldId={FieldId} ({FieldName}): Temperature={Temperature}°C, Moisture={Moisture}%, Precipitation={Precipitation}mm",
                            field.Id, field.Name, airTemperature, soilMoisture, precipitation);
                    }

                    Agent.Tracer.CurrentSpan?.SetLabel("MessagesCreated", result.Count);

                    return result;
                },
                ApiConstants.SubtypeHttp);

            // ===============================
            // PUBLISH TO QUEUE (via output binding)
            // ===============================
            transaction.SetLabel("MessagesPublished", messages.Count);

            _logger.LogInformation("Publishing {Count} sensor data message(s) to queue", messages.Count);

            return messages.ToArray();
        }
        catch (Exception ex)
        {
            transaction.CaptureException(ex);

            _logger.LogError(ex, "Error in CollectSensorData - {ErrorMessage}", ex.Message);
            throw;
        }
        finally
        {
            transaction.End();
        }
    }

    private async Task<(decimal AirTemperature, decimal Precipitation)> GetWeatherDataAsync(double latitude, double longitude)
    {
        try
        {
            var lat = latitude.ToString(CultureInfo.InvariantCulture);
            var lon = longitude.ToString(CultureInfo.InvariantCulture);
            var apiUrl = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,precipitation&timezone=auto";

            var weatherData = await _apiClientService.GetAsync<OpenMeteoResponse>(apiUrl);

            var airTemperature = (decimal)(weatherData?.Current?.Temperature2m ?? 25.0);
            var precipitation = (decimal)(weatherData?.Current?.Precipitation ?? 0.0);

            return (airTemperature, precipitation);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve weather data for lat={Latitude}, lon={Longitude}. Using mock data.", latitude, longitude);
            return (25.5m, 2.3m);
        }
    }
}
