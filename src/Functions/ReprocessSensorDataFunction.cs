using AgroSolutions.Functions.Logging;
using Azure.Messaging.ServiceBus;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroSolutions.Functions.Functions;

public class ReprocessSensorDataFunction
{
    private readonly ILogger<ReprocessSensorDataFunction> _logger;
    private readonly ServiceInfoEnricher _serviceInfoEnricher;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly string _functionServiceName;

    private const string QueueName = "sensor-data-received-queue";
    private const int MaxDlqRetries = 3;
    private const int MaxMessagesPerRun = 50;

    public ReprocessSensorDataFunction(
        ILogger<ReprocessSensorDataFunction> logger,
        ServiceInfoEnricher serviceInfoEnricher,
        ServiceBusClient serviceBusClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceInfoEnricher = serviceInfoEnricher;
        _serviceBusClient = serviceBusClient;
        _functionServiceName = configuration["ReprocessSensorData:ServiceName"] ?? "func-agro-reprocess-sensor-data";
    }

    [Function("ReprocessSensorData")]
    public async Task ReprocessSensorData([TimerTrigger("0 0 3 * * *")] TimerInfo timerInfo)
    {
        _serviceInfoEnricher.SetServiceName(_functionServiceName);

        var transaction = Agent.Tracer.StartTransaction(
            "ReprocessSensorData",
            ApiConstants.TypeMessaging);

        var receiver = _serviceBusClient.CreateReceiver(QueueName,
            new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
        var sender = _serviceBusClient.CreateSender(QueueName);

        var reprocessed = 0;
        var discarded = 0;

        try
        {
            transaction.SetLabel("FunctionServiceName", _functionServiceName);

            _logger.LogInformation("ReprocessSensorData triggered at {Time}", DateTime.UtcNow);

            var messages = await receiver.ReceiveMessagesAsync(
                MaxMessagesPerRun, TimeSpan.FromSeconds(10));

            _logger.LogInformation("Found {Count} message(s) in DLQ", messages.Count);

            foreach (var message in messages)
            {
                var retryCount = message.ApplicationProperties.TryGetValue("DlqRetryCount", out var count)
                    ? (int)count
                    : 0;

                if (retryCount >= MaxDlqRetries)
                {
                    _logger.LogError(
                        "Message {MessageId} exceeded max DLQ retries ({MaxRetries}). " +
                        "DeadLetterReason: {Reason}. Removing permanently.",
                        message.MessageId, MaxDlqRetries, message.DeadLetterReason);

                    await receiver.CompleteMessageAsync(message);
                    discarded++;
                    continue;
                }

                var resubmitMessage = new ServiceBusMessage(message.Body)
                {
                    ContentType = message.ContentType,
                    Subject = message.Subject,
                    MessageId = $"{message.MessageId}-dlq-retry-{retryCount + 1}",
                    CorrelationId = message.CorrelationId
                };

                foreach (var prop in message.ApplicationProperties)
                    resubmitMessage.ApplicationProperties[prop.Key] = prop.Value;

                resubmitMessage.ApplicationProperties["DlqRetryCount"] = retryCount + 1;
                resubmitMessage.ApplicationProperties["DlqReprocessedAt"] = DateTime.UtcNow.ToString("O");
                resubmitMessage.ApplicationProperties["DlqOriginalReason"] = message.DeadLetterReason ?? "Unknown";

                await sender.SendMessageAsync(resubmitMessage);
                await receiver.CompleteMessageAsync(message);
                reprocessed++;

                _logger.LogInformation(
                    "Resubmitted message {MessageId} to queue (retry {RetryCount}/{MaxRetries})",
                    message.MessageId, retryCount + 1, MaxDlqRetries);
            }

            transaction.SetLabel("Reprocessed", reprocessed);
            transaction.SetLabel("Discarded", discarded);

            _logger.LogInformation(
                "ReprocessSensorData completed. Reprocessed: {Reprocessed}, Discarded: {Discarded}",
                reprocessed, discarded);
        }
        catch (Exception ex)
        {
            transaction.CaptureException(ex);
            _logger.LogError(ex, "Error in ReprocessSensorData - {ErrorMessage}", ex.Message);
            throw;
        }
        finally
        {
            await receiver.DisposeAsync();
            await sender.DisposeAsync();
            transaction.End();
        }
    }
}
