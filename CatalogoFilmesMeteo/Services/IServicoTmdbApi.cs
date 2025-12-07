using CatalogoFilmesMeteo.Models.DTOs;

namespace CatalogoFilmesMeteo.Services;

public interface IServicoTmdbApi
{
    Task<RespostaBuscaTmdb> BuscarFilmesAsync(string consulta, int pagina);
    Task<DetalhesFilmeTmdb> ObterDetalhesFilmeAsync(int tmdbId);
    Task<RespostaImagensTmdb> ObterImagensFilmeAsync(int tmdbId);
    Task<ConfiguracaoTmdb> ObterConfiguracaoAsync();
}

