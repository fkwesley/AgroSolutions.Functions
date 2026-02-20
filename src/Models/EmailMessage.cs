namespace Common.Notifications.Function.Models;

public class EmailMessage
{
    /// <summary>
    /// Primary recipients
    /// </summary>
    public List<string> To { get; set; } = new();

    /// <summary>
    /// Carbon copy recipients
    /// </summary>
    public List<string> Cc { get; set; } = new();

    /// <summary>
    /// Blind carbon copy recipients
    /// </summary>
    public List<string> Bcc { get; set; } = new();

    public string Subject { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string? PlainTextContent { get; set; }

    /// <summary>
    /// Correlation ID for tracking
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Notification priority level (Low, Normal, High, Urgent)
    /// Maps to X-Priority and Importance email headers
    /// </summary>
    public PriorityEnum Priority { get; set; } = PriorityEnum.Normal;
}