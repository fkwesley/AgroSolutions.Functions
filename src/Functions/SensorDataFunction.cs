using System.Text.Json;
using Azure.Messaging.ServiceBus;
using AgroSolutions.Functions.Interfaces;
using AgroSolutions.Functions.Models;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroSolutions.Functions.Functions;

public class SensorDataFunction
{
    private readonly ILogger<SensorDataFunction> _logger;
    private readonly IApiClientService _apiClientService;
    private readonly IMessageTracingService _messageTracingService;
    private readonly string _telemetryApiUrl;
    private readonly string _telemetryApiToken;

    public SensorDataFunction(
        ILogger<SensorDataFunction> logger,
        IApiClientService apiClientService,
        IMessageTracingService messageTracingService,
        IConfiguration configuration)
    {
        _logger = logger;
        _apiClientService = apiClientService;
        _messageTracingService = messageTracingService;
        _telemetryApiUrl = configuration["TelemetryApi:Url"]
            ?? throw new InvalidOperationException("TelemetryApi:Url is not configured.");
        _telemetryApiToken = configuration["TelemetryApi:Token"]
            ?? throw new InvalidOperationException("TelemetryApi:Token is not configured.");
    }

    [Function("ProcessSensorData")]
    public async Task ProcessSensorData(
        [ServiceBusTrigger("sensor-data-received-queue", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        var tracingContext = _messageTracingService.ExtractTracingContext(message);

        var transaction = Agent.Tracer.StartTransaction(
            "ProcessSensorData",
            ApiConstants.TypeMessaging);

        try
        {
            transaction.SetLabel("CorrelationId", tracingContext.CorrelationId);
            transaction.SetLabel("MessageId", message.MessageId);

            _logger.LogInformation(
                "Processing sensor data message ID: {MessageId}, CorrelationId: {CorrelationId}",
                message.MessageId,
                tracingContext.CorrelationId);

            // ===============================
            // PARSE MESSAGE SPAN
            // ===============================
            var sensorDataRequest =
                await transaction.CaptureSpan(
                    "Parse Service Bus Message",
                    ApiConstants.TypeMessaging,
                    async () =>
                    {
                        var request = JsonSerializer.Deserialize<SensorDataRequest>(message.Body.ToString());

                        Agent.Tracer.CurrentSpan?.SetLabel("MessageSize", message.Body.Length);
                        Agent.Tracer.CurrentSpan?.SetLabel("EnqueuedTime", message.EnqueuedTime.ToString("O"));

                        return request;
                    },
                    "azureservicebus");

            if (sensorDataRequest == null)
            {
                _logger.LogError(
                    "Failed to deserialize sensor data for message ID: {MessageId}, CorrelationId: {CorrelationId}",
                    message.MessageId,
                    tracingContext.CorrelationId);

                await messageActions.DeadLetterMessageAsync(
                    message,
                    deadLetterReason: "DeserializationError",
                    deadLetterErrorDescription: "Could not deserialize sensor data",
                    propertiesToModify: null);

                return;
            }

            // ===============================
            // VALIDATION
            // ===============================
            var (isValid, errorReason, errorDescription) = ValidateSensorDataRequest(sensorDataRequest);

            if (!isValid)
            {
                _logger.LogError(
                    "Validation failed for sensor data message ID: {MessageId}, CorrelationId: {CorrelationId}, Reason: {Reason}",
                    message.MessageId,
                    tracingContext.CorrelationId,
                    errorReason);

                await messageActions.DeadLetterMessageAsync(
                    message,
                    deadLetterReason: errorReason!,
                    deadLetterErrorDescription: errorDescription!,
                    propertiesToModify: null);

                return;
            }

            // ===============================
            // TELEMETRY API CALL
            // ===============================
            await transaction.CaptureSpan(
                "Send Sensor Data to Telemetry API",
                ApiConstants.TypeExternal,
                async () =>
                {
                    await _apiClientService.SendAsync(_telemetryApiUrl, sensorDataRequest, tracingContext, _telemetryApiToken);
                },
                ApiConstants.SubtypeHttp);

            await messageActions.CompleteMessageAsync(message);

            _logger.LogInformation("Successfully processed sensor data message ID: {MessageId}", message.MessageId);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            transaction.CaptureException(ex);

            _logger.LogWarning(
                ex,
                "Rate limit exceeded for message ID: {MessageId}, CorrelationId: {CorrelationId}. Message will be retried.",
                message.MessageId,
                tracingContext.CorrelationId);

            // Abandona a mensagem para que ela volte para a fila e seja reprocessada
            await messageActions.AbandonMessageAsync(message);
        }
        catch (JsonException ex)
        {
            transaction.CaptureException(ex);

            await messageActions.DeadLetterMessageAsync(
                message,
                propertiesToModify: null,
                deadLetterReason: "InvalidJsonFormat",
                deadLetterErrorDescription: ex.Message,
                cancellationToken: CancellationToken.None);

            return;
        }
        catch (Exception ex)
        {
            transaction.CaptureException(ex);

            _logger.LogError(
                ex,
                "Error processing message ID: {MessageId}",
                message.MessageId);

            await messageActions.DeadLetterMessageAsync(message);
        }
        finally
        {
            transaction.End();
        }
    }

    private static (bool IsValid, string? ErrorReason, string? ErrorDescription) ValidateSensorDataRequest(SensorDataRequest request)
    {
        if (request.FieldId <= 0)
            return (false, "ValidationError", "FieldId is required");

        if (request.SoilMoisture < 0 || request.SoilMoisture > 100)
            return (false, "ValidationError", "Soil moisture must be between 0 and 100");

        if (request.AirTemperature < -50 || request.AirTemperature > 80)
            return (false, "ValidationError", "Air temperature must be between -50 and 80");

        if (request.Precipitation < 0)
            return (false, "ValidationError", "Precipitation cannot be negative");

        if (request.CollectedAt == default)
            return (false, "ValidationError", "Collection date is required");

        if (string.IsNullOrWhiteSpace(request.AlertEmailTo))
            return (false, "ValidationError", "Alert email is required");

        return (true, null, null);
    }
}
