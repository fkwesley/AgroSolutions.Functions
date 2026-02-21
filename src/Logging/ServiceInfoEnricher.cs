using Serilog.Core;
using Serilog.Events;

namespace AgroSolutions.Functions.Logging;

public class ServiceInfoEnricher : ILogEventEnricher
{
    private readonly string _defaultServiceName;
    private readonly string _environment;
    private readonly AsyncLocal<string?> _serviceName = new();

    public ServiceInfoEnricher(string defaultServiceName, string environment)
    {
        _defaultServiceName = defaultServiceName;
        _environment = environment;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var serviceName = _serviceName.Value ?? _defaultServiceName;
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ServiceName", serviceName));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Environment", _environment));
    }

    public void SetServiceName(string? serviceName) => _serviceName.Value = serviceName;

    public string GetServiceName() => _serviceName.Value ?? _defaultServiceName;
}
