using System.Text.Json.Serialization;

namespace AgroSolutions.Functions.Models;

public class FieldResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("farmId")]
    public int FarmId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("areaHectares")]
    public decimal AreaHectares { get; set; }

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("totalCropSeasons")]
    public int TotalCropSeasons { get; set; }

    [JsonPropertyName("activeCropSeasons")]
    public int ActiveCropSeasons { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}
