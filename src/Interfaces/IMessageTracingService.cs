using Azure.Messaging.ServiceBus;
using AgroSolutions.Functions.Models;

namespace AgroSolutions.Functions.Interfaces;

public interface IMessageTracingService
{
    TracingContext ExtractTracingContext(ServiceBusReceivedMessage message);
}
