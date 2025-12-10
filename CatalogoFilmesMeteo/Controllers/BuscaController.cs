using Microsoft.AspNetCore.Mvc;
using CatalogoFilmesMeteo.Services;
using CatalogoFilmesMeteo.Models.DTOs;
using System.Linq;

namespace CatalogoFilmesMeteo.Controllers;

public class BuscaController : Controller
{
    private readonly IServicoTmdbApi _tmdbService;
    private readonly IFilmeImportService _importService;
    private readonly IServicoGeocodificacao _geocodificacao;
    private readonly ILogger<BuscaController> _logger;

    public BuscaController(
        IServicoTmdbApi tmdbService,
        IFilmeImportService importService,
        IServicoGeocodificacao geocodificacao,
        ILogger<BuscaController> logger)
    {
        _tmdbService = tmdbService;
        _importService = importService;
        _geocodificacao = geocodificacao;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Gêneros principais do TMDb (IDs)
        var generos = new Dictionary<string, int>
        {
            { "Ação", 28 },
            { "Comédia", 35 },
            { "Drama", 18 },
            { "Terror", 27 },
            { "Ficção Científica", 878 },
            { "Romance", 10749 }
        };

        var filmesPorGenero = new Dictionary<string, RespostaBuscaTmdb>();

        // Buscar filmes para cada gênero (apenas primeira página, top 4-6 filmes)
        foreach (var genero in generos)
        {
            try
            {
                var resultado = await _tmdbService.BuscarFilmesPorGeneroAsync(genero.Value, 1);
                if (resultado != null && resultado.Resultados.Any())
                {
                    // Limitar a 6 filmes por gênero
                    resultado.Resultados = resultado.Resultados.Take(6).ToList();
                    filmesPorGenero[genero.Key] = resultado;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao buscar filmes do gênero {Genero} (ID: {Id})", genero.Key, genero.Value);
                // Continua mesmo se um gênero falhar
            }
        }

        ViewBag.FilmesPorGenero = filmesPorGenero;
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
        // Parsing manual de coordenadas (inputs HTML usam ponto decimal)
        if (Request.Form.ContainsKey("latitude") && !string.IsNullOrWhiteSpace(Request.Form["latitude"]))
        {
            var latStr = Request.Form["latitude"].ToString().Replace(',', '.');
            if (decimal.TryParse(latStr, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out var lat))
            {
                latitude = lat;
            }
        }
        
        if (Request.Form.ContainsKey("longitude") && !string.IsNullOrWhiteSpace(Request.Form["longitude"]))
        {
            var lonStr = Request.Form["longitude"].ToString().Replace(',', '.');
            if (decimal.TryParse(lonStr, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out var lon))
            {
                longitude = lon;
            }
        }

        _logger.LogInformation("Iniciando importação - TmdbId: {TmdbId}, Cidade: {Cidade}, Lat: {Lat}, Lon: {Lon}", 
            tmdbId, cidade ?? "null", 
            latitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null", 
            longitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null");

        try
        {
            // Validação de latitude (-90 a 90)
            if (latitude.HasValue && (latitude < -90 || latitude > 90))
            {
                _logger.LogWarning("Latitude inválida: {Latitude}", latitude);
                TempData["ErrorMessage"] = "Latitude deve estar entre -90 e 90 graus.";
                return RedirectToAction("Pesquisar", new { query = ViewBag.Query ?? "" });
            }

            // Validação de longitude (-180 a 180)
            if (longitude.HasValue && (longitude < -180 || longitude > 180))
            {
                _logger.LogWarning("Longitude inválida: {Longitude}", longitude);
                TempData["ErrorMessage"] = "Longitude deve estar entre -180 e 180 graus.";
                return RedirectToAction("Pesquisar", new { query = ViewBag.Query ?? "" });
            }

            var filme = await _importService.ImportarFilmeAsync(tmdbId, cidade, latitude, longitude);
            
            _logger.LogInformation("Filme importado com sucesso - Id: {Id}, Titulo: {Titulo}, Cidade: {Cidade}, Lat: {Lat}, Lon: {Lon}", 
                filme.Id, filme.Titulo, filme.CidadeReferencia ?? "null", 
                filme.Latitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null", 
                filme.Longitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null");
            
            TempData["SuccessMessage"] = $"Filme '{filme.Titulo}' importado com sucesso!";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao importar filme - TmdbId: {TmdbId}", tmdbId);
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao importar filme - TmdbId: {TmdbId}, Cidade: {Cidade}, Lat: {Lat}, Lon: {Lon}", 
                tmdbId, cidade ?? "null", latitude?.ToString() ?? "null", longitude?.ToString() ?? "null");
            TempData["ErrorMessage"] = "Erro ao importar filme. Tente novamente.";
        }

        return RedirectToAction("Index", "Gerenciar");
    }

    /// <summary>
    /// Endpoint AJAX para buscar coordenadas de uma cidade usando Nominatim.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> BuscarCoordenadas(string cidade)
    {
        if (string.IsNullOrWhiteSpace(cidade))
        {
            return Json(new { sucesso = false, mensagem = "Nome da cidade é obrigatório." });
        }

        try
        {
            _logger.LogInformation("Buscando coordenadas via AJAX para cidade: {Cidade}", cidade);
            
            var coordenadas = await _geocodificacao.ObterCoordenadasAsync(cidade);
            
            if (coordenadas.HasValue)
            {
                _logger.LogInformation("Coordenadas encontradas - Cidade: {Cidade}, Lat: {Lat}, Lon: {Lon}", 
                    cidade, coordenadas.Value.latitude, coordenadas.Value.longitude);
                
                // Usar ToString com formato F6 para limitar a 6 casas decimais e InvariantCulture para garantir ponto decimal no JSON
                return Json(new 
                { 
                    sucesso = true, 
                    latitude = coordenadas.Value.latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture), 
                    longitude = coordenadas.Value.longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) 
                });
            }
            else
            {
                _logger.LogWarning("Cidade não encontrada no Nominatim: {Cidade}", cidade);
                return Json(new { sucesso = false, mensagem = $"Cidade '{cidade}' não encontrada. Verifique o nome e tente novamente." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar coordenadas para cidade: {Cidade}", cidade);
            return Json(new { sucesso = false, mensagem = "Erro ao buscar coordenadas. Tente novamente." });
        }
    }
}