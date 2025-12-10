namespace CatalogoFilmesMeteo.Services;

/// <summary>
/// Interface para o serviço de geocodificação (conversão de cidade em coordenadas).
/// </summary>
public interface IServicoGeocodificacao
{
    /// <summary>
    /// Obtém as coordenadas (latitude e longitude) de uma cidade.
    /// </summary>
    /// <param name="cidade">Nome da cidade</param>
    /// <returns>Tupla com latitude e longitude, ou null se não encontrado</returns>
    Task<(decimal latitude, decimal longitude)?> ObterCoordenadasAsync(string cidade);
}

