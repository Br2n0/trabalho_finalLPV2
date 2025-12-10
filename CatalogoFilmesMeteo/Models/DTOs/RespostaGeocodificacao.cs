using System.Text.Json.Serialization;

namespace CatalogoFilmesMeteo.Models.DTOs;

/// <summary>
/// DTO para resposta da API Nominatim (OpenStreetMap).
/// Nominatim retorna um array direto de resultados.
/// </summary>
public class ResultadoGeocodificacao
{
    [JsonPropertyName("place_id")]
    public long PlaceId { get; set; }

    [JsonPropertyName("display_name")]
    public string NomeExibicao { get; set; } = string.Empty;

    [JsonPropertyName("lat")]
    public string LatitudeStr { get; set; } = string.Empty;

    [JsonPropertyName("lon")]
    public string LongitudeStr { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public EnderecoNominatim? Endereco { get; set; }

    // Propriedades calculadas para facilitar o uso
    // Usa InvariantCulture para garantir que o ponto seja reconhecido como separador decimal
    // Retorna null se o parsing falhar, em vez de 0, para não mascarar erros
    public decimal? Latitude => decimal.TryParse(LatitudeStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat) ? lat : null;
    public decimal? Longitude => decimal.TryParse(LongitudeStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lon) ? lon : null;
}

public class EnderecoNominatim
{
    [JsonPropertyName("city")]
    public string? Cidade { get; set; }

    [JsonPropertyName("town")]
    public string? Municipio { get; set; }

    [JsonPropertyName("state")]
    public string? Estado { get; set; }

    [JsonPropertyName("country")]
    public string? Pais { get; set; }
}

