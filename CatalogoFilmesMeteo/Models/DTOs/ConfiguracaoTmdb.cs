using System.Text.Json.Serialization;

namespace CatalogoFilmesMeteo.Models.DTOs;

public class ConfiguracaoTmdb
{
    [JsonPropertyName("images")]
    public ConfiguracaoImagensTmdb? Imagens { get; set; }
}

public class ConfiguracaoImagensTmdb
{
    [JsonPropertyName("base_url")]
    public string UrlBase { get; set; } = string.Empty;

    [JsonPropertyName("secure_base_url")]
    public string UrlBaseSegura { get; set; } = string.Empty;

    [JsonPropertyName("backdrop_sizes")]
    public List<string> TamanhosFundo { get; set; } = new();

    [JsonPropertyName("logo_sizes")]
    public List<string> TamanhosLogo { get; set; } = new();

    [JsonPropertyName("poster_sizes")]
    public List<string> TamanhosPoster { get; set; } = new();

    [JsonPropertyName("profile_sizes")]
    public List<string> TamanhosPerfil { get; set; } = new();

    [JsonPropertyName("still_sizes")]
    public List<string> TamanhosStill { get; set; } = new();
}

