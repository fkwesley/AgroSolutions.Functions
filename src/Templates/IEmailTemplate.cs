namespace Common.Notifications.Function.Templates;

/// <summary>
/// Base interface for email templates
/// </summary>
public interface IEmailTemplate
{
    /// <summary>
    /// Gets the subject template with placeholders
    /// </summary>
    string GetSubjectTemplate();

    /// <summary>
    /// Gets the HTML body template with placeholders
    /// </summary>
    string GetBodyTemplate();

    /// <summary>
    /// Replaces parameters in the subject
    /// </summary>
    string GetSubject(Dictionary<string, string> parameters);

    /// <summary>
    /// Replaces parameters in the body
    /// </summary>
    string GetBody(Dictionary<string, string> parameters);
}
