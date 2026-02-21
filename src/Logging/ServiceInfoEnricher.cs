using Serilog.Core;
using Serilog.Events;

namespace AgroSolutions.Functions.Logging;

public class ServiceInfoEnricher : ILogEventEnricher
{
    private readonly string _serviceName;
    private readonly string _environment;

    public ServiceInfoEnricher(string serviceName, string environment)
    {
        _serviceName = serviceName;
        _environment = environment;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ServiceName", _serviceName));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Environment", _environment));
    }
}
