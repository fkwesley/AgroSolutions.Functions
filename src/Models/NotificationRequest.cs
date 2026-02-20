using System.ComponentModel.DataAnnotations;

namespace Common.Notifications.Function.Models;

public class NotificationRequest
{
    /// <summary>
    /// Email recipients (primary)
    /// </summary>
    [Required]
    public List<string> EmailTo { get; set; } = new();

    /// <summary>
    /// Email carbon copy recipients
    /// </summary>
    public List<string> EmailCc { get; set; } = new();

    /// <summary>
    /// Email blind carbon copy recipients
    /// </summary>
    public List<string> EmailBcc { get; set; } = new();

    /// <summary>
    /// Template ID for predefined email templates.
    /// If provided, uses template with parameters. Otherwise, uses Subject and Body.
    /// </summary>
    public string? TemplateId { get; set; }

    /// <summary>
    /// Parameters for template replacement (key-value pairs)
    /// Example: { "{orderId}": "12345", "{customerName}": "John Doe" }
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();

    /// <summary>
    /// Email subject line (required if TemplateId is not provided)
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Email body with detailed explanation (required if TemplateId is not provided)
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Notification priority level (Low, Normal, High, Urgent).
    /// Maps to SMTP headers (X-Priority, Importance) for email client display.
    /// Default: Normal
    /// </summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public PriorityEnum Priority { get; set; } = PriorityEnum.Normal;
}

/// <summary>
/// Email priority levels for SMTP headers (X-Priority, Importance)
/// </summary>
public enum PriorityEnum
{
    Low,
    Normal,
    High,
    Urgent
}
