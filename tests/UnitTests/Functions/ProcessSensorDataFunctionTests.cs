using System.Text.Json;
using AgroSolutions.Functions.Functions;
using AgroSolutions.Functions.Interfaces;
using AgroSolutions.Functions.Logging;
using AgroSolutions.Functions.Models;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Functions;

public class ProcessSensorDataFunctionTests
{
    private readonly Mock<ILogger<ProcessSensorDataFunction>> _mockLogger;
    private readonly Mock<IApiClientService> _mockApiClient;
    private readonly Mock<IMessageTracingService> _mockTracingService;
    private readonly Mock<ServiceBusMessageActions> _mockMessageActions;
    private readonly ServiceInfoEnricher _serviceInfoEnricher;
    private readonly ProcessSensorDataFunction _sut;

    private static readonly TracingContext DefaultTracingContext = new()
    {
        CorrelationId = "test-correlation-id",
        Traceparent = "00-abc-def-01"
    };

    public ProcessSensorDataFunctionTests()
    {
        _mockLogger = new Mock<ILogger<ProcessSensorDataFunction>>();
        _mockApiClient = new Mock<IApiClientService>();
        _mockTracingService = new Mock<IMessageTracingService>();
        _mockMessageActions = new Mock<ServiceBusMessageActions>();
        _serviceInfoEnricher = new ServiceInfoEnricher("test-service", "test");

        _mockTracingService
            .Setup(x => x.ExtractTracingContext(It.IsAny<ServiceBusReceivedMessage>()))
            .Returns(DefaultTracingContext);

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["TelemetryApi:Url"]).Returns("https://test/telemetry");
        mockConfig.Setup(c => c["TelemetryApi:Token"]).Returns("test-token");
        mockConfig.Setup(c => c["ProcessSensorData:ServiceName"]).Returns("func-test-process");

