using Common.Notifications.Function.Logging;
using FluentAssertions;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace UnitTests.Logging;

public class CorrelationIdEnricherTests
{
    [Fact]
    public void SetCorrelationId_ShouldStoreValue()
    {
        var enricher = new CorrelationIdEnricher();
        var correlationId = Guid.NewGuid().ToString();

        enricher.SetCorrelationId(correlationId);

        // Não podemos verificar diretamente pois o campo é privado,
        // mas podemos validar através do Enrich
        var logEvent = CreateLogEvent();
        var factory = new LogEventPropertyFactory();
        enricher.Enrich(logEvent, factory);

        logEvent.Properties.Should().ContainKey("CorrelationId");
        logEvent.Properties["CorrelationId"].ToString().Should().Contain(correlationId);
    }

    [Fact]
    public void SetCorrelationId_WithNull_ShouldNotAddProperty()
    {
        var enricher = new CorrelationIdEnricher();

        enricher.SetCorrelationId(null);

        var logEvent = CreateLogEvent();
        var factory = new LogEventPropertyFactory();
        enricher.Enrich(logEvent, factory);

        // Quando null, a propriedade não é adicionada
        logEvent.Properties.Should().NotContainKey("CorrelationId");
    }

    [Fact]
    public void SetCorrelationId_WithEmptyString_ShouldNotAddProperty()
    {
        var enricher = new CorrelationIdEnricher();

        enricher.SetCorrelationId("");

        var logEvent = CreateLogEvent();
        var factory = new LogEventPropertyFactory();
        enricher.Enrich(logEvent, factory);

        // Quando vazio, a propriedade não é adicionada
        logEvent.Properties.Should().NotContainKey("CorrelationId");
    }

    [Fact]
    public void Enrich_WithCorrelationId_ShouldAddToLogEvent()
    {
        var enricher = new CorrelationIdEnricher();
        var correlationId = Guid.NewGuid().ToString();
        enricher.SetCorrelationId(correlationId);

        var logEvent = CreateLogEvent();
        var factory = new LogEventPropertyFactory();
        enricher.Enrich(logEvent, factory);

        logEvent.Properties.Should().ContainKey("CorrelationId");
        var value = logEvent.Properties["CorrelationId"].ToString().Trim('"');
        value.Should().Be(correlationId);
    }

    [Fact]
    public void Enrich_WithoutCorrelationId_ShouldNotAddProperty()
    {
        var enricher = new CorrelationIdEnricher();

        var logEvent = CreateLogEvent();
        var factory = new LogEventPropertyFactory();
        enricher.Enrich(logEvent, factory);

        logEvent.Properties.Should().NotContainKey("CorrelationId");
    }

    [Fact]
    public void Enrich_MultipleTimes_ShouldUpdateValue()
    {
        var enricher = new CorrelationIdEnricher();
        var correlationId1 = Guid.NewGuid().ToString();
        var correlationId2 = Guid.NewGuid().ToString();
        var factory = new LogEventPropertyFactory();

        enricher.SetCorrelationId(correlationId1);
        var logEvent1 = CreateLogEvent();
        enricher.Enrich(logEvent1, factory);

        enricher.SetCorrelationId(correlationId2);
        var logEvent2 = CreateLogEvent();
        enricher.Enrich(logEvent2, factory);

        logEvent1.Properties["CorrelationId"].ToString().Should().Contain(correlationId1);
        logEvent2.Properties["CorrelationId"].ToString().Should().Contain(correlationId2);
    }

    [Fact]
    public void GetCorrelationId_ShouldReturnStoredValue()
    {
        var enricher = new CorrelationIdEnricher();
        var correlationId = Guid.NewGuid().ToString();

        enricher.SetCorrelationId(correlationId);
        var result = enricher.GetCorrelationId();

        result.Should().Be(correlationId);
    }

    [Fact]
    public void GetCorrelationId_WhenNotSet_ShouldReturnNull()
    {
        var enricher = new CorrelationIdEnricher();

        var result = enricher.GetCorrelationId();

        result.Should().BeNull();
    }

    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test", new List<MessageTemplateToken>()),
            new List<LogEventProperty>());
    }

    private class LogEventPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }
    }
}

