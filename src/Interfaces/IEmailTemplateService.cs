namespace Common.Notifications.Function.Services;

/// <summary>
/// Service responsible for managing and processing email templates
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Gets subject for a template with parameters replaced
    /// </summary>
    string GetSubject(string templateId, Dictionary<string, string> parameters);

    /// <summary>
    /// Gets HTML body for a template with parameters replaced
    /// </summary>
    string GetBody(string templateId, Dictionary<string, string> parameters);

    /// <summary>
    /// Checks if a template exists
    /// </summary>
    bool TemplateExists(string templateId);
}
