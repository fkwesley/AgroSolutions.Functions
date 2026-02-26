using System.Text.Json;
using AgroSolutions.Functions.Functions;
using AgroSolutions.Functions.Interfaces;
using AgroSolutions.Functions.Logging;
using AgroSolutions.Functions.Models;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Functions;

public class CollectSensorDataFunctionTests
{
    private readonly Mock<ILogger<CollectSensorDataFunction>> _mockLogger;
    private readonly Mock<IApiClientService> _mockApiClient;
    private readonly ServiceInfoEnricher _serviceInfoEnricher;
    private readonly CollectSensorDataFunction _sut;

    private static readonly List<FieldResponse> ActiveFields =
    [
        new() { Id = 1, Name = "Talhão Leste", Latitude = -4.6494, Longitude = -40.2002, IsActive = true },
        new() { Id = 2, Name = "Talhão Sul", Latitude = -3.7304, Longitude = -38.5217, IsActive = true }
    ];

    private static readonly OpenMeteoResponse WeatherResponse = new()
    {
        Current = new OpenMeteoCurrent { Temperature2m = 28.5, Precipitation = 1.2 }
    };

    public CollectSensorDataFunctionTests()
    {
        _mockLogger = new Mock<ILogger<CollectSensorDataFunction>>();
        _mockApiClient = new Mock<IApiClientService>();
        _serviceInfoEnricher = new ServiceInfoEnricher("test-service", "test");

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["FieldsApi:Url"]).Returns("https://test/fields");
        mockConfig.Setup(c => c["FieldsApi:Token"]).Returns("test-token");
        mockConfig.Setup(c => c["CollectSensorData:AlertEmailTo"]).Returns("test@test.com");
        mockConfig.Setup(c => c["CollectSensorData:ServiceNames"]).Returns("func-test-collect");

        _sut = new CollectSensorDataFunction(
            _mockLogger.Object,
            _mockApiClient.Object,
            _serviceInfoEnricher,
            mockConfig.Object);
    }

    [Fact]
    public async Task CollectSensorData_WithActiveFields_ReturnsMessagesForEachField()
    {
        // Arrange
        SetupFieldsApi(ActiveFields);
        SetupWeatherApi(WeatherResponse);

        // Act
        var result = await _sut.CollectSensorData(new TimerInfo());

        // Assert
        result.Should().HaveCount(2);

        var first = JsonSerializer.Deserialize<SensorDataRequest>(result[0])!;
        first.FieldId.Should().Be(1);
        first.AirTemperature.Should().Be(28.5m);
        first.Precipitation.Should().Be(1.2m);
        first.AlertEmailTo.Should().Be("test@test.com");
        first.SoilMoisture.Should().BeInRange(20m, 80m);
        first.CollectedAt.Should().NotBe(default);

        var second = JsonSerializer.Deserialize<SensorDataRequest>(result[1])!;
        second.FieldId.Should().Be(2);
    }

    [Fact]
    public async Task CollectSensorData_WhenNoFieldsReturned_ReturnsEmptyArray()
    {
        // Arrange
        SetupFieldsApi(null);

        // Act
        var result = await _sut.CollectSensorData(new TimerInfo());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CollectSensorData_WhenEmptyFieldsList_ReturnsEmptyArray()
    {
        // Arrange
        SetupFieldsApi([]);

        // Act
        var result = await _sut.CollectSensorData(new TimerInfo());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CollectSensorData_WhenNoActiveFields_ReturnsEmptyArray()
    {
        // Arrange
        var inactiveFields = new List<FieldResponse>
        {
            new() { Id = 1, Name = "Inativo", IsActive = false },
            new() { Id = 2, Name = "Inativo 2", IsActive = false }
        };
        SetupFieldsApi(inactiveFields);

        // Act
        var result = await _sut.CollectSensorData(new TimerInfo());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CollectSensorData_FiltersOnlyActiveFields()
    {
        // Arrange
        var mixedFields = new List<FieldResponse>
        {
            new() { Id = 1, Name = "Ativo", Latitude = -23.55, Longitude = -46.63, IsActive = true },
            new() { Id = 2, Name = "Inativo", Latitude = -22.90, Longitude = -47.05, IsActive = false },
            new() { Id = 3, Name = "Ativo 2", Latitude = -3.73, Longitude = -38.52, IsActive = true }
        };
        SetupFieldsApi(mixedFields);
        SetupWeatherApi(WeatherResponse);

        // Act
        var result = await _sut.CollectSensorData(new TimerInfo());

        // Assert
        result.Should().HaveCount(2);

        var fieldIds = result
            .Select(r => JsonSerializer.Deserialize<SensorDataRequest>(r)!.FieldId)
            .ToList();
        fieldIds.Should().BeEquivalentTo([1, 3]);
    }

    [Fact]
    public async Task CollectSensorData_WhenWeatherApiFails_UsesMockData()
    {
        // Arrange
        SetupFieldsApi(ActiveFields);

        _mockApiClient
            .Setup(x => x.GetAsync<OpenMeteoResponse>(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IDictionary<string, string>?>()))
            .Returns(() => Task.FromException<OpenMeteoResponse?>(new HttpRequestException("API unavailable")));

        // Act
        var result = await _sut.CollectSensorData(new TimerInfo());

        // Assert
        result.Should().HaveCount(2);

        var message = JsonSerializer.Deserialize<SensorDataRequest>(result[0])!;
        message.AirTemperature.Should().Be(-5.5m);
        message.Precipitation.Should().Be(62.3m);
    }

    [Fact]
    public async Task CollectSensorData_WhenFieldsApiThrows_PropagatesException()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync<List<FieldResponse>>(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IDictionary<string, string>?>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var act = () => _sut.CollectSensorData(new TimerInfo());

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Connection refused");
    }

    [Fact]
    public async Task CollectSensorData_CallsFieldsApiWithToken()
    {
        // Arrange
        SetupFieldsApi(ActiveFields);
        SetupWeatherApi(WeatherResponse);

        // Act
        await _sut.CollectSensorData(new TimerInfo());

        // Assert
        _mockApiClient.Verify(x => x.GetAsync<List<FieldResponse>>(
            "https://test/fields",
            "test-token",
            It.IsAny<IDictionary<string, string>?>()), Times.Once);
    }

    [Fact]
    public async Task CollectSensorData_CallsWeatherApiForEachActiveField()
    {
        // Arrange
        SetupFieldsApi(ActiveFields);
        SetupWeatherApi(WeatherResponse);

        // Act
        await _sut.CollectSensorData(new TimerInfo());

        // Assert
        _mockApiClient.Verify(x => x.GetAsync<OpenMeteoResponse>(
            It.Is<string>(url => url.Contains("open-meteo.com")),
            It.IsAny<string?>(),
            It.IsAny<IDictionary<string, string>?>()), Times.Exactly(2));
    }

    private void SetupFieldsApi(List<FieldResponse>? fields)
    {
        _mockApiClient
            .Setup(x => x.GetAsync<List<FieldResponse>>(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IDictionary<string, string>?>()))
            .ReturnsAsync(fields);
    }

    private void SetupWeatherApi(OpenMeteoResponse? response)
    {
        _mockApiClient
            .Setup(x => x.GetAsync<OpenMeteoResponse>(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IDictionary<string, string>?>()))
            .ReturnsAsync(response);
    }
}
