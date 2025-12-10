using Microsoft.AspNetCore.Mvc;
using CatalogoFilmesMeteo.Models;
using CatalogoFilmesMeteo.Services;
using CatalogoFilmesMeteo.Repositories;

namespace CatalogoFilmesMeteo.Controllers;

public class FilmeController : Controller
{
    private readonly IFilmeRepository _filmeRepository;
    private readonly IServicoTmdbApi _tmdbService;
    private readonly IExportService _exportService;
    private readonly ILogger<FilmeController> _logger;

    public FilmeController(
        IFilmeRepository filmeRepository,
        IServicoTmdbApi tmdbService,
        IExportService exportService,
        ILogger<FilmeController> logger)
    {
        _filmeRepository = filmeRepository;
        _tmdbService = tmdbService;
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Exporta o catálogo de filmes para formato CSV.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportCsv()
    {
        try
        {
            var filmes = await _filmeRepository.ListAsync();
            var bytes = await _exportService.ExportToCsvAsync(filmes);

            _logger.LogInformation("Exportação CSV realizada com sucesso. {Count} filmes exportados", filmes.Count());

            var nomeArquivo = $"catalogo_filmes_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv", nomeArquivo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar catálogo para CSV");
            TempData["ErrorMessage"] = "Erro ao exportar catálogo para CSV. Tente novamente.";
            return RedirectToAction("Index", "Gerenciar");
        }
    }

    /// <summary>
    /// Exporta o catálogo de filmes para formato Excel.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        try
        {
            var filmes = await _filmeRepository.ListAsync();
            var bytes = await _exportService.ExportToExcelAsync(filmes);

            _logger.LogInformation("Exportação Excel realizada com sucesso. {Count} filmes exportados", filmes.Count());

            var nomeArquivo = $"catalogo_filmes_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nomeArquivo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar catálogo para Excel");
            TempData["ErrorMessage"] = "Erro ao exportar catálogo para Excel. Tente novamente.";
            return RedirectToAction("Index", "Gerenciar");
        }
    }
}