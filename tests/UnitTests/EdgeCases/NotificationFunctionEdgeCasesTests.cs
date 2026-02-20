using Azure.Messaging.ServiceBus;
using Common.Notifications.Function.Functions;
using Common.Notifications.Function.Interfaces;
using Common.Notifications.Function.Logging;
using Common.Notifications.Function.Models;
using Common.Notifications.Function.Services;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;

namespace UnitTests.EdgeCases;

public class NotificationFunctionEdgeCasesTests
{
    private readonly Mock<ILogger<NotificationFunction>> _loggerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IEmailTemplateService> _templateServiceMock;
    private readonly Mock<CorrelationIdEnricher> _correlationIdEnricherMock;
    private readonly Mock<ServiceBusMessageActions> _messageActionsMock;
    private readonly NotificationFunction _function;

    public NotificationFunctionEdgeCasesTests()
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

    [Fact]
    public async Task Run_WithVeryLargeEmailList_ShouldProcessAll()
    {
        var correlationId = Guid.NewGuid().ToString();
        var largeEmailList = Enumerable.Range(1, 100)
            .Select(i => $"user{i}@test.com")
            .ToList();

        var notificationRequest = new NotificationRequest
        {
            EmailTo = largeEmailList,
            Subject = "Test",
            Body = "Test Body",        };

        var message = CreateServiceBusMessage(notificationRequest, correlationId);

        _emailServiceMock
            .Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _function.Run(message, _messageActionsMock.Object);

        _emailServiceMock.Verify(
            x => x.SendEmailAsync(
                It.Is<EmailMessage>(e => e.To.Count == 100),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _messageActionsMock.Verify(
            x => x.CompleteMessageAsync(message, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_WithSpecialCharactersInSubject_ShouldHandleCorrectly()
    {
        var correlationId = Guid.NewGuid().ToString();
        var specialSubject = "Teste com çãõ áéíóú àèìòù âêîôû <>&\"'";

        var notificationRequest = new NotificationRequest
        {
            EmailTo = new List<string> { "test@test.com" },
            Subject = specialSubject,
            Body = "Test Body",        };

        var message = CreateServiceBusMessage(notificationRequest, correlationId);

        _emailServiceMock
            .Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _function.Run(message, _messageActionsMock.Object);

        _emailServiceMock.Verify(
            x => x.SendEmailAsync(
                It.Is<EmailMessage>(e => e.Subject == specialSubject),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_WithSpecialCharactersInParameters_ShouldHandleCorrectly()
    {
        var correlationId = Guid.NewGuid().ToString();
        var templateId = "HeatStress";

        var notificationRequest = new NotificationRequest
        {
            EmailTo = new List<string> { "test@test.com" },
            TemplateId = templateId,
            Parameters = new Dictionary<string, string>
            {
                { "{fieldId}", "Café São João" },
                { "{stressLevel}", "Crítico <ALTO>" },
                { "{correlationId}", correlationId }
            },        };

        var message = CreateServiceBusMessage(notificationRequest, correlationId);

        _templateServiceMock
            .Setup(x => x.GetSubject(templateId, It.IsAny<Dictionary<string, string>>()))
            .Returns("Test Subject");

        _templateServiceMock
            .Setup(x => x.GetBody(templateId, It.IsAny<Dictionary<string, string>>()))
            .Returns("<html>Test Body</html>");

        _emailServiceMock
            .Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _function.Run(message, _messageActionsMock.Object);

        _messageActionsMock.Verify(
            x => x.CompleteMessageAsync(message, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_WithEmptyStringInSubject_ShouldDeadLetter()
    {
        var correlationId = Guid.NewGuid().ToString();

        var notificationRequest = new NotificationRequest
        {
            EmailTo = new List<string> { "test@test.com" },
            Subject = "   ",
            Body = "Test Body",        };

        var message = CreateServiceBusMessage(notificationRequest, correlationId);

        await _function.Run(message, _messageActionsMock.Object);

        _messageActionsMock.Verify(
            x => x.DeadLetterMessageAsync(
                message,
                It.IsAny<Dictionary<string, object>>(),
                It.Is<string>(r => r == "ValidationError"),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_WithEmptyStringInBody_ShouldDeadLetter()
    {
        var correlationId = Guid.NewGuid().ToString();

        var notificationRequest = new NotificationRequest
        {
            EmailTo = new List<string> { "test@test.com" },
            Subject = "Test",
            Body = "   ",        };

        var message = CreateServiceBusMessage(notificationRequest, correlationId);

        await _function.Run(message, _messageActionsMock.Object);

        _messageActionsMock.Verify(
            x => x.DeadLetterMessageAsync(
                message,
                It.IsAny<Dictionary<string, object>>(),
                It.Is<string>(r => r == "ValidationError"),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@test.com")]
    [InlineData("user@")]
    [InlineData("user")]
    public async Task Run_WithMalformedEmailAddresses_ShouldStillProcess(string malformedEmail)
    {
        // Note: A validação de formato de email não é feita pela função,
        // seria responsabilidade do Azure Communication Services
        var correlationId = Guid.NewGuid().ToString();

        var notificationRequest = new NotificationRequest
        {
            EmailTo = new List<string> { malformedEmail },
            Subject = "Test",
            Body = "Test Body",        };

        var message = CreateServiceBusMessage(notificationRequest, correlationId);

        _emailServiceMock
            .Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _function.Run(message, _messageActionsMock.Object);

        _emailServiceMock.Verify(
            x => x.SendEmailAsync(
                It.Is<EmailMessage>(e => e.To.Contains(malformedEmail)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_WithDuplicateRecipients_ShouldProcessAll()
    {
        var correlationId = Guid.NewGuid().ToString();

        var notificationRequest = new NotificationRequest
        {
            EmailTo = new List<string> 
            { 
                "duplicate@test.com", 
                "duplicate@test.com",
                "unique@test.com" 
            },
            Subject = "Test",
            Body = "Test Body",        };

        var message = CreateServiceBusMessage(notificationRequest, correlationId);

        _emailServiceMock
            .Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _function.Run(message, _messageActionsMock.Object);

        _emailServiceMock.Verify(
            x => x.SendEmailAsync(
                It.Is<EmailMessage>(e => e.To.Count == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_WithVeryLongSubject_ShouldProcess()
    {
        var correlationId = Guid.NewGuid().ToString();
        var longSubject = new string('A', 1000);

        var notificationRequest = new NotificationRequest
        {
            EmailTo = new List<string> { "test@test.com" },
            Subject = longSubject,
            Body = "Test Body",        };

        var message = CreateServiceBusMessage(notificationRequest, correlationId);

        _emailServiceMock
            .Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _function.Run(message, _messageActionsMock.Object);

        _emailServiceMock.Verify(
            x => x.SendEmailAsync(
                It.Is<EmailMessage>(e => e.Subject.Length == 1000),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_WithVeryLargeBody_ShouldProcess()
    {
        var correlationId = Guid.NewGuid().ToString();
        var largeBody = new string('B', 10000);

        var notificationRequest = new NotificationRequest
        {
            EmailTo = new List<string> { "test@test.com" },
            Subject = "Test",
            Body = largeBody,        };

        var message = CreateServiceBusMessage(notificationRequest, correlationId);

        _emailServiceMock
            .Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _function.Run(message, _messageActionsMock.Object);

        _messageActionsMock.Verify(
            x => x.CompleteMessageAsync(message, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_WithEmptyParametersDictionary_ShouldUseTemplate()
    {
        var correlationId = Guid.NewGuid().ToString();
        var templateId = "HeatStress";

        var notificationRequest = new NotificationRequest
        {
            EmailTo = new List<string> { "test@test.com" },
            TemplateId = templateId,
            Parameters = new Dictionary<string, string>(),        };

        var message = CreateServiceBusMessage(notificationRequest, correlationId);

        _templateServiceMock
            .Setup(x => x.GetSubject(templateId, It.IsAny<Dictionary<string, string>>()))
            .Returns("Test Subject");

        _templateServiceMock
            .Setup(x => x.GetBody(templateId, It.IsAny<Dictionary<string, string>>()))
            .Returns("<html>Test Body</html>");

        _emailServiceMock
            .Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _function.Run(message, _messageActionsMock.Object);

        _templateServiceMock.Verify(
            x => x.GetSubject(templateId, It.IsAny<Dictionary<string, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_WithCcAndBccSameAsTo_ShouldProcessAll()
    {
        var correlationId = Guid.NewGuid().ToString();
        var email = "same@test.com";

        var notificationRequest = new NotificationRequest
        {
            EmailTo = new List<string> { email },
            EmailCc = new List<string> { email },
            EmailBcc = new List<string> { email },
            Subject = "Test",
            Body = "Test Body",        };

        var message = CreateServiceBusMessage(notificationRequest, correlationId);

        _emailServiceMock
            .Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _function.Run(message, _messageActionsMock.Object);

        _emailServiceMock.Verify(
            x => x.SendEmailAsync(
                It.Is<EmailMessage>(e => 
                    e.To.Contains(email) && 
                    e.Cc.Contains(email) && 
                    e.Bcc.Contains(email)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Run_WithUnicodeCharactersInAllFields_ShouldProcess()
    {
        var correlationId = Guid.NewGuid().ToString();

        var notificationRequest = new NotificationRequest
        {
            EmailTo = new List<string> { "测试@test.com" },
            Subject = "テスト 测试 Тест",
            Body = "<html>🌡️ 🔥 ⚠️ Emoji test</html>",        };

        var message = CreateServiceBusMessage(notificationRequest, correlationId);

        _emailServiceMock
            .Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _function.Run(message, _messageActionsMock.Object);

        _messageActionsMock.Verify(
            x => x.CompleteMessageAsync(message, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static ServiceBusReceivedMessage CreateServiceBusMessage(NotificationRequest request, string? correlationId = null)
    {
        var json = JsonSerializer.Serialize(request);
        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: new BinaryData(Encoding.UTF8.GetBytes(json)),
            messageId: Guid.NewGuid().ToString(),
            correlationId: correlationId ?? Guid.NewGuid().ToString()
        );
    }
}

