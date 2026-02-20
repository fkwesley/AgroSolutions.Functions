using Common.Notifications.Function.Models;

namespace Common.Notifications.Function.Templates;

/// <summary>
/// Factory for creating email template instances
/// </summary>
public class EmailTemplateFactory
{
    private readonly Dictionary<EmailTemplateType, Func<IEmailTemplate>> _templateFactories;

    public EmailTemplateFactory()
    {
        _templateFactories = new Dictionary<EmailTemplateType, Func<IEmailTemplate>>
        {
            [EmailTemplateType.ExcessiveRainfall] = () => new ExcessiveRainfallTemplate(),
            [EmailTemplateType.Drought] = () => new DroughtTemplate(),
            [EmailTemplateType.ExtremeHeat] = () => new ExtremeHeatTemplate(),
            [EmailTemplateType.FreezingTemperature] = () => new FreezingTemperatureTemplate(),
            [EmailTemplateType.HeatStress] = () => new HeatStressTemplate(),
            [EmailTemplateType.PestRisk] = () => new PestRiskTemplate(),
            [EmailTemplateType.Irrigation] = () => new IrrigationTemplate()
        };
    }

    /// <summary>
    /// Creates a template instance by type
    /// </summary>
    public IEmailTemplate CreateTemplate(EmailTemplateType templateType)
    {
        if (!_templateFactories.TryGetValue(templateType, out var factory))
        {
            throw new ArgumentException($"Template type '{templateType}' not registered in factory");
        }

        return factory();
    }

    /// <summary>
    /// Creates a template instance from string template ID
    /// </summary>
    public IEmailTemplate CreateTemplate(string templateId)
    {
        var templateType = EmailTemplateTypeExtensions.Parse(templateId);
        return CreateTemplate(templateType);
    }

    /// <summary>
    /// Checks if a template exists
    /// </summary>
    public bool TemplateExists(string templateId)
    {
        return EmailTemplateTypeExtensions.TryParse(templateId, out var templateType) 
            && _templateFactories.ContainsKey(templateType);
    }

    /// <summary>
    /// Checks if a template type exists
    /// </summary>
    public bool TemplateExists(EmailTemplateType templateType)
    {
        return _templateFactories.ContainsKey(templateType);
    }

    /// <summary>
    /// Gets all available template types
    /// </summary>
    public IEnumerable<EmailTemplateType> GetAllTemplateTypes()
    {
        return _templateFactories.Keys;
    }

    /// <summary>
    /// Gets all available template IDs
    /// </summary>
    public IEnumerable<string> GetAllTemplateIds()
    {
        return _templateFactories.Keys.Select(t => t.ToTemplateId());
    }
}
