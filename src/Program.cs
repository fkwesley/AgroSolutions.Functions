using AgroSolutions.Functions.Interfaces;
using AgroSolutions.Functions.Logging;
using AgroSolutions.Functions.Services;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

var builder = FunctionsApplication.CreateBuilder(args);

// Configura Serilog
var configuration = builder.Configuration;
var serviceName = configuration["ElasticApm:ServiceName"] ?? "func-agro";
var environment = configuration["ElasticApm:Environment"] ?? "Development";
var elasticEndpoint = configuration["ElasticLogs:Endpoint"];
var elasticApiKey = configuration["ElasticLogs:ApiKey"];
var indexPrefix = configuration["ElasticLogs:IndexPrefix"] ?? "agro";

// Cria enrichers customizados
var correlationIdEnricher = new CorrelationIdEnricher();
var serviceInfoEnricher = new ServiceInfoEnricher(serviceName, environment);

var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.WithThreadId()
    .Enrich.WithMachineName()
    .Enrich.With(correlationIdEnricher)
    .Enrich.With(serviceInfoEnricher)
    .WriteTo.Console();

// Adiciona sink do Elasticsearch se configurado
if (!string.IsNullOrEmpty(elasticEndpoint) && !string.IsNullOrEmpty(elasticApiKey))
{
    var elasticOptions = new ElasticsearchSinkOptions(new Uri(elasticEndpoint))
    {
        AutoRegisterTemplate = true,
        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
        IndexFormat = $"{indexPrefix}-traces",
        ModifyConnectionSettings = conn => conn.ApiKeyAuthentication(new Elasticsearch.Net.ApiKeyAuthenticationCredentials(elasticApiKey)),
        EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog,
        FailureCallback = (logEvent, ex) => Console.WriteLine($"Unable to submit event to Elasticsearch: {logEvent.MessageTemplate}, Error: {ex?.Message}"),
        BatchPostingLimit = 50,
        Period = TimeSpan.FromSeconds(2)
    };

    loggerConfiguration.WriteTo.Elasticsearch(elasticOptions);
}

Log.Logger = loggerConfiguration.CreateLogger();

// Registra os enrichers no DI para serem usados nas functions
builder.Services.AddSingleton(correlationIdEnricher);
builder.Services.AddSingleton(serviceInfoEnricher);

// Configura Serilog como provider de logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddSerilog(dispose: true);
});

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Registra os serviços de tracing e API
builder.Services.AddSingleton<IMessageTracingService, MessageTracingService>();
builder.Services.AddHttpClient<IApiClientService, ApiClientService>();

// Registra ServiceBusClient para acesso direto à DLQ (ReprocessSensorDataFunction)
var serviceBusConnectionString = configuration["ServiceBusConnection"];
if (!string.IsNullOrEmpty(serviceBusConnectionString))
    builder.Services.AddSingleton(new ServiceBusClient(serviceBusConnectionString));

// Configura Elastic APM para Azure Functions
var elasticEnabled = configuration.GetValue<bool>("ElasticApm:Enabled");

if (elasticEnabled)
{
    // Para Azure Functions Isolated Process, o APM é inicializado automaticamente
    // através da detecção de configuração no appsettings.json ou environment variables
    Environment.SetEnvironmentVariable("ELASTIC_APM_SERVER_URL", configuration["ElasticApm:ServerUrl"]);
    Environment.SetEnvironmentVariable("ELASTIC_APM_SECRET_TOKEN", configuration["ElasticApm:SecretToken"]);
    Environment.SetEnvironmentVariable("ELASTIC_APM_SERVICE_NAME", configuration["ElasticApm:ServiceName"]);
    Environment.SetEnvironmentVariable("ELASTIC_APM_ENVIRONMENT", configuration["ElasticApm:Environment"]);
}

try
{
    Log.Information("Starting Azure Function: {ServiceName} in {Environment}", serviceName, environment);
    builder.Build().Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
