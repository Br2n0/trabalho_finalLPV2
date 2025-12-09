using Microsoft.AspNetCore.Mvc;
using CatalogoFilmesMeteo.Services;

namespace CatalogoFilmesMeteo.Controllers;

public class BuscaController : Controller
{
    private readonly IServicoTmdbApi _tmdbService;
    private readonly IFilmeImportService _importService;
    private readonly ILogger<BuscaController> _logger;

    public BuscaController(
        IServicoTmdbApi tmdbService,
        IFilmeImportService importService,
        ILogger<BuscaController> logger)
    {
        _tmdbService = tmdbService;
        _importService = importService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Pesquisar(string query, int pagina = 1)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            TempData["ErrorMessage"] = "Por favor, digite algo para pesquisar";
            return RedirectToAction("Index");
        }

        try
        {
            var resultado = await _tmdbService.BuscarFilmesAsync(query, pagina);
            ViewBag.Query = query;
            return View(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao pesquisar filmes");
            TempData["ErrorMessage"] = "Erro ao pesquisar filmes. Tente novamente.";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Importar(int tmdbId, string? cidade, decimal? latitude, decimal? longitude)
    {
        try
        {
            await _importService.ImportarFilmeAsync(tmdbId, cidade, latitude, longitude);
            TempData["SuccessMessage"] = "Filme importado com sucesso!";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao importar filme");
            TempData["ErrorMessage"] = "Erro ao importar filme. Tente novamente.";
        }

        return RedirectToAction("Index", "Gerenciar");
    }
}