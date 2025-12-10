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

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        try
        {
            var filme = await _filmeRepository.GetByIdAsync(id);
            if (filme == null)
            {
                TempData["ErrorMessage"] = "Filme não encontrado.";
                return RedirectToAction("Index", "Gerenciar");
            }
            
            return View(filme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar filme para edição");
            TempData["ErrorMessage"] = "Erro ao carregar filme para edição.";
            return RedirectToAction("Index", "Gerenciar");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(Filme filme)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(filme);
            }

            // Preserva os campos que não são editáveis
            var filmeExistente = await _filmeRepository.GetByIdAsync(filme.Id);
            if (filmeExistente == null)
            {
                TempData["ErrorMessage"] = "Filme não encontrado.";
                return RedirectToAction("Index", "Gerenciar");
            }

            // Atualiza apenas os campos editáveis
            filmeExistente.Titulo = filme.Titulo;
            filmeExistente.TituloOriginal = filme.TituloOriginal;
            filmeExistente.Sinopse = filme.Sinopse;
            filmeExistente.DataLancamento = filme.DataLancamento;
            filmeExistente.Genero = filme.Genero;
            filmeExistente.Lingua = filme.Lingua;
            filmeExistente.Duracao = filme.Duracao;
            filmeExistente.NotaMedia = filme.NotaMedia;
            filmeExistente.ElencoPrincipal = filme.ElencoPrincipal;
            filmeExistente.CidadeReferencia = filme.CidadeReferencia;
            filmeExistente.Latitude = filme.Latitude;
            filmeExistente.Longitude = filme.Longitude;
            filmeExistente.DataAtualizacao = DateTime.Now;

            await _filmeRepository.UpdateAsync(filmeExistente);

            TempData["SuccessMessage"] = $"Filme '{filme.Titulo}' atualizado com sucesso!";
            return RedirectToAction("Detalhes", new { id = filme.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar filme ID: {Id}", filme.Id);
            TempData["ErrorMessage"] = "Erro ao atualizar filme. Tente novamente.";
            return View(filme);
        }
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