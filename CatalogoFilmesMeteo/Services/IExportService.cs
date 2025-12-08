using CatalogoFilmesMeteo.Models;

namespace CatalogoFilmesMeteo.Services;

/// <summary>
/// Interface para o serviço de exportação de filmes.
/// Permite exportar o catálogo para formatos CSV e Excel.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Exporta uma lista de filmes para formato CSV.
    /// Retorna os bytes do arquivo CSV gerado.
    /// </summary>
    Task<byte[]> ExportToCsvAsync(IEnumerable<Filme> filmes);
    
    /// <summary>
    /// Exporta uma lista de filmes para formato Excel (.xlsx).
    /// Retorna os bytes do arquivo Excel gerado.
    /// </summary>
    Task<byte[]> ExportToExcelAsync(IEnumerable<Filme> filmes);
}
