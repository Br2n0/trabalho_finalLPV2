using CatalogoFilmesMeteo.Models;

namespace CatalogoFilmesMeteo.Repositories;

/// <summary>
/// Interface do repositório de filmes.
/// Define as operações CRUD básicas para gerenciar filmes no banco de dados.
/// </summary>
public interface IFilmeRepository
{
    /// <summary>
    /// Cria um novo filme no banco de dados.
    /// </summary>
    Task<Filme> CreateAsync(Filme filme);
    
    /// <summary>
    /// Busca um filme pelo ID interno do banco.
    /// </summary>
    Task<Filme?> GetByIdAsync(int id);
    
    /// <summary>
    /// Busca um filme pelo ID do TMDb (útil para verificar duplicatas).
    /// </summary>
    Task<Filme?> GetByTmdbIdAsync(int tmdbId);
    
    /// <summary>
    /// Retorna todos os filmes cadastrados.
    /// </summary>
    Task<IEnumerable<Filme>> ListAsync();
    
    /// <summary>
    /// Atualiza um filme existente.
    /// </summary>
    Task<Filme> UpdateAsync(Filme filme);
    
    /// <summary>
    /// Remove um filme do banco de dados.
    /// Retorna true se o filme foi removido, false se não foi encontrado.
    /// </summary>
    Task<bool> DeleteAsync(int id);
}
