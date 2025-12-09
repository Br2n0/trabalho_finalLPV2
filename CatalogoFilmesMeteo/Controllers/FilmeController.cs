using Microsoft.AspNetCore.Mvc;
using CatalogoFilmesMeteo.Repositories;
using CatalogoFilmesMeteo.Services;

namespace CatalogoFilmesMeteo.Controllers;

/// <summary>
/// Controller responsável por gerenciar operações relacionadas a filmes,
/// incluindo exportação para CSV e Excel.
/// </summary>
public class FilmeController : Controller
{
    private readonly IFilmeRepository _filmeRepository;
    private readonly IExportService _exportService;
    private readonly ILogger<FilmeController> _logger;

    public FilmeController(
        IFilmeRepository filmeRepository,
        IExportService exportService,
        ILogger<FilmeController> logger)
    {
        _filmeRepository = filmeRepository;
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Exporta todos os filmes do catálogo para formato CSV e retorna como download.
    /// </summary>
    /// <returns>FileResult com o arquivo CSV para download</returns>
    public async Task<IActionResult> ExportCsv()
    {
        try
        {
            var filmes = await _filmeRepository.ListAsync();
            var bytes = await _exportService.ExportToCsvAsync(filmes);

            return File(
                fileContents: bytes,
                contentType: "text/csv",
                fileDownloadName: $"catalogo_filmes_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar filmes para CSV");
            TempData["ErrorMessage"] = "Erro ao exportar catálogo para CSV. Tente novamente.";
            return RedirectToAction("Index", "Gerenciar");
        }
    }

    /// <summary>
    /// Exporta todos os filmes do catálogo para formato Excel (.xlsx) e retorna como download.
    /// </summary>
    /// <returns>FileResult com o arquivo Excel para download</returns>
    public async Task<IActionResult> ExportExcel()
    {
        try
        {
            var filmes = await _filmeRepository.ListAsync();
            var bytes = await _exportService.ExportToExcelAsync(filmes);

            return File(
                fileContents: bytes,
                contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: $"catalogo_filmes_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar filmes para Excel");
            TempData["ErrorMessage"] = "Erro ao exportar catálogo para Excel. Tente novamente.";
            return RedirectToAction("Index", "Gerenciar");
        }
    }
}