using Azure;
using Azure.Communication.Email;
using Common.Notifications.Function.Interfaces;
using Common.Notifications.Function.Models;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Common.Notifications.Function.Services;

public class AzureCommunicationEmailService : IEmailService
{
    private readonly EmailClient _emailClient;
    private readonly string _senderAddress;
    private readonly ILogger<AzureCommunicationEmailService> _logger;

    public AzureCommunicationEmailService(
        IConfiguration configuration,
        ILogger<AzureCommunicationEmailService> logger)
    {
        _logger = logger;

        var connectionString = configuration["AzureCommunicationServices:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Communication Services connection string not configured");

        _senderAddress = configuration["AzureCommunicationServices:SenderAddress"]
            ?? throw new InvalidOperationException("Azure Communication Services sender address not configured");

        _emailClient = new EmailClient(connectionString);
    }

    public async Task SendEmailAsync(Models.EmailMessage emailMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            if (emailMessage.To == null || emailMessage.To.Count == 0)
                throw new ArgumentException("At least one recipient is required", nameof(emailMessage));

            var emailContent = new EmailContent(emailMessage.Subject)
            {
                Html = emailMessage.HtmlContent
            };

            if (!string.IsNullOrEmpty(emailMessage.PlainTextContent))
                emailContent.PlainText = emailMessage.PlainTextContent;

            // Primary recipients
            var toRecipients = emailMessage.To.Select(email => new EmailAddress(email)).ToList();
            var emailRecipients = new EmailRecipients(toRecipients);

            // CC recipients
            if (emailMessage.Cc != null && emailMessage.Cc.Count > 0)
            {
                foreach (var email in emailMessage.Cc)
                    emailRecipients.CC.Add(new EmailAddress(email));
            }

            // BCC recipients
            if (emailMessage.Bcc != null && emailMessage.Bcc.Count > 0)
            {
                foreach (var email in emailMessage.Bcc)
                    emailRecipients.BCC.Add(new EmailAddress(email));
            }

            var azureEmailMessage = new Azure.Communication.Email.EmailMessage(_senderAddress, emailRecipients, emailContent);

            // ✅ Adicionar headers de prioridade baseados no EmailPriority
            ApplyEmailPriorityHeaders(azureEmailMessage, emailMessage.Priority);

            var recipientsInfo = $"To: {string.Join(", ", emailMessage.To)}";

            if (emailMessage.Cc?.Count > 0)
                recipientsInfo += $", Cc: {string.Join(", ", emailMessage.Cc)}";
            if (emailMessage.Bcc?.Count > 0)
                recipientsInfo += $", Bcc: {emailMessage.Bcc.Count} recipient(s)";

            _logger.LogInformation(
                "Sending email - Subject: {Subject}, Priority: {Priority}, Recipients: {Recipients}, CorrelationId: {CorrelationId}", 
                emailMessage.Subject,
                emailMessage.Priority,
                recipientsInfo,
                emailMessage.CorrelationId ?? "N/A");

            EmailSendOperation emailSendOperation;

            // Captura detalhada da chamada ao Azure Communication Services
            if (Agent.Tracer.CurrentSpan != null)
            {
                emailSendOperation = await Agent.Tracer.CurrentSpan.CaptureSpan(
                    "ACS SendAsync",
                    ApiConstants.TypeExternal,
                    async () =>
                    {
                        Agent.Tracer.CurrentSpan?.SetLabel("Sender", _senderAddress);
                        Agent.Tracer.CurrentSpan?.SetLabel("ToCount", emailMessage.To.Count);
                        Agent.Tracer.CurrentSpan?.SetLabel("CcCount", emailMessage.Cc?.Count ?? 0);
                        Agent.Tracer.CurrentSpan?.SetLabel("BccCount", emailMessage.Bcc?.Count ?? 0);
                        Agent.Tracer.CurrentSpan?.SetLabel("Priority", emailMessage.Priority.ToString());

                        return await _emailClient.SendAsync(
                            WaitUntil.Started,
                            azureEmailMessage,
                            cancellationToken);
                    },
                    "azure-communication-services");
            }
            else
            {
                emailSendOperation = await _emailClient.SendAsync(
                    WaitUntil.Started,
                    azureEmailMessage,
                    cancellationToken);
            }

            _logger.LogInformation(
                "Email sent successfully. Operation ID: {OperationId}, CorrelationId: {CorrelationId}", 
                emailSendOperation.Id,
                emailMessage.CorrelationId ?? "N/A");
        }
        catch (RequestFailedException ex) when (ex.Status == 429)
        {
            _logger.LogWarning("Too many requests (429) when sending email. Message will be reprocessed.");
            throw new TooManyRequestsException("Rate limit exceeded", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email. CorrelationId: {CorrelationId}", emailMessage.CorrelationId ?? "N/A");
            throw;
        }
    }

    /// <summary>
    /// Applies SMTP priority headers to email message based on priority level.
    /// These headers are recognized by most email clients (Outlook, Gmail, etc.)
    /// </summary>
    private static void ApplyEmailPriorityHeaders(Azure.Communication.Email.EmailMessage azureEmailMessage, PriorityEnum priority)
    {
        // X-Priority: 1 (Highest) to 5 (Lowest)
        // Importance: high, normal, low
        // Priority: urgent, normal, non-urgent

        var (importance, priorityText) = priority switch
        {
            PriorityEnum.Urgent or PriorityEnum.High => ("high", "urgent"),
            PriorityEnum.Low => ("low", "non-urgent"),
            _ => ("normal", "normal")
        };

        azureEmailMessage.Headers.Add("X-Priority", ((int)priority).ToString());
        azureEmailMessage.Headers.Add("Importance", importance);
        azureEmailMessage.Headers.Add("Priority", priorityText);

        // X-MSMail-Priority para compatibilidade com clientes Microsoft
        azureEmailMessage.Headers.Add("X-MSMail-Priority", importance);
    }
}

public class TooManyRequestsException : Exception
{
    public TooManyRequestsException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
