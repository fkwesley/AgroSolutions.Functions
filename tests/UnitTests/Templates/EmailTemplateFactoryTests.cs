using Common.Notifications.Function.Models;
using Common.Notifications.Function.Templates;
using FluentAssertions;

namespace UnitTests.Templates;

public class EmailTemplateFactoryTests
{
    private readonly EmailTemplateFactory _factory;

    public EmailTemplateFactoryTests()
    {
        _factory = new EmailTemplateFactory();
    }

    [Theory]
    [InlineData("HeatStress", typeof(HeatStressTemplate))]
    [InlineData("Drought", typeof(DroughtTemplate))]
    [InlineData("ExcessiveRainfall", typeof(ExcessiveRainfallTemplate))]
    [InlineData("FreezingTemperature", typeof(FreezingTemperatureTemplate))]
    [InlineData("ExtremeHeat", typeof(ExtremeHeatTemplate))]
    [InlineData("PestRisk", typeof(PestRiskTemplate))]
    [InlineData("Irrigation", typeof(IrrigationTemplate))]
    public void CreateTemplate_WithValidTemplateId_ShouldReturnCorrectTemplateType(string templateId, Type expectedType)
    {
        var template = _factory.CreateTemplate(templateId);

        template.Should().NotBeNull();
        template.Should().BeOfType(expectedType);
    }

    [Theory]
    [InlineData("heatstress")]
    [InlineData("HEATSTRESS")]
    [InlineData("HeatStress")]
    [InlineData("drought")]
    [InlineData("DROUGHT")]
    public void CreateTemplate_WithCaseInsensitiveId_ShouldWork(string templateId)
    {
        var act = () => _factory.CreateTemplate(templateId);

        act.Should().NotThrow();
    }

    [Fact]
    public void CreateTemplate_WithInvalidTemplateId_ShouldThrowArgumentException()
    {
        var act = () => _factory.CreateTemplate("invalid-template");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid template ID*");
    }

    [Fact]
    public void CreateTemplate_WithEmptyTemplateId_ShouldThrowArgumentException()
    {
        var act = () => _factory.CreateTemplate("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateTemplate_WithNullTemplateId_ShouldThrowArgumentException()
    {
        var act = () => _factory.CreateTemplate(null!);

        act.Should().Throw<ArgumentException>();
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
        var result = _factory.TemplateExists(templateId);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("unknown-template")]
    [InlineData("test123")]
    public void TemplateExists_WithInvalidTemplateId_ShouldReturnFalse(string templateId)
    {
        var result = _factory.TemplateExists(templateId);

        result.Should().BeFalse();
    }

    [Fact]
    public void GetAllTemplateIds_ShouldReturnAllTemplateTypes()
    {
        var result = _factory.GetAllTemplateIds();

        result.Should().NotBeEmpty();
        result.Should().HaveCount(7);
        result.Should().Contain("HeatStress");
        result.Should().Contain("Drought");
        result.Should().Contain("ExcessiveRainfall");
        result.Should().Contain("FreezingTemperature");
        result.Should().Contain("ExtremeHeat");
        result.Should().Contain("PestRisk");
        result.Should().Contain("Irrigation");
    }

    [Fact]
    public void GetAllTemplateTypes_ShouldReturnAllEnumValues()
    {
        var result = _factory.GetAllTemplateTypes();

        result.Should().NotBeEmpty();
        result.Should().HaveCount(7);
        result.Should().Contain(EmailTemplateType.HeatStress);
        result.Should().Contain(EmailTemplateType.Drought);
        result.Should().Contain(EmailTemplateType.ExcessiveRainfall);
        result.Should().Contain(EmailTemplateType.FreezingTemperature);
        result.Should().Contain(EmailTemplateType.ExtremeHeat);
        result.Should().Contain(EmailTemplateType.PestRisk);
        result.Should().Contain(EmailTemplateType.Irrigation);
    }

    [Fact]
    public void CreateTemplate_MultipleCalls_ShouldReturnNewInstances()
    {
        var template1 = _factory.CreateTemplate("HeatStress");
        var template2 = _factory.CreateTemplate("HeatStress");

        template1.Should().NotBeSameAs(template2);
    }

    [Theory]
    [InlineData("HeatStress")]
    [InlineData("Drought")]
    [InlineData("Irrigation")]
    public void CreatedTemplate_ShouldImplementIEmailTemplate(string templateId)
    {
        var template = _factory.CreateTemplate(templateId);

        template.Should().BeAssignableTo<IEmailTemplate>();
    }

    [Theory]
    [InlineData("HeatStress")]
    [InlineData("Drought")]
    [InlineData("ExcessiveRainfall")]
    public void CreatedTemplate_ShouldHaveSubjectAndBody(string templateId)
    {
        var template = _factory.CreateTemplate(templateId);
        var parameters = new Dictionary<string, string>
        {
            { "{fieldId}", "123" },
            { "{correlationId}", Guid.NewGuid().ToString() }
        };

        var subject = template.GetSubject(parameters);
        var body = template.GetBody(parameters);

        subject.Should().NotBeNullOrEmpty();
        body.Should().NotBeNullOrEmpty();
        body.Should().Contain("html");
    }
}