using System.Text.Json;
using CatalogoFilmesMeteo.Exceptions;
using CatalogoFilmesMeteo.Models.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace CatalogoFilmesMeteo.Services;

public class ServicoApiTempo : IServicoApiTempo
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ServicoApiTempo> _logger;
    private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";

    public ServicoApiTempo(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<ServicoApiTempo> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<RespostaPrevisaoTempo> ObterPrevisaoDiariaAsync(decimal latitude, decimal longitude)
    {
        var cacheKey = $"weather_{latitude}_{longitude}";
        
        if (_cache.TryGetValue(cacheKey, out RespostaPrevisaoTempo? cached))
        {
            _logger.LogInformation("Cache hit para previsão do tempo - Lat: {Latitude}, Lon: {Longitude}", latitude, longitude);
            return cached!;
        }

        var url = $"{BaseUrl}?latitude={latitude}&longitude={longitude}&daily=temperature_2m_max,temperature_2m_min&timezone=auto";
        
        try
        {
            _logger.LogInformation("Obtendo previsão do tempo - URL: {Url}, Parâmetros: lat={Latitude}, lon={Longitude}", url, latitude, longitude);
            
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("Resposta Open-Meteo - Status: {StatusCode}, Data: {Data}", response.StatusCode, DateTime.Now);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Erro na API Open-Meteo - Status: {StatusCode}, Resposta: {Resposta}", response.StatusCode, content);
                throw new ExcecaoApiTempo($"Erro ao obter previsão do tempo: {response.StatusCode}", (int)response.StatusCode);
            }

            var resultado = JsonSerializer.Deserialize<RespostaPrevisaoTempo>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (resultado == null)
                throw new ExcecaoApiTempo("Resposta inválida da API Open-Meteo");

            _cache.Set(cacheKey, resultado, TimeSpan.FromMinutes(10));
            return resultado;
        }
        catch (ExcecaoApiTempo)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter previsão do tempo - URL: {Url}", url);
            throw new ExcecaoApiTempo($"Erro ao obter previsão do tempo: {ex.Message}", ex);
        }
    }
}

