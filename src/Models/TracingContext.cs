namespace AgroSolutions.Functions.Models;

public class TracingContext
{
    public string CorrelationId { get; init; } = default!;
    public string? Traceparent { get; init; }
}
