using CatalogoFilmesMeteo.Models;
using CatalogoFilmesMeteo.Models.DTOs;

namespace CatalogoFilmesMeteo.Services;

/// <summary>
/// Interface para o serviço de importação de filmes do TMDb.
/// Responsável por mapear dados do TMDb para o modelo local e persistir no banco.
/// </summary>
public interface IFilmeImportService
{
    /// <summary>
    /// Importa um filme do TMDb para o banco de dados local.
    /// Faz o mapeamento de DetalhesFilmeTmdb para Filme e salva.
    /// </summary>
    Task<Filme> ImportarFilmeAsync(DetalhesFilmeTmdb detalhesTmdb);

    /// <summary>
    /// Importa um filme pelo seu TMDb ID, opcionalmente definindo cidade e coordenadas.
    /// Faz a busca dos detalhes no TMDb e persiste no banco local.
    /// </summary>
    Task<Filme> ImportarFilmeAsync(int tmdbId, string? cidade, decimal? latitude, decimal? longitude);
}