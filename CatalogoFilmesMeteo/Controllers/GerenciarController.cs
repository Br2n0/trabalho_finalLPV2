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
    private readonly IServicoGeocodificacao _geocodificacao;
    private readonly ILogger<GerenciarController> _logger;

    public GerenciarController(
        IFilmeRepository filmeRepository,
        IServicoTmdbApi tmdbService,
        IServicoApiTempo weatherService,
        IServicoGeocodificacao geocodificacao,
        ILogger<GerenciarController> logger)
    {
        _filmeRepository = filmeRepository;
        _tmdbService = tmdbService;
        _weatherService = weatherService;
        _geocodificacao = geocodificacao;
        _logger = logger;
    }

    // GET: /Gerenciar
    public async Task<IActionResult> Index()
    {
        var filmes = await _filmeRepository.ListAsync();
        var filmesList = filmes.ToList();
        
        // Buscar previsões do tempo para filmes com coordenadas
        var previsoes = new Dictionary<int, Models.DTOs.RespostaPrevisaoTempo>();
        
        var tasks = filmesList
            .Where(f => f.Latitude.HasValue && f.Longitude.HasValue)
            .Select(async filme =>
            {
                try
                {
                    var previsao = await _weatherService.ObterPrevisaoDiariaAsync(
                        filme.Latitude.Value,
                        filme.Longitude.Value);
                    previsoes[filme.Id] = previsao;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao obter previsão do tempo para filme {FilmeId}", filme.Id);
                }
            });

        await Task.WhenAll(tasks);
        
        ViewBag.Previsoes = previsoes;
        return View(filmesList);
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

        // Buscar previsão do tempo se tiver coordenadas ou cidade
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
        else if (!string.IsNullOrWhiteSpace(filme.CidadeReferencia))
        {
            // Se não tem coordenadas mas tem cidade, tenta buscar previsão pela cidade
            try
            {
                var previsao = await _weatherService.ObterPrevisaoPorCidadeAsync(filme.CidadeReferencia);
                if (previsao != null)
                {
                    ViewBag.Previsao = previsao;
                    // Atualiza as coordenadas no banco se foram obtidas
                    if (!filme.Latitude.HasValue || !filme.Longitude.HasValue)
                    {
                        filme.Latitude = previsao.Latitude;
                        filme.Longitude = previsao.Longitude;
                        filme.DataAtualizacao = DateTime.Now;
                        await _filmeRepository.UpdateAsync(filme);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao obter previsão do tempo por cidade para o filme {FilmeId}", id);
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
        // Parsing manual de coordenadas (inputs HTML usam ponto decimal)
        if (Request.Form.ContainsKey("Latitude") && !string.IsNullOrWhiteSpace(Request.Form["Latitude"]))
        {
            var latStr = Request.Form["Latitude"].ToString().Replace(',', '.');
            if (decimal.TryParse(latStr, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out var lat))
            {
                filme.Latitude = lat;
            }
        }
        
        if (Request.Form.ContainsKey("Longitude") && !string.IsNullOrWhiteSpace(Request.Form["Longitude"]))
        {
            var lonStr = Request.Form["Longitude"].ToString().Replace(',', '.');
            if (decimal.TryParse(lonStr, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out var lon))
            {
                filme.Longitude = lon;
            }
        }

        _logger.LogInformation("Iniciando edição de filme - Id: {Id}, Cidade: {Cidade}, Lat: {Lat}, Lon: {Lon}", 
            id, filme.CidadeReferencia ?? "null", 
            filme.Latitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null", 
            filme.Longitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null");

        if (id != filme.Id)
        {
            _logger.LogWarning("Tentativa de edição com ID inconsistente - Id recebido: {Id}, Id do filme: {FilmeId}", id, filme.Id);
            return BadRequest();
        }

        // Busca o filme existente para preservar campos que não são editáveis
        var filmeExistente = await _filmeRepository.GetByIdAsync(id);
        if (filmeExistente == null)
        {
            _logger.LogWarning("Filme não encontrado para edição - Id: {Id}", id);
            TempData["ErrorMessage"] = "Filme não encontrado";
            return RedirectToAction("Index");
        }

        // Validação de latitude (-90 a 90)
        if (filme.Latitude.HasValue && (filme.Latitude < -90 || filme.Latitude > 90))
        {
            _logger.LogWarning("Latitude inválida: {Latitude}", filme.Latitude);
            ModelState.AddModelError(nameof(filme.Latitude), "Latitude deve estar entre -90 e 90 graus.");
        }

        // Validação de longitude (-180 a 180)
        if (filme.Longitude.HasValue && (filme.Longitude < -180 || filme.Longitude > 180))
        {
            _logger.LogWarning("Longitude inválida: {Longitude}", filme.Longitude);
            ModelState.AddModelError(nameof(filme.Longitude), "Longitude deve estar entre -180 e 180 graus.");
        }

        // PRESERVA TODOS OS CAMPOS DO TMDb - apenas atualiza localização
        filme.TmdbId = filmeExistente.TmdbId;
        filme.Titulo = filmeExistente.Titulo;
        filme.TituloOriginal = filmeExistente.TituloOriginal;
        filme.Sinopse = filmeExistente.Sinopse;
        filme.DataLancamento = filmeExistente.DataLancamento;
        filme.Genero = filmeExistente.Genero;
        filme.PosterPath = filmeExistente.PosterPath;
        filme.Lingua = filmeExistente.Lingua;
        filme.Duracao = filmeExistente.Duracao;
        filme.NotaMedia = filmeExistente.NotaMedia;
        filme.ElencoPrincipal = filmeExistente.ElencoPrincipal;
        filme.DataCriacao = filmeExistente.DataCriacao;

        // Remove campos do TMDb do ModelState para evitar validação
        ModelState.Remove(nameof(filme.Titulo));
        ModelState.Remove(nameof(filme.TituloOriginal));
        ModelState.Remove(nameof(filme.Sinopse));
        ModelState.Remove(nameof(filme.DataLancamento));
        ModelState.Remove(nameof(filme.Genero));
        ModelState.Remove(nameof(filme.Lingua));
        ModelState.Remove(nameof(filme.Duracao));
        ModelState.Remove(nameof(filme.NotaMedia));
        ModelState.Remove(nameof(filme.ElencoPrincipal));

        // Se cidade foi fornecida mas não há coordenadas, busca coordenadas via geocodificação
        if (!string.IsNullOrWhiteSpace(filme.CidadeReferencia) && 
            (!filme.Latitude.HasValue || !filme.Longitude.HasValue))
        {
            try
            {
                _logger.LogDebug("Buscando coordenadas para cidade: {Cidade}", filme.CidadeReferencia);
                var coordenadas = await _geocodificacao.ObterCoordenadasAsync(filme.CidadeReferencia);
                if (coordenadas.HasValue)
                {
                    filme.Latitude = coordenadas.Value.latitude;
                    filme.Longitude = coordenadas.Value.longitude;
                    _logger.LogInformation("Coordenadas obtidas para cidade {Cidade}: Lat={Lat}, Lon={Lon}", 
                        filme.CidadeReferencia, filme.Latitude, filme.Longitude);
                }
                else
                {
                    _logger.LogWarning("Não foi possível obter coordenadas para a cidade: {Cidade}", 
                        filme.CidadeReferencia);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar coordenadas para cidade: {Cidade}", filme.CidadeReferencia);
                // Continua mesmo se falhar a geocodificação
            }
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState inválido para filme {Id}", id);
            return View(filme);
        }

        try
        {
            filme.DataAtualizacao = DateTime.Now;
            await _filmeRepository.UpdateAsync(filme);
            
            _logger.LogInformation("Filme atualizado com sucesso - Id: {Id}, Cidade: {Cidade}, Lat: {Lat}, Lon: {Lon}", 
                filme.Id, filme.CidadeReferencia ?? "null", 
                filme.Latitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null", 
                filme.Longitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null");
            
            TempData["SuccessMessage"] = "Localização do filme atualizada com sucesso!";
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

    // GET: /Gerenciar/AdicionarLocalizacao/5
    public async Task<IActionResult> AdicionarLocalizacao(int id)
    {
        var filme = await _filmeRepository.GetByIdAsync(id);

        if (filme == null)
        {
            TempData["ErrorMessage"] = "Filme não encontrado";
            return RedirectToAction("Index");
        }

        return View(filme);
    }

    // POST: /Gerenciar/AdicionarLocalizacao/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdicionarLocalizacao(int id, string? cidade, decimal? latitude, decimal? longitude)
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

        _logger.LogInformation("Adicionando localização - FilmeId: {Id}, Cidade: {Cidade}, Lat: {Lat}, Lon: {Lon}", 
            id, cidade ?? "null", 
            latitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null", 
            longitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null");

        var filme = await _filmeRepository.GetByIdAsync(id);
        if (filme == null)
        {
            _logger.LogWarning("Filme não encontrado para adicionar localização - Id: {Id}", id);
            TempData["ErrorMessage"] = "Filme não encontrado";
            return RedirectToAction("Index");
        }

        // Validação de latitude (-90 a 90)
        if (latitude.HasValue && (latitude < -90 || latitude > 90))
        {
            _logger.LogWarning("Latitude inválida: {Latitude}", latitude);
            TempData["ErrorMessage"] = "Latitude deve estar entre -90 e 90 graus.";
            return View(filme);
        }

        // Validação de longitude (-180 a 180)
        if (longitude.HasValue && (longitude < -180 || longitude > 180))
        {
            _logger.LogWarning("Longitude inválida: {Longitude}", longitude);
            TempData["ErrorMessage"] = "Longitude deve estar entre -180 e 180 graus.";
            return View(filme);
        }

        // Se cidade foi fornecida mas não há coordenadas, busca coordenadas via geocodificação
        if (!string.IsNullOrWhiteSpace(cidade) && (!latitude.HasValue || !longitude.HasValue))
        {
            try
            {
                _logger.LogDebug("Buscando coordenadas para cidade: {Cidade}", cidade);
                var coordenadas = await _geocodificacao.ObterCoordenadasAsync(cidade);
                if (coordenadas.HasValue)
                {
                    latitude = coordenadas.Value.latitude;
                    longitude = coordenadas.Value.longitude;
                    _logger.LogInformation("Coordenadas obtidas para cidade {Cidade}: Lat={Lat}, Lon={Lon}", 
                        cidade, latitude, longitude);
                }
                else
                {
                    _logger.LogWarning("Não foi possível obter coordenadas para a cidade: {Cidade}", cidade);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar coordenadas para cidade: {Cidade}", cidade);
                // Continua mesmo se falhar a geocodificação
            }
        }

        // Atualiza apenas os campos de localização
        if (!string.IsNullOrWhiteSpace(cidade))
        {
            filme.CidadeReferencia = cidade.Trim();
        }
        if (latitude.HasValue)
        {
            filme.Latitude = latitude;
        }
        if (longitude.HasValue)
        {
            filme.Longitude = longitude;
        }
        filme.DataAtualizacao = DateTime.Now;

        try
        {
            await _filmeRepository.UpdateAsync(filme);
            
            _logger.LogInformation("Localização adicionada com sucesso - FilmeId: {Id}, Cidade: {Cidade}, Lat: {Lat}, Lon: {Lon}", 
                filme.Id, filme.CidadeReferencia ?? "null", 
                filme.Latitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null", 
                filme.Longitude?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null");
            
            TempData["SuccessMessage"] = "Localização adicionada com sucesso!";
            return RedirectToAction("Detalhes", new { id = filme.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar localização ao filme {FilmeId}", id);
            TempData["ErrorMessage"] = "Erro ao adicionar localização. Tente novamente.";
            return View(filme);
        }
    }
}