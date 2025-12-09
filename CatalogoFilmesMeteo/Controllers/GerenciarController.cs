using Microsoft.AspNetCore.Mvc;
using CatalogoFilmesMeteo.Models;
using CatalogoFilmesMeteo.Repositories;
using CatalogoFilmesMeteo.Services;

namespace CatalogoFilmesMeteo.Controllers;

public class GerenciarController : Controller
{
    private readonly IFilmeRepository _filmeRepository;
    private readonly IServicoTmdbApi _tmdbService;
    private readonly IServicoApiTempo _weatherService;
    private readonly ILogger<GerenciarController> _logger;

    public GerenciarController(
        IFilmeRepository filmeRepository,
        IServicoTmdbApi tmdbService,
        IServicoApiTempo weatherService,
        ILogger<GerenciarController> logger)
    {
        _filmeRepository = filmeRepository;
        _tmdbService = tmdbService;
        _weatherService = weatherService;
        _logger = logger;
    }

    // GET: /Gerenciar
    public async Task<IActionResult> Index()
    {
        var filmes = await _filmeRepository.ListAsync();
        return View(filmes);
    }

    // GET: /Gerenciar/Detalhes/5
    public async Task<IActionResult> Detalhes(int id)
    {
        var filme = await _filmeRepository.GetByIdAsync(id);

        if (filme == null)
        {
            TempData["ErrorMessage"] = "Filme não encontrado";
            return RedirectToAction("Index");
        }

        // Buscar previsão do tempo se tiver coordenadas
        if (filme.Latitude.HasValue && filme.Longitude.HasValue)
        {
            try
            {
                var previsao = await _weatherService.ObterPrevisaoDiariaAsync(
                    filme.Latitude.Value,
                    filme.Longitude.Value);
                ViewBag.Previsao = previsao;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao obter previsão do tempo para o filme {FilmeId}", id);
            }
        }

        // Construir URL do poster
        ViewBag.PosterUrl = _tmdbService.ConstruirUrlPoster(filme.PosterPath);

        return View(filme);
    }

    // GET: /Gerenciar/Editar/5
    public async Task<IActionResult> Editar(int id)
    {
        var filme = await _filmeRepository.GetByIdAsync(id);

        if (filme == null)
        {
            TempData["ErrorMessage"] = "Filme não encontrado";
            return RedirectToAction("Index");
        }

        return View(filme);
    }

    // POST: /Gerenciar/Editar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Filme filme)
    {
        if (id != filme.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(filme);
        }

        try
        {
            await _filmeRepository.UpdateAsync(filme);
            TempData["SuccessMessage"] = "Filme atualizado com sucesso!";
            return RedirectToAction("Detalhes", new { id = filme.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar filme {FilmeId}", id);
            TempData["ErrorMessage"] = "Erro ao atualizar filme. Tente novamente.";
            return View(filme);
        }
    }

    // GET: /Gerenciar/Deletar/5
    public async Task<IActionResult> Deletar(int id)
    {
        var filme = await _filmeRepository.GetByIdAsync(id);

        if (filme == null)
        {
            TempData["ErrorMessage"] = "Filme não encontrado";
            return RedirectToAction("Index");
        }

        ViewBag.PosterUrl = _tmdbService.ConstruirUrlPoster(filme.PosterPath);
        return View(filme);
    }

    // POST: /Gerenciar/Deletar/5
    [HttpPost, ActionName("Deletar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletarConfirmado(int id)
    {
        try
        {
            var deletado = await _filmeRepository.DeleteAsync(id);

            if (deletado)
            {
                TempData["SuccessMessage"] = "Filme removido com sucesso!";
            }
            else
            {
                TempData["ErrorMessage"] = "Filme não encontrado";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar filme {FilmeId}", id);
            TempData["ErrorMessage"] = "Erro ao deletar filme. Tente novamente.";
        }

        return RedirectToAction("Index");
    }
}