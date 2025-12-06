using System.Text.Json.Serialization;

namespace CatalogoFilmesMeteo.Models.DTOs;

public class RespostaBuscaTmdb
{
    [JsonPropertyName("page")]
    public int Pagina { get; set; }

    [JsonPropertyName("results")]
    public List<ResultadoFilmeTmdb> Resultados { get; set; } = new();

    [JsonPropertyName("total_pages")]
    public int TotalPaginas { get; set; }

    [JsonPropertyName("total_results")]
    public int TotalResultados { get; set; }
}

public class ResultadoFilmeTmdb
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Titulo { get; set; } = string.Empty;

    [JsonPropertyName("original_title")]
    public string TituloOriginal { get; set; } = string.Empty;

    [JsonPropertyName("overview")]
    public string? Sinopse { get; set; }

    [JsonPropertyName("release_date")]
    public string? DataLancamento { get; set; }

    [JsonPropertyName("poster_path")]
    public string? CaminhoPoster { get; set; }

    [JsonPropertyName("backdrop_path")]
    public string? CaminhoFundo { get; set; }

    [JsonPropertyName("vote_average")]
    public decimal MediaVotos { get; set; }

    [JsonPropertyName("vote_count")]
    public int TotalVotos { get; set; }
}

