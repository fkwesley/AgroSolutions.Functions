using Serilog.Core;
using Serilog.Events;

namespace AgroSolutions.Functions.Logging;

public class CorrelationIdEnricher : ILogEventEnricher
{
    private const string CorrelationIdPropertyName = "CorrelationId";
    private readonly AsyncLocal<string?> _correlationId = new();

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!string.IsNullOrEmpty(_correlationId.Value))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(CorrelationIdPropertyName, _correlationId.Value));
        }
    }

    public void SetCorrelationId(string? correlationId)
    {
        _correlationId.Value = correlationId;
    }

    public string? GetCorrelationId()
    {
        return _correlationId.Value;
    }
}
