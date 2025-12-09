using System.Text.Json;
using CatalogoFilmesMeteo.Exceptions;
using CatalogoFilmesMeteo.Models.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace CatalogoFilmesMeteo.Services;

public class ServicoTmdbApi : IServicoTmdbApi
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ServicoTmdbApi> _logger;
    private readonly string _apiKey;
    private const string BaseUrl = "https://api.themoviedb.org/3";

    public ServicoTmdbApi(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<ServicoTmdbApi> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;

        // para facilitar a execução do projeto sem necessidade de configuração manual.
        // Em produção de verdade nao faria assim 
        _apiKey = configuration["Tmdb:ApiKey"]
                  ?? Environment.GetEnvironmentVariable("TMDB_API_KEY")
                  ?? "af717ba3966e7fa1a21fd07060aac165";
    }

    public async Task<RespostaBuscaTmdb> BuscarFilmesAsync(string consulta, int pagina)
    {
        var cacheKey = $"tmdb_search_{consulta}_{pagina}";

        if (_cache.TryGetValue(cacheKey, out RespostaBuscaTmdb? cached))
        {
            _logger.LogInformation("Cache hit para busca: {Consulta}, página: {Pagina}", consulta, pagina);
            return cached!;
        }

        var url = $"{BaseUrl}/search/movie?api_key={_apiKey}&query={Uri.EscapeDataString(consulta)}&page={pagina}";

        try
        {
            _logger.LogInformation("Buscando filmes - URL: {Url}, Parâmetros: consulta={Consulta}, pagina={Pagina}", url, consulta, pagina);

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Resposta TMDb - Status: {StatusCode}, Data: {Data}", response.StatusCode, DateTime.Now);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro na API TMDb - Status: {StatusCode}, Resposta: {Resposta}", response.StatusCode, content);
                throw new ExcecaoTmdbApi($"Erro ao buscar filmes: {response.StatusCode}", (int)response.StatusCode);
            }

            var resultado = JsonSerializer.Deserialize<RespostaBuscaTmdb>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (resultado == null)
                throw new ExcecaoTmdbApi("Resposta inválida da API TMDb");

            _cache.Set(cacheKey, resultado, TimeSpan.FromMinutes(5));
            return resultado;
        }
        catch (ExcecaoTmdbApi)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar filmes - URL: {Url}", url);
            throw new ExcecaoTmdbApi($"Erro ao buscar filmes: {ex.Message}", ex);
        }
    }

    public async Task<DetalhesFilmeTmdb> ObterDetalhesFilmeAsync(int tmdbId)
    {
        var cacheKey = $"tmdb_movie_{tmdbId}";

        if (_cache.TryGetValue(cacheKey, out DetalhesFilmeTmdb? cached))
        {
            _logger.LogInformation("Cache hit para detalhes do filme: {Id}", tmdbId);
            return cached!;
        }

        var url = $"{BaseUrl}/movie/{tmdbId}?api_key={_apiKey}&append_to_response=credits";

        try
        {
            _logger.LogInformation("Obtendo detalhes do filme - URL: {Url}, ID: {Id}", url, tmdbId);

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Resposta TMDb - Status: {StatusCode}, Data: {Data}", response.StatusCode, DateTime.Now);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro na API TMDb - Status: {StatusCode}, Resposta: {Resposta}", response.StatusCode, content);
                throw new ExcecaoTmdbApi($"Erro ao obter detalhes do filme: {response.StatusCode}", (int)response.StatusCode);
            }

            var resultado = JsonSerializer.Deserialize<DetalhesFilmeTmdb>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (resultado == null)
                throw new ExcecaoTmdbApi("Resposta inválida da API TMDb");

            _cache.Set(cacheKey, resultado, TimeSpan.FromMinutes(10));
            return resultado;
        }
        catch (ExcecaoTmdbApi)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter detalhes do filme - URL: {Url}", url);
            throw new ExcecaoTmdbApi($"Erro ao obter detalhes do filme: {ex.Message}", ex);
        }
    }

    public async Task<RespostaImagensTmdb> ObterImagensFilmeAsync(int tmdbId)
    {
        var url = $"{BaseUrl}/movie/{tmdbId}/images?api_key={_apiKey}";

        try
        {
            _logger.LogInformation("Obtendo imagens do filme - URL: {Url}, ID: {Id}", url, tmdbId);

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Resposta TMDb - Status: {StatusCode}, Data: {Data}", response.StatusCode, DateTime.Now);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro na API TMDb - Status: {StatusCode}, Resposta: {Resposta}", response.StatusCode, content);
                throw new ExcecaoTmdbApi($"Erro ao obter imagens do filme: {response.StatusCode}", (int)response.StatusCode);
            }

            var resultado = JsonSerializer.Deserialize<RespostaImagensTmdb>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (resultado == null)
                throw new ExcecaoTmdbApi("Resposta inválida da API TMDb");

            return resultado;
        }
        catch (ExcecaoTmdbApi)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter imagens do filme - URL: {Url}", url);
            throw new ExcecaoTmdbApi($"Erro ao obter imagens do filme: {ex.Message}", ex);
        }
    }

    public async Task<ConfiguracaoTmdb> ObterConfiguracaoAsync()
    {
        const string cacheKey = "tmdb_config";

        if (_cache.TryGetValue(cacheKey, out ConfiguracaoTmdb? cached))
        {
            _logger.LogInformation("Cache hit para configuração TMDb");
            return cached!;
        }

        var url = $"{BaseUrl}/configuration?api_key={_apiKey}";

        try
        {
            _logger.LogInformation("Obtendo configuração TMDb - URL: {Url}", url);

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Resposta TMDb - Status: {StatusCode}, Data: {Data}", response.StatusCode, DateTime.Now);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro na API TMDb - Status: {StatusCode}, Resposta: {Resposta}", response.StatusCode, content);
                throw new ExcecaoTmdbApi($"Erro ao obter configuração: {response.StatusCode}", (int)response.StatusCode);
            }

            var resultado = JsonSerializer.Deserialize<ConfiguracaoTmdb>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (resultado == null)
                throw new ExcecaoTmdbApi("Resposta inválida da API TMDb");

            _cache.Set(cacheKey, resultado, TimeSpan.FromHours(1));
            return resultado;
        }
        catch (ExcecaoTmdbApi)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter configuração TMDb - URL: {Url}", url);
            throw new ExcecaoTmdbApi($"Erro ao obter configuração: {ex.Message}", ex);
        }
    }

    public string ConstruirUrlPoster(string? posterPath, string tamanho = "w500")
    {
        if (string.IsNullOrEmpty(posterPath))
            return "/images/no-poster.png";

        return $"https://image.tmdb.org/t/p/{tamanho}{posterPath}";
    }
}