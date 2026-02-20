namespace Common.Notifications.Function.Models;

/// <summary>
/// Types of email templates available in the system
/// </summary>
public enum EmailTemplateType
{
    ExcessiveRainfall,
    Drought,
    ExtremeHeat,
    FreezingTemperature,
    HeatStress,
    PestRisk,
    Irrigation
}

/// <summary>
/// Extension methods for EmailTemplateType
/// </summary>
public static class EmailTemplateTypeExtensions
{
    /// <summary>
    /// Converts string to EmailTemplateType
    /// </summary>
    public static EmailTemplateType Parse(string templateId)
    {
        if (Enum.TryParse<EmailTemplateType>(templateId, ignoreCase: true, out var result))
        {
            return result;
        }
        
        throw new ArgumentException($"Invalid template ID: {templateId}. Valid values are: {string.Join(", ", Enum.GetNames<EmailTemplateType>())}");
    }

    /// <summary>
    /// Tries to convert string to EmailTemplateType
    /// </summary>
    public static bool TryParse(string templateId, out EmailTemplateType result)
    {
        return Enum.TryParse(templateId, ignoreCase: true, out result);
    }

    /// <summary>
    /// Converts EmailTemplateType to string
    /// </summary>
    public static string ToTemplateId(this EmailTemplateType templateType)
    {
        return templateType.ToString();
    }

    /// <summary>
    /// Gets all available template IDs
    /// </summary>
    public static IEnumerable<string> GetAllTemplateIds()
    {
        return Enum.GetNames<EmailTemplateType>();
    }
}
