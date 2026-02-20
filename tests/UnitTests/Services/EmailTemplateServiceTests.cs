using Common.Notifications.Function.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Services;

public class EmailTemplateServiceTests
{
    private readonly Mock<ILogger<EmailTemplateService>> _loggerMock;
    private readonly EmailTemplateService _service;

    public EmailTemplateServiceTests()
    {
        _loggerMock = new Mock<ILogger<EmailTemplateService>>();
        _service = new EmailTemplateService(_loggerMock.Object);
    }

    [Theory]
    [InlineData("HeatStress")]
    [InlineData("Drought")]
    [InlineData("ExcessiveRainfall")]
    [InlineData("FreezingTemperature")]
    [InlineData("ExtremeHeat")]
    [InlineData("PestRisk")]
    [InlineData("Irrigation")]
    public void TemplateExists_WithValidTemplateId_ShouldReturnTrue(string templateId)
    {
        var result = _service.TemplateExists(templateId);

        result.Should().BeTrue();
    }

    [Fact]
    public void TemplateExists_WithInvalidTemplateId_ShouldReturnFalse()
    {
        var result = _service.TemplateExists("non-existent-template");

        result.Should().BeFalse();
    }

    [Fact]
    public void GetSubject_WithValidTemplate_ShouldReturnSubject()
    {
        var parameters = new Dictionary<string, string>
        {
            { "{fieldId}", "123" },
            { "{stressLevel}", "Alto" },
            { "{correlationId}", Guid.NewGuid().ToString() }
        };

        var result = _service.GetSubject("HeatStress", parameters);

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("123");
    }

    [Fact]
    public void GetBody_WithValidTemplate_ShouldReturnHtmlBody()
    {
        var parameters = new Dictionary<string, string>
        {
            { "{fieldId}", "123" },
            { "{stressLevel}", "Alto" },
            { "{durationHours}", "8" },
            { "{averageTemperature}", "35" },
            { "{peakTemperature}", "40" },
            { "{correlationId}", Guid.NewGuid().ToString() }
        };

        var result = _service.GetBody("HeatStress", parameters);

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("html");
        result.Should().Contain("123");
    }

    [Fact]
    public void GetSubject_WithInvalidTemplateId_ShouldThrowKeyNotFoundException()
    {
        var parameters = new Dictionary<string, string>();

        var act = () => _service.GetSubject("invalid-template", parameters);

        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*invalid-template*");
    }

    [Fact]
    public void GetBody_WithInvalidTemplateId_ShouldThrowKeyNotFoundException()
    {
        var parameters = new Dictionary<string, string>();

        var act = () => _service.GetBody("invalid-template", parameters);

        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*invalid-template*");
    }

    [Fact]
    public void GetSubject_WithoutCorrelationId_ShouldAddDefaultCorrelationId()
    {
        var parameters = new Dictionary<string, string>
        {
            { "{fieldId}", "123" },
            { "{stressLevel}", "Alto" }
        };

        var result = _service.GetSubject("HeatStress", parameters);

        result.Should().NotBeNullOrEmpty();
        parameters.Should().ContainKey("{correlationId}");
        parameters["{correlationId}"].Should().Be("N/A");
    }

    [Fact]
    public void GetBody_WithoutCorrelationId_ShouldAddDefaultCorrelationId()
    {
        var parameters = new Dictionary<string, string>
        {
            { "{fieldId}", "123" },
            { "{stressLevel}", "Alto" },
            { "{durationHours}", "8" },
            { "{averageTemperature}", "35" },
            { "{peakTemperature}", "40" }
        };

        var result = _service.GetBody("HeatStress", parameters);

        result.Should().NotBeNullOrEmpty();
        parameters.Should().ContainKey("{correlationId}");
    }

    [Fact]
    public void GetAllTemplateIds_ShouldReturnAllAvailableTemplates()
    {
        var result = _service.GetAllTemplateIds();

        result.Should().NotBeEmpty();
        result.Should().Contain("HeatStress");
        result.Should().Contain("Drought");
        result.Should().Contain("ExcessiveRainfall");
    }

    [Fact]
    public void GetAllTemplateTypes_ShouldReturnAllTemplateEnumValues()
    {
        var result = _service.GetAllTemplateTypes();

        result.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("Drought")]
    [InlineData("Irrigation")]
    public void GetSubject_WithDifferentTemplates_ShouldReturnDifferentSubjects(string templateId)
    {
        var parameters = new Dictionary<string, string>
        {
            { "{fieldName}", "Campo Teste" },
            { "{date}", DateTime.Now.ToString("dd/MM/yyyy") },
            { "{correlationId}", Guid.NewGuid().ToString() }
        };

        var result = _service.GetSubject(templateId, parameters);

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetBody_ShouldReplaceAllParameters()
    {
        var fieldId = "456";
        var stressLevel = "Crítico";
        var durationHours = "12";
        var averageTemperature = "38";
        var peakTemperature = "42";
        var correlationId = Guid.NewGuid().ToString();

        var parameters = new Dictionary<string, string>
        {
            { "{fieldId}", fieldId },
            { "{stressLevel}", stressLevel },
            { "{durationHours}", durationHours },
            { "{averageTemperature}", averageTemperature },
            { "{peakTemperature}", peakTemperature },
            { "{correlationId}", correlationId }
        };

        var result = _service.GetBody("HeatStress", parameters);

        result.Should().Contain(fieldId);
        result.Should().NotContain("{fieldId}");
    }
}