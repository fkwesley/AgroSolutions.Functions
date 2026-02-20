using Common.Notifications.Function.Models;

namespace Common.Notifications.Function.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(EmailMessage emailMessage, CancellationToken cancellationToken = default);
}
