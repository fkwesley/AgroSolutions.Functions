using Azure.Messaging.ServiceBus;
using Common.Notifications.Function.Functions;
using Common.Notifications.Function.Interfaces;
using Common.Notifications.Function.Logging;
using Common.Notifications.Function.Models;
using Common.Notifications.Function.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace UnitTests.Functions;

public class NotificationFunctionTests
{
    private readonly Mock<ILogger<NotificationFunction>> _loggerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IEmailTemplateService> _templateServiceMock;
    private readonly Mock<CorrelationIdEnricher> _correlationIdEnricherMock;
    private readonly Mock<ServiceBusMessageActions> _messageActionsMock;
    private readonly NotificationFunction _function;

    public NotificationFunctionTests()
    {
        _loggerMock = new Mock<ILogger<NotificationFunction>>();
        _emailServiceMock = new Mock<IEmailService>();
        _templateServiceMock = new Mock<IEmailTemplateService>();
        _correlationIdEnricherMock = new Mock<CorrelationIdEnricher>();
        _messageActionsMock = new Mock<ServiceBusMessageActions> { CallBase = false };

        _function = new NotificationFunction(
            _loggerMock.Object,
            _emailServiceMock.Object,
            _templateServiceMock.Object,
            _correlationIdEnricherMock.Object
        );
    }

    // =========================
    // INVALID JSON
    // =========================

    [Fact]
    public async Task Run_WithInvalidJson_ShouldDeadLetterMessage()
    {
        var correlationId = Guid.NewGuid().ToString();
        var invalidJson = "{ invalid json }";

        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: new BinaryData(Encoding.UTF8.GetBytes(invalidJson)),
            messageId: Guid.NewGuid().ToString(),
            correlationId: correlationId
        );

        await _function.Run(message, _messageActionsMock.Object);

        _messageActionsMock.Verify(
            x => x.DeadLetterMessageAsync(
                message,
                It.IsAny<Dictionary<string, object>>(),
                It.Is<string>(r => r == "InvalidJsonFormat"),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _emailServiceMock.Verify(
            x => x.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================
    // MISSING SUBJECT
    // =========================

    [Fact]
    public async Task Run_WithMissingSubjectInDirectMode_ShouldDeadLetterMessage()
    {
        var correlationId = Guid.NewGuid().ToString();

        var notificationRequest = new NotificationRequest
        {
            EmailTo = new List<string> { "test@example.com" },
            Body = "Test Body"
        };

        var message = CreateServiceBusMessage(notificationRequest, correlationId);

        await _function.Run(message, _messageActionsMock.Object);

        _messageActionsMock.Verify(
            x => x.DeadLetterMessageAsync(
                message,
                It.IsAny<Dictionary<string, object>>(),
                It.Is<string>(r => r == "ValidationError"),
                It.Is<string>(d => d.Contains("Subject required")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================
    // MISSING BODY
    // =========================

    [Fact]
    public async Task Run_WithMissingBodyInDirectMode_ShouldDeadLetterMessage()
    {
        var correlationId = Guid.NewGuid().ToString();

        var notificationRequest = new NotificationRequest
        {
            EmailTo = new List<string> { "test@example.com" },
            Subject = "Test Subject"
        };

        var message = CreateServiceBusMessage(notificationRequest, correlationId);

        await _function.Run(message, _messageActionsMock.Object);

        _messageActionsMock.Verify(
            x => x.DeadLetterMessageAsync(
                message,
                It.IsAny<Dictionary<string, object>>(),
                It.Is<string>(r => r == "ValidationError"),
                It.Is<string>(d => d.Contains("Body required")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================
    // TEMPLATE NOT FOUND
    // =========================

    [Fact]
    public async Task Run_WithTemplateNotFound_ShouldDeadLetterMessage()
    {
        var correlationId = Guid.NewGuid().ToString();
        var templateId = "non-existent-template";

        var notificationRequest = new NotificationRequest
        {
            EmailTo = new List<string> { "test@example.com" },
            TemplateId = templateId
        };

        var message = CreateServiceBusMessage(notificationRequest, correlationId);

        _templateServiceMock
            .Setup(x => x.GetSubject(templateId, It.IsAny<Dictionary<string, string>>()))
            .Throws(new KeyNotFoundException($"Template '{templateId}' not found"));

        await _function.Run(message, _messageActionsMock.Object);

        _messageActionsMock.Verify(
            x => x.DeadLetterMessageAsync(
                message,
                It.IsAny<Dictionary<string, object>>(),
                It.Is<string>(r => r == "TemplateNotFound"),
                It.Is<string>(d => d.Contains(templateId)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================
    // NULL BODY
    // =========================

    [Fact]
    public async Task Run_WithNullBody_ShouldDeadLetterMessage()
    {
        var correlationId = Guid.NewGuid().ToString();

        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: new BinaryData(Encoding.UTF8.GetBytes("null")),
            messageId: Guid.NewGuid().ToString(),
            correlationId: correlationId
        );

        await _function.Run(message, _messageActionsMock.Object);

        _messageActionsMock.Verify(
            x => x.DeadLetterMessageAsync(
                message,
                It.IsAny<Dictionary<string, object>>(),
                It.Is<string>(r => r == "DeserializationError"),
                It.Is<string>(d => d.Contains("Could not deserialize message body")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================
    // HELPER
    // =========================

    private static ServiceBusReceivedMessage CreateServiceBusMessage(
        NotificationRequest request,
        string? correlationId = null)
    {
        var json = JsonSerializer.Serialize(request);

        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: new BinaryData(Encoding.UTF8.GetBytes(json)),
            messageId: Guid.NewGuid().ToString(),
            correlationId: correlationId ?? Guid.NewGuid().ToString()
        );
    }
}
