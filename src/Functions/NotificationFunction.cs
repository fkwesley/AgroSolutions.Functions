using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Common.Notifications.Function.Interfaces;
using Common.Notifications.Function.Logging;
using Common.Notifications.Function.Models;
using Common.Notifications.Function.Services;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Common.Notifications.Function.Functions;

public class NotificationFunction
{
    private readonly ILogger<NotificationFunction> _logger;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _templateService;
    private readonly CorrelationIdEnricher _correlationIdEnricher;

    public NotificationFunction(
        ILogger<NotificationFunction> logger,
        IEmailService emailService,
        IEmailTemplateService templateService,
        CorrelationIdEnricher correlationIdEnricher)
    {
        _logger = logger;
        _emailService = emailService;
        _templateService = templateService;
        _correlationIdEnricher = correlationIdEnricher;
    }

    [Function("ProcessNotificationQueue")]
    public async Task Run(
        [ServiceBusTrigger("notifications-queue", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        // ===============================
        // CORRELATION ID
        // ===============================
        string? correlationId = message.CorrelationId;

        if (string.IsNullOrEmpty(correlationId) && message.ApplicationProperties.TryGetValue("CorrelationId", out var customCorrelationId))
            correlationId = customCorrelationId?.ToString();

        _correlationIdEnricher.SetCorrelationId(correlationId);

        // ===============================
        // DISTRIBUTED TRACING
        // ===============================
        var traceparent = message.ApplicationProperties.TryGetValue("traceparent", out var traceparentValue)
            ? traceparentValue?.ToString() : null;

        DistributedTracingData? distributedTracingData = null;

        if (!string.IsNullOrWhiteSpace(traceparent))
            distributedTracingData = DistributedTracingData.TryDeserializeFromString(traceparent);

        var transaction = Agent.Tracer.StartTransaction(
            "ProcessNotificationQueue",
            ApiConstants.TypeMessaging,
            distributedTracingData);

        try
        {
            transaction.SetLabel("CorrelationId", correlationId ?? "N/A");
            transaction.SetLabel("MessageId", message.MessageId);

            _logger.LogInformation(
                "Processing message ID: {MessageId}, CorrelationId: {CorrelationId}",
                message.MessageId,
                correlationId ?? "N/A");

            // ===============================
            // PARSE MESSAGE SPAN
            // ===============================
            var notificationRequest =
                await transaction.CaptureSpan(
                    "Parse Service Bus Message",
                    ApiConstants.TypeMessaging,
                    async () =>
                    {
                        var request = JsonSerializer.Deserialize<NotificationRequest>(message.Body.ToString());

                        Agent.Tracer.CurrentSpan?.SetLabel("MessageSize", message.Body.Length);
                        Agent.Tracer.CurrentSpan?.SetLabel("EnqueuedTime", message.EnqueuedTime.ToString("O"));

                        return request;
                    },
                    "azureservicebus");

            if (notificationRequest == null)
            {
                _logger.LogError(
                    "Failed to deserialize message body for message ID: {MessageId}, CorrelationId: {CorrelationId}", 
                    message.MessageId,
                    correlationId ?? "N/A");

                await messageActions.DeadLetterMessageAsync(
                    message,
                    deadLetterReason: "DeserializationError",
                    deadLetterErrorDescription: "Could not deserialize message body",
                    propertiesToModify: null);

                return;
            }

            // ===============================
            // VALIDATION
            // ===============================
            var (isValid, errorReason, errorDescription) = ValidateNotificationRequest(notificationRequest);

            if (!isValid)
            {
                _logger.LogError(
                    "Validation failed for message ID: {MessageId}, CorrelationId: {CorrelationId}, Reason: {Reason}", 
                    message.MessageId,
                    correlationId ?? "N/A",
                    errorReason);

                await messageActions.DeadLetterMessageAsync(
                    message,
                    deadLetterReason: errorReason!,
                    deadLetterErrorDescription: errorDescription!,
                    propertiesToModify: null);

                return;
            }

            // ===============================
            // TEMPLATE PROCESSING
            // ===============================
            string subject;
            string htmlBody;

            if (!string.IsNullOrWhiteSpace(notificationRequest.TemplateId))
            {
                (subject, htmlBody) =
                    await transaction.CaptureSpan(
                        "Process Email Template",
                        "template",
                        async () =>
                        {
                            Agent.Tracer.CurrentSpan?.SetLabel(
                                "TemplateId", notificationRequest.TemplateId);

                            var subj = _templateService.GetSubject(notificationRequest.TemplateId, notificationRequest.Parameters);
                            var body = _templateService.GetBody(notificationRequest.TemplateId, notificationRequest.Parameters);

                            return (subj, body);
                        },
                        "email-template");
            }
            else
            {
                subject = notificationRequest.Subject!;
                htmlBody = notificationRequest.Body!;
            }

            // ===============================
            // EMAIL SENDING
            // ===============================
            var emailMessage = new EmailMessage
            {
                To = notificationRequest.EmailTo,
                Cc = notificationRequest.EmailCc ?? new(),
                Bcc = notificationRequest.EmailBcc ?? new(),
                Subject = subject,
                HtmlContent = htmlBody,
                CorrelationId = correlationId,
                Priority = notificationRequest.Priority
            };

            transaction.SetLabel("RecipientCount", emailMessage.To.Count);
            transaction.SetLabel("TemplateId", notificationRequest.TemplateId ?? "Direct");

            await transaction.CaptureSpan(
                "Send Email via ACS",
                ApiConstants.TypeExternal,
                async () =>
                {
                    Agent.Tracer.CurrentSpan?.SetLabel("RecipientCount", emailMessage.To.Count);
                    await _emailService.SendEmailAsync(emailMessage);
                },
                ApiConstants.SubtypeHttp);

            await messageActions.CompleteMessageAsync(message);

            _logger.LogInformation("Successfully processed message ID: {MessageId}", message.MessageId);
        }
        catch (TooManyRequestsException ex)
        {
            transaction.CaptureException(ex);

            _logger.LogWarning(
                ex,
                "Rate limit exceeded for message ID: {MessageId}, CorrelationId: {CorrelationId}. Message will be retried.",
                message.MessageId,
                correlationId ?? "N/A");

            // Abandona a mensagem para que ela volte para a fila e seja reprocessada
            await messageActions.AbandonMessageAsync(message);
        }
        catch (JsonException ex)
        {
            transaction.CaptureException(ex);

            await messageActions.DeadLetterMessageAsync(
                message,
                propertiesToModify: null,
                deadLetterReason: "InvalidJsonFormat",
                deadLetterErrorDescription: ex.Message,
                cancellationToken: CancellationToken.None);

            return;
        }
        catch (KeyNotFoundException ex)
        {
            transaction.CaptureException(ex);

            await messageActions.DeadLetterMessageAsync(
                message,
                propertiesToModify: null,
                deadLetterReason: "TemplateNotFound",
                deadLetterErrorDescription: ex.Message,
                cancellationToken: CancellationToken.None);

            return;
        }
        catch (Exception ex)
        {
            transaction.CaptureException(ex);

            _logger.LogError(
                ex,
                "Error processing message ID: {MessageId}",
                message.MessageId);

            await messageActions.DeadLetterMessageAsync(message);
        }
        finally
        {
            transaction.End();
        }
    }

    private static (bool IsValid, string? ErrorReason, string? ErrorDescription) ValidateNotificationRequest(NotificationRequest request)
    {
        if (request.EmailTo == null || request.EmailTo.Count == 0)
            return (false, "ValidationError", "No recipients specified");

        if (string.IsNullOrWhiteSpace(request.TemplateId))
        {
            if (string.IsNullOrWhiteSpace(request.Subject))
                return (false, "ValidationError", "Subject required");

            if (string.IsNullOrWhiteSpace(request.Body))
                return (false, "ValidationError", "Body required");
        }

        return (true, null, null);
    }
}