        _sut = new ProcessSensorDataFunction(
            _mockLogger.Object,
            _mockApiClient.Object,
            _mockTracingService.Object,
            _serviceInfoEnricher,
            mockConfig.Object);
    }

    // ===============================
    // HAPPY PATH
    // ===============================

    [Fact]
    public async Task ProcessSensorData_WithValidMessage_CompletesMessage()
    {
        // Arrange
        var sensorData = CreateValidSensorData();
        var message = CreateMessage(sensorData);

        // Act
        await _sut.ProcessSensorData(message, _mockMessageActions.Object);

        // Assert
        _mockApiClient.Verify(x => x.SendAsync(
            "https://test/telemetry",
            It.Is<SensorDataRequest>(r => r.FieldId == 1),
            It.IsAny<TracingContext>(),
            "test-token",
            It.IsAny<IDictionary<string, string>?>()), Times.Once);

        _mockMessageActions.Verify(x => x.CompleteMessageAsync(
            message, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ===============================
    // DESERIALIZATION FAILURES
    // ===============================

    [Fact]
    public async Task ProcessSensorData_WhenDeserializationReturnsNull_DeadLettersMessage()
    {
        // Arrange
        var message = CreateMessage("null");

        // Act
        await _sut.ProcessSensorData(message, _mockMessageActions.Object);

        // Assert
        _mockMessageActions.Verify(x => x.DeadLetterMessageAsync(
            message,
            It.IsAny<Dictionary<string, object>?>(),
            "DeserializationError",
            "Could not deserialize sensor data",
            It.IsAny<CancellationToken>()), Times.Once);

        VerifyMessageNotCompleted(message);
    }

    [Fact]
    public async Task ProcessSensorData_WithInvalidJson_DeadLettersMessage()
    {
        // Arrange
        var message = CreateMessage("{ invalid json }}}");

        // Act
        await _sut.ProcessSensorData(message, _mockMessageActions.Object);

        // Assert
        _mockMessageActions.Verify(x => x.DeadLetterMessageAsync(
            message,
            It.IsAny<Dictionary<string, object>?>(),
            "InvalidJsonFormat",
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        VerifyMessageNotCompleted(message);
    }

    // ===============================
    // VALIDATION FAILURES
    // ===============================

    [Fact]
    public async Task ProcessSensorData_WithInvalidFieldId_DeadLettersMessage()
    {
        // Arrange
        var sensorData = CreateValidSensorData();
        sensorData.FieldId = 0;
        var message = CreateMessage(sensorData);

        // Act
        await _sut.ProcessSensorData(message, _mockMessageActions.Object);

        // Assert
        VerifyDeadLetterWithReason(message, "ValidationError", "FieldId is required");
        VerifyMessageNotCompleted(message);
    }

    [Fact]
    public async Task ProcessSensorData_WithSoilMoistureAbove100_DeadLettersMessage()
    {
        // Arrange
        var sensorData = CreateValidSensorData();
        sensorData.SoilMoisture = 150m;
        var message = CreateMessage(sensorData);

        // Act
        await _sut.ProcessSensorData(message, _mockMessageActions.Object);

        // Assert
        VerifyDeadLetterWithReason(message, "ValidationError", "Soil moisture must be between 0 and 100");
    }

    [Fact]
    public async Task ProcessSensorData_WithNegativeSoilMoisture_DeadLettersMessage()
    {
        // Arrange
        var sensorData = CreateValidSensorData();
        sensorData.SoilMoisture = -1m;
        var message = CreateMessage(sensorData);

        // Act
        await _sut.ProcessSensorData(message, _mockMessageActions.Object);

        // Assert
        VerifyDeadLetterWithReason(message, "ValidationError", "Soil moisture must be between 0 and 100");
    }

    [Fact]
    public async Task ProcessSensorData_WithAirTemperatureAbove80_DeadLettersMessage()
    {
        // Arrange
        var sensorData = CreateValidSensorData();
        sensorData.AirTemperature = 81m;
        var message = CreateMessage(sensorData);

        // Act
        await _sut.ProcessSensorData(message, _mockMessageActions.Object);

        // Assert
        VerifyDeadLetterWithReason(message, "ValidationError", "Air temperature must be between -50 and 80");
    }

    [Fact]
    public async Task ProcessSensorData_WithAirTemperatureBelowMinus50_DeadLettersMessage()
    {
        // Arrange
        var sensorData = CreateValidSensorData();
        sensorData.AirTemperature = -51m;
        var message = CreateMessage(sensorData);

        // Act
        await _sut.ProcessSensorData(message, _mockMessageActions.Object);

        // Assert
        VerifyDeadLetterWithReason(message, "ValidationError", "Air temperature must be between -50 and 80");
    }

    [Fact]
    public async Task ProcessSensorData_WithNegativePrecipitation_DeadLettersMessage()
    {
        // Arrange
        var sensorData = CreateValidSensorData();
        sensorData.Precipitation = -1m;
        var message = CreateMessage(sensorData);

        // Act
        await _sut.ProcessSensorData(message, _mockMessageActions.Object);

        // Assert
        VerifyDeadLetterWithReason(message, "ValidationError", "Precipitation cannot be negative");
    }

    [Fact]
    public async Task ProcessSensorData_WithDefaultCollectedAt_DeadLettersMessage()
    {
        // Arrange
        var sensorData = CreateValidSensorData();
        sensorData.CollectedAt = default;
        var message = CreateMessage(sensorData);

        // Act
        await _sut.ProcessSensorData(message, _mockMessageActions.Object);

        // Assert
        VerifyDeadLetterWithReason(message, "ValidationError", "Collection date is required");
    }

    [Fact]
    public async Task ProcessSensorData_WithMissingAlertEmail_DeadLettersMessage()
    {
        // Arrange
        var sensorData = CreateValidSensorData();
        sensorData.AlertEmailTo = "";
        var message = CreateMessage(sensorData);

        // Act
        await _sut.ProcessSensorData(message, _mockMessageActions.Object);

        // Assert
        VerifyDeadLetterWithReason(message, "ValidationError", "Alert email is required");
    }

    // ===============================
    // ERROR HANDLING
    // ===============================

    [Fact]
    public async Task ProcessSensorData_OnRateLimit_AbandonsMessage()
    {
        // Arrange
        var message = CreateMessage(CreateValidSensorData());

        _mockApiClient
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<SensorDataRequest>(),
                It.IsAny<TracingContext>(),
                It.IsAny<string?>(),
                It.IsAny<IDictionary<string, string>?>()))
            .ThrowsAsync(new HttpRequestException("Rate limit", null, System.Net.HttpStatusCode.TooManyRequests));

        // Act
        await _sut.ProcessSensorData(message, _mockMessageActions.Object);

        // Assert
        _mockMessageActions.Verify(x => x.AbandonMessageAsync(
            message,
            It.IsAny<Dictionary<string, object>?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        VerifyMessageNotCompleted(message);
    }

    [Fact]
    public async Task ProcessSensorData_OnGenericException_DeadLettersMessage()
    {
        // Arrange
        var message = CreateMessage(CreateValidSensorData());

        _mockApiClient
            .Setup(x => x.SendAsync(
                It.IsAny<string>(),
                It.IsAny<SensorDataRequest>(),
                It.IsAny<TracingContext>(),
                It.IsAny<string?>(),
                It.IsAny<IDictionary<string, string>?>()))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        // Act
        await _sut.ProcessSensorData(message, _mockMessageActions.Object);

        // Assert
        _mockMessageActions.Verify(x => x.DeadLetterMessageAsync(
            message,
            It.IsAny<Dictionary<string, object>?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        VerifyMessageNotCompleted(message);
    }

    // ===============================
    // HELPERS
    // ===============================

    private static SensorDataRequest CreateValidSensorData() => new()
    {
        FieldId = 1,
        SoilMoisture = 45.5m,
        AirTemperature = 28.0m,
        Precipitation = 1.2m,
        CollectedAt = DateTime.UtcNow,
        AlertEmailTo = "test@test.com"
    };

    private static ServiceBusReceivedMessage CreateMessage(object body, string messageId = "test-msg-id")
    {
        var json = body is string s ? s : JsonSerializer.Serialize(body);
        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(json),
            messageId: messageId,
            enqueuedTime: DateTimeOffset.UtcNow);
    }

    private void VerifyDeadLetterWithReason(ServiceBusReceivedMessage message, string reason, string description)
    {
        _mockMessageActions.Verify(x => x.DeadLetterMessageAsync(
            message,
            It.IsAny<Dictionary<string, object>?>(),
            reason,
            description,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private void VerifyMessageNotCompleted(ServiceBusReceivedMessage message)
    {
        _mockMessageActions.Verify(x => x.CompleteMessageAsync(
            message, It.IsAny<CancellationToken>()), Times.Never);
    }
}
