namespace CatalogoFilmesMeteo.Models;

public class Filme
{
    public int Id { get; set; }
    public int TmdbId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string TituloOriginal { get; set; } = string.Empty;
    public string? Sinopse { get; set; }
    public DateTime? DataLancamento { get; set; }
    public string? Genero { get; set; }
    public string? PosterPath { get; set; }
    public string? Lingua { get; set; }
    public int? Duracao { get; set; }
    public decimal? NotaMedia { get; set; }
    public string? ElencoPrincipal { get; set; }
    public string? CidadeReferencia { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }
}

