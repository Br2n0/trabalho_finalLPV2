using CatalogoFilmesMeteo.Models.DTOs;

namespace CatalogoFilmesMeteo.Services;

public interface IServicoApiTempo
{
    Task<RespostaPrevisaoTempo> ObterPrevisaoDiariaAsync(decimal latitude, decimal longitude);
}

