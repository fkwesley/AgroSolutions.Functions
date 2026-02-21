using System.Text.Json.Serialization;

namespace AgroSolutions.Functions.Models;

public class SensorDataRequest
{
    [JsonPropertyName("fieldId")]
    public int FieldId { get; set; }

    [JsonPropertyName("soilMoisture")]
    public decimal SoilMoisture { get; set; }

    [JsonPropertyName("airTemperature")]
    public decimal AirTemperature { get; set; }

    [JsonPropertyName("precipitation")]
    public decimal Precipitation { get; set; }

    [JsonPropertyName("collectedAt")]
    public DateTime CollectedAt { get; set; }

    [JsonPropertyName("alertEmailTo")]
    public string AlertEmailTo { get; set; } = string.Empty;
}
