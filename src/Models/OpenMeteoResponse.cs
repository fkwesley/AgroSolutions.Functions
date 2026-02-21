using System.Text.Json.Serialization;

namespace AgroSolutions.Functions.Models;

public class OpenMeteoResponse
{
    [JsonPropertyName("current")]
    public OpenMeteoCurrent? Current { get; set; }
}

public class OpenMeteoCurrent
{
    [JsonPropertyName("temperature_2m")]
    public double Temperature2m { get; set; }

    [JsonPropertyName("precipitation")]
    public double Precipitation { get; set; }
}
