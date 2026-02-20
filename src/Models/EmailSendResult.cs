namespace Common.Notifications.Function.Models;

/// <summary>
/// Resultado do envio de email
/// </summary>
public class EmailSendResult
{
    public bool Success { get; set; }
    public string? OperationId { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsRateLimitError { get; set; }
}
