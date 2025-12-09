using Microsoft.AspNetCore.Mvc;
using CatalogoFilmesMeteo.Models;
using CatalogoFilmesMeteo.Services;
using CatalogoFilmesMeteo.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CatalogoFilmesMeteo.Controllers;

public class FilmeController : Controller
{
    private readonly IFilmeRepository _filmeRepository;
    private readonly IServicoTmdbApi _tmdbService;
    private readonly ILogger<FilmeController> _logger;

    public FilmeController(
        IFilmeRepository filmeRepository,
        IServicoTmdbApi tmdbService,
        ILogger<FilmeController> logger)
    {
        _filmeRepository = filmeRepository;
        _tmdbService = tmdbService;
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
}