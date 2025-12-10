using CatalogoFilmesMeteo.Models.DTOs;

namespace CatalogoFilmesMeteo.Services;

/// <summary>
/// Interface para o serviço de API do TMDb (The Movie Database).
/// </summary>
public interface IServicoTmdbApi
{
    /// <summary>
    /// Busca filmes por termo de pesquisa.
    /// </summary>
    Task<RespostaBuscaTmdb> BuscarFilmesAsync(string consulta, int pagina);

    /// <summary>
    /// Obtém os detalhes completos de um filme.
    /// </summary>
    Task<DetalhesFilmeTmdb> ObterDetalhesFilmeAsync(int tmdbId);

    /// <summary>
    /// Obtém as imagens de um filme.
    /// </summary>
    Task<RespostaImagensTmdb> ObterImagensFilmeAsync(int tmdbId);

    /// <summary>
    /// Obtém a configuração da API (URLs base para imagens, etc).
    /// </summary>
    Task<ConfiguracaoTmdb> ObterConfiguracaoAsync();

    /// <summary>
    /// Constrói a URL completa para um poster.
    /// </summary>
    string ConstruirUrlPoster(string? posterPath, string tamanho = "w500");

    /// <summary>
    /// Busca filmes por gênero usando o endpoint discover.
    /// </summary>
    Task<RespostaBuscaTmdb> BuscarFilmesPorGeneroAsync(int generoId, int pagina = 1);
}