public class ServiceInfoEnricherTests
{
    [Fact]
    public void Constructor_ShouldStoreServiceNameAndEnvironment()
    {
        var serviceName = "TestService";
        var environment = "Production";

        var enricher = new ServiceInfoEnricher(serviceName, environment);

        // Validamos através do Enrich
        var logEvent = CreateLogEvent();
        var factory = new LogEventPropertyFactory();
        enricher.Enrich(logEvent, factory);

        logEvent.Properties.Should().ContainKey("ServiceName");
        logEvent.Properties.Should().ContainKey("Environment");
    }

    [Fact]
    public void Enrich_ShouldAddServiceNameToLogEvent()
    {
        var serviceName = "TestService";
        var environment = "Development";
        var enricher = new ServiceInfoEnricher(serviceName, environment);

        var logEvent = CreateLogEvent();
        var factory = new LogEventPropertyFactory();
        enricher.Enrich(logEvent, factory);

        logEvent.Properties.Should().ContainKey("ServiceName");
        logEvent.Properties["ServiceName"].ToString().Should().Contain(serviceName);
    }

    [Fact]
    public void Enrich_ShouldAddEnvironmentToLogEvent()
    {
        var serviceName = "TestService";
        var environment = "Staging";
        var enricher = new ServiceInfoEnricher(serviceName, environment);

        var logEvent = CreateLogEvent();
        var factory = new LogEventPropertyFactory();
        enricher.Enrich(logEvent, factory);

        logEvent.Properties.Should().ContainKey("Environment");
        logEvent.Properties["Environment"].ToString().Should().Contain(environment);
    }

    [Theory]
    [InlineData("func-notifications", "Development")]
    [InlineData("func-notifications", "Staging")]
    [InlineData("func-notifications", "Production")]
    [InlineData("notification-service", "Test")]
    public void Enrich_WithDifferentValues_ShouldAddCorrectValues(string serviceName, string environment)
    {
        var enricher = new ServiceInfoEnricher(serviceName, environment);

        var logEvent = CreateLogEvent();
        var factory = new LogEventPropertyFactory();
        enricher.Enrich(logEvent, factory);

        logEvent.Properties["ServiceName"].ToString().Should().Contain(serviceName);
        logEvent.Properties["Environment"].ToString().Should().Contain(environment);
    }

    [Fact]
    public void Enrich_ShouldAddBothProperties()
    {
        var enricher = new ServiceInfoEnricher("TestService", "Production");

        var logEvent = CreateLogEvent();
        var factory = new LogEventPropertyFactory();
        enricher.Enrich(logEvent, factory);

        logEvent.Properties.Should().HaveCountGreaterOrEqualTo(2);
        logEvent.Properties.Keys.Should().Contain("ServiceName");
        logEvent.Properties.Keys.Should().Contain("Environment");
    }

    [Fact]
    public void Enrich_MultipleLogEvents_ShouldEnrichAll()
    {
        var enricher = new ServiceInfoEnricher("TestService", "Development");
        var factory = new LogEventPropertyFactory();

        var logEvent1 = CreateLogEvent();
        var logEvent2 = CreateLogEvent();
        var logEvent3 = CreateLogEvent();

        enricher.Enrich(logEvent1, factory);
        enricher.Enrich(logEvent2, factory);
        enricher.Enrich(logEvent3, factory);

        logEvent1.Properties.Should().ContainKey("ServiceName");
        logEvent2.Properties.Should().ContainKey("ServiceName");
        logEvent3.Properties.Should().ContainKey("ServiceName");
    }

    [Fact]
    public void Enrich_WithEmptyServiceName_ShouldStillWork()
    {
        var enricher = new ServiceInfoEnricher("", "Development");

        var logEvent = CreateLogEvent();
        var factory = new LogEventPropertyFactory();
        enricher.Enrich(logEvent, factory);

        logEvent.Properties.Should().ContainKey("ServiceName");
        logEvent.Properties.Should().ContainKey("Environment");
    }

    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test", new List<MessageTemplateToken>()),
            new List<LogEventProperty>());
    }

    private class LogEventPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }
    }
}