using AgroSolutions.Functions.Functions;
using AgroSolutions.Functions.Logging;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Functions;

public class ReprocessSensorDataFunctionTests
{
    private readonly Mock<ILogger<ReprocessSensorDataFunction>> _mockLogger;
    private readonly Mock<ServiceBusClient> _mockServiceBusClient;
    private readonly Mock<ServiceBusReceiver> _mockReceiver;
    private readonly Mock<ServiceBusSender> _mockSender;
    private readonly ServiceInfoEnricher _serviceInfoEnricher;
    private readonly ReprocessSensorDataFunction _sut;

    public ReprocessSensorDataFunctionTests()
    {
        _mockLogger = new Mock<ILogger<ReprocessSensorDataFunction>>();
        _mockServiceBusClient = new Mock<ServiceBusClient>();
        _mockReceiver = new Mock<ServiceBusReceiver>();
        _mockSender = new Mock<ServiceBusSender>();
        _serviceInfoEnricher = new ServiceInfoEnricher("test-service", "test");

        _mockServiceBusClient
            .Setup(x => x.CreateReceiver(It.IsAny<string>(), It.IsAny<ServiceBusReceiverOptions>()))
            .Returns(_mockReceiver.Object);

        _mockServiceBusClient
            .Setup(x => x.CreateSender(It.IsAny<string>()))
            .Returns(_mockSender.Object);

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["ReprocessSensorData:ServiceName"]).Returns("func-test-reprocess");

