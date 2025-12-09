using System.Text.Json.Serialization;

namespace CatalogoFilmesMeteo.Models.DTOs;

public class RespostaImagensTmdb
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("backdrops")]
    public List<ImagemTmdb> Fundos { get; set; } = new();

    [JsonPropertyName("posters")]
    public List<ImagemTmdb> Posters { get; set; } = new();
}

public class ImagemTmdb
{
    [JsonPropertyName("file_path")]
    public string CaminhoArquivo { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public int Largura { get; set; }

    [JsonPropertyName("height")]
    public int Altura { get; set; }

    [JsonPropertyName("aspect_ratio")]
    public decimal Proporcao { get; set; }

    [JsonPropertyName("vote_average")]
    public decimal MediaVotos { get; set; }

    [JsonPropertyName("vote_count")]
    public int TotalVotos { get; set; }
}