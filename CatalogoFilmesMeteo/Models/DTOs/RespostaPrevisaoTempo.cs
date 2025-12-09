using System.Text.Json.Serialization;

namespace CatalogoFilmesMeteo.Models.DTOs;

public class RespostaPrevisaoTempo
{
    [JsonPropertyName("latitude")]
    public decimal Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public decimal Longitude { get; set; }

    [JsonPropertyName("timezone")]
    public string FusoHorario { get; set; } = string.Empty;

    [JsonPropertyName("daily")]
    public PrevisaoDiaria? Diario { get; set; }
}

public class PrevisaoDiaria
{
    [JsonPropertyName("time")]
    public List<string> Datas { get; set; } = new();

    [JsonPropertyName("temperature_2m_max")]
    public List<decimal?> TemperaturaMaxima { get; set; } = new();

    [JsonPropertyName("temperature_2m_min")]
    public List<decimal?> TemperaturaMinima { get; set; } = new();
}