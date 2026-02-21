using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using AgroSolutions.Functions.Interfaces;
using AgroSolutions.Functions.Logging;
using AgroSolutions.Functions.Models;

namespace AgroSolutions.Functions.Services;

public class MessageTracingService : IMessageTracingService
{
    private readonly CorrelationIdEnricher _correlationIdEnricher;

    public MessageTracingService(CorrelationIdEnricher correlationIdEnricher)
    {
        _correlationIdEnricher = correlationIdEnricher;
    }

    public TracingContext ExtractTracingContext(ServiceBusReceivedMessage message)
    {
        // CorrelationId: mensagem > ApplicationProperties > Activity > novo Guid
        string? correlationId = message.CorrelationId;

        if (string.IsNullOrEmpty(correlationId) && message.ApplicationProperties.TryGetValue("CorrelationId", out var customCorrelationId))
            correlationId = customCorrelationId?.ToString();
        
        if (string.IsNullOrEmpty(correlationId))
            correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();

        if (Guid.TryParse(correlationId, out var parsedGuid))
            correlationId = parsedGuid.ToString("D");

        _correlationIdEnricher.SetCorrelationId(correlationId);

        // Traceparent: mensagem > Activity.Current
        string? traceparent = message.ApplicationProperties.TryGetValue("traceparent", out var traceparentValue)
            ? traceparentValue?.ToString()
            : null;

        if (string.IsNullOrEmpty(traceparent))
            traceparent = Activity.Current?.Id;

        return new TracingContext
        {
            CorrelationId = correlationId,
            Traceparent = traceparent
        };
    }
}
