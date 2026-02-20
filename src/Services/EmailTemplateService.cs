using Common.Notifications.Function.Models;
using Common.Notifications.Function.Templates;
using Microsoft.Extensions.Logging;

namespace Common.Notifications.Function.Services;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly ILogger<EmailTemplateService> _logger;
    private readonly EmailTemplateFactory _templateFactory;

    public EmailTemplateService(ILogger<EmailTemplateService> logger)
    {
        _logger = logger;
        _templateFactory = new EmailTemplateFactory();
    }

    public string GetSubject(string templateId, Dictionary<string, string> parameters)
    {
        try
        {
            var template = _templateFactory.CreateTemplate(templateId);
            
            EnsureCorrelationId(parameters);
            
            return template.GetSubject(parameters);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Error getting subject for template: {TemplateId}", templateId);
            throw new KeyNotFoundException($"Template '{templateId}' not found", ex);
        }
    }

    public string GetBody(string templateId, Dictionary<string, string> parameters)
    {
        try
        {
            var template = _templateFactory.CreateTemplate(templateId);
            
            EnsureCorrelationId(parameters);
            
            return template.GetBody(parameters);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Error getting body for template: {TemplateId}", templateId);
            throw new KeyNotFoundException($"Template '{templateId}' not found", ex);
        }
    }

    public bool TemplateExists(string templateId)
    {
        return _templateFactory.TemplateExists(templateId);
    }

    /// <summary>
    /// Gets all available template IDs
    /// </summary>
    public IEnumerable<string> GetAllTemplateIds()
    {
        return _templateFactory.GetAllTemplateIds();
    }

    /// <summary>
    /// Gets all available template types
    /// </summary>
    public IEnumerable<EmailTemplateType> GetAllTemplateTypes()
    {
        return _templateFactory.GetAllTemplateTypes();
    }

    private void EnsureCorrelationId(Dictionary<string, string> parameters)
    {
        if (!parameters.ContainsKey("{correlationId}") && !parameters.ContainsKey("correlationId"))
        {
            parameters["{correlationId}"] = "N/A";
        }
    }
}
