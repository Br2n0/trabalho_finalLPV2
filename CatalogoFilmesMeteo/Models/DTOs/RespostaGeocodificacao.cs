using System.Text.Json.Serialization;

namespace CatalogoFilmesMeteo.Models.DTOs;

public class RespostaGeocodificacao
{
    [JsonPropertyName("results")]
    public List<ResultadoGeocodificacao> Resultados { get; set; } = new();
}

public class ResultadoGeocodificacao
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public decimal Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public decimal Longitude { get; set; }

    [JsonPropertyName("country")]
    public string Pais { get; set; } = string.Empty;

    [JsonPropertyName("admin1")]
    public string? Estado { get; set; }
}

