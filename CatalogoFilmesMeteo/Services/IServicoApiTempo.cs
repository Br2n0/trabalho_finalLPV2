using CatalogoFilmesMeteo.Models.DTOs;

namespace CatalogoFilmesMeteo.Services;

/// <summary>
/// Interface para o serviço de API de tempo/meteorologia.
/// </summary>
public interface IServicoApiTempo
{
    /// <summary>
    /// Obtém a previsão diária do tempo para coordenadas específicas.
    /// </summary>
    Task<RespostaPrevisaoTempo> ObterPrevisaoDiariaAsync(decimal latitude, decimal longitude);
}