        _sut = new ReprocessSensorDataFunction(
            _mockLogger.Object,
            _serviceInfoEnricher,
            _mockServiceBusClient.Object,
            mockConfig.Object);
    }

    // ===============================
    // EMPTY DLQ
    // ===============================

    [Fact]
    public async Task ReprocessSensorData_WhenDlqIsEmpty_CompletesWithoutProcessing()
    {
        // Arrange
        SetupDlqMessages([]);

        // Act
        await _sut.ReprocessSensorData(new TimerInfo());

        // Assert
        _mockSender.Verify(x => x.SendMessageAsync(
            It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockReceiver.Verify(x => x.CompleteMessageAsync(
            It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ===============================
    // RESUBMIT (retry < max)
    // ===============================

    [Fact]
    public async Task ReprocessSensorData_WithFirstRetryMessage_ResubmitsToQueue()
    {
        // Arrange
        var message = CreateDlqMessage("msg-1", retryCount: 0);
        SetupDlqMessages([message]);

        // Act
        await _sut.ReprocessSensorData(new TimerInfo());

        // Assert
        _mockSender.Verify(x => x.SendMessageAsync(
            It.Is<ServiceBusMessage>(m =>
                m.MessageId == "msg-1-dlq-retry-1" &&
                (int)m.ApplicationProperties["DlqRetryCount"] == 1 &&
                m.ApplicationProperties.ContainsKey("DlqOriginalReason")),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockReceiver.Verify(x => x.CompleteMessageAsync(
            message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReprocessSensorData_WithSecondRetryMessage_ResubmitsWithIncrementedCount()
    {
        // Arrange
        var message = CreateDlqMessage("msg-2", retryCount: 2);
        SetupDlqMessages([message]);

        // Act
        await _sut.ReprocessSensorData(new TimerInfo());

        // Assert
        _mockSender.Verify(x => x.SendMessageAsync(
            It.Is<ServiceBusMessage>(m =>
                m.MessageId == "msg-2-dlq-retry-3" &&
                (int)m.ApplicationProperties["DlqRetryCount"] == 3),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReprocessSensorData_ResubmittedMessage_ContainsReprocessMetadata()
    {
        // Arrange
        var message = CreateDlqMessage("msg-meta", retryCount: 0);
        SetupDlqMessages([message]);

        ServiceBusMessage? capturedMessage = null;
        _mockSender
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ReprocessSensorData(new TimerInfo());

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.ApplicationProperties.Should().ContainKey("DlqReprocessedAt");
        capturedMessage.ApplicationProperties.Should().ContainKey("DlqOriginalReason");
    }

    [Fact]
    public async Task ReprocessSensorData_ResubmittedMessage_PreservesOriginalBody()
    {
        // Arrange
        var body = """{"fieldId":42,"soilMoisture":35.5}""";
        var message = CreateDlqMessage("msg-body", retryCount: 0, body: body);
        SetupDlqMessages([message]);

        ServiceBusMessage? capturedMessage = null;
        _mockSender
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ReprocessSensorData(new TimerInfo());

        // Assert
        capturedMessage.Should().NotBeNull();
        capturedMessage!.Body.ToString().Should().Be(body);
    }

    // ===============================
    // DISCARD (retry >= max)
    // ===============================

    [Fact]
    public async Task ReprocessSensorData_WithMaxRetriesReached_DiscardsMessage()
    {
        // Arrange
        var message = CreateDlqMessage("msg-discard", retryCount: 3);
        SetupDlqMessages([message]);

        // Act
        await _sut.ReprocessSensorData(new TimerInfo());

        // Assert
        _mockReceiver.Verify(x => x.CompleteMessageAsync(
            message, It.IsAny<CancellationToken>()), Times.Once);

        _mockSender.Verify(x => x.SendMessageAsync(
            It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReprocessSensorData_WithRetriesAboveMax_DiscardsMessage()
    {
        // Arrange
        var message = CreateDlqMessage("msg-over", retryCount: 5);
        SetupDlqMessages([message]);

        // Act
        await _sut.ReprocessSensorData(new TimerInfo());

        // Assert
        _mockReceiver.Verify(x => x.CompleteMessageAsync(
            message, It.IsAny<CancellationToken>()), Times.Once);

        _mockSender.Verify(x => x.SendMessageAsync(
            It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ===============================
    // MIXED BATCH
    // ===============================

    [Fact]
    public async Task ReprocessSensorData_WithMixedMessages_ResubmitsAndDiscardsCorrectly()
    {
        // Arrange
        var resubmitMsg1 = CreateDlqMessage("msg-retry-0", retryCount: 0);
        var resubmitMsg2 = CreateDlqMessage("msg-retry-2", retryCount: 2);
        var discardMsg = CreateDlqMessage("msg-retry-3", retryCount: 3);
        SetupDlqMessages([resubmitMsg1, resubmitMsg2, discardMsg]);

        // Act
        await _sut.ReprocessSensorData(new TimerInfo());

        // Assert - 2 resubmitted, 1 discarded
        _mockSender.Verify(x => x.SendMessageAsync(
            It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        // All 3 completed (removed from DLQ)
        _mockReceiver.Verify(x => x.CompleteMessageAsync(
            It.IsAny<ServiceBusReceivedMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    // ===============================
    // NO RETRY COUNT PROPERTY
    // ===============================

    [Fact]
    public async Task ReprocessSensorData_WithoutDlqRetryCountProperty_TreatsAsFirstRetry()
    {
        // Arrange
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            messageId: "msg-no-prop");
        SetupDlqMessages([message]);

        // Act
        await _sut.ReprocessSensorData(new TimerInfo());

        // Assert
        _mockSender.Verify(x => x.SendMessageAsync(
            It.Is<ServiceBusMessage>(m =>
                (int)m.ApplicationProperties["DlqRetryCount"] == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ===============================
    // ERROR HANDLING
    // ===============================

    [Fact]
    public async Task ReprocessSensorData_WhenReceiverThrows_PropagatesException()
    {
        // Arrange
        _mockReceiver
            .Setup(x => x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceBusException("Connection lost", ServiceBusFailureReason.ServiceCommunicationProblem));

        // Act
        var act = () => _sut.ReprocessSensorData(new TimerInfo());

        // Assert
        await act.Should().ThrowAsync<ServiceBusException>();
    }

    // ===============================
    // HELPERS
    // ===============================

    private void SetupDlqMessages(List<ServiceBusReceivedMessage> messages)
    {
        _mockReceiver
            .Setup(x => x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);
    }

    private static ServiceBusReceivedMessage CreateDlqMessage(
        string messageId,
        int retryCount = 0,
        string body = "{}")
    {
        var properties = new Dictionary<string, object> { ["DlqRetryCount"] = retryCount };

        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(body),
            messageId: messageId,
            properties: properties);
    }
}
