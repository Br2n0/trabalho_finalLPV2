using System.Text.Json.Serialization;

namespace CatalogoFilmesMeteo.Models.DTOs;

public class DetalhesFilmeTmdb
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

    [JsonPropertyName("runtime")]
    public int? Duracao { get; set; }

    [JsonPropertyName("vote_average")]
    public decimal MediaVotos { get; set; }

    [JsonPropertyName("vote_count")]
    public int TotalVotos { get; set; }

    [JsonPropertyName("genres")]
    public List<GeneroTmdb> Generos { get; set; } = new();

    [JsonPropertyName("spoken_languages")]
    public List<IdiomaTmdb> Idiomas { get; set; } = new();

    [JsonPropertyName("credits")]
    public CreditosTmdb? Creditos { get; set; }
}

public class GeneroTmdb
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Nome { get; set; } = string.Empty;
}

public class IdiomaTmdb
{
    [JsonPropertyName("iso_639_1")]
    public string CodigoIso { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Nome { get; set; } = string.Empty;
}

public class CreditosTmdb
{
    [JsonPropertyName("cast")]
    public List<ElencoTmdb> Elenco { get; set; } = new();
}

public class ElencoTmdb
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("character")]
    public string? Personagem { get; set; }

    [JsonPropertyName("order")]
    public int Ordem { get; set; }
}