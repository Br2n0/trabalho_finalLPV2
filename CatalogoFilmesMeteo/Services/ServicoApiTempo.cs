using System.Text.Json;
using CatalogoFilmesMeteo.Exceptions;
using CatalogoFilmesMeteo.Models.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace CatalogoFilmesMeteo.Services;

public class ServicoApiTempo : IServicoApiTempo
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IServicoGeocodificacao _geocodificacao;
    private readonly ILogger<ServicoApiTempo> _logger;
    private const string BaseUrl = "https://api.open-meteo.com/v1/forecast";

    public ServicoApiTempo(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IServicoGeocodificacao geocodificacao,
        ILogger<ServicoApiTempo> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _geocodificacao = geocodificacao;
        _logger = logger;
    }
    
    public async Task<RespostaPrevisaoTempo?> ObterPrevisaoPorCidadeAsync(string cidade)
    {
        if (string.IsNullOrWhiteSpace(cidade))
            return null;

        try
        {
            var coordenadas = await _geocodificacao.ObterCoordenadasAsync(cidade);
            
            if (!coordenadas.HasValue)
            {
                _logger.LogWarning("Não foi possível obter coordenadas para a cidade: {Cidade}", cidade);
                return null;
            }

            return await ObterPrevisaoDiariaAsync(coordenadas.Value.latitude, coordenadas.Value.longitude);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter previsão do tempo para cidade: {Cidade}", cidade);
            return null;
        }
    }

    public async Task<RespostaPrevisaoTempo> ObterPrevisaoDiariaAsync(decimal latitude, decimal longitude)
    {
        var cacheKey = $"weather_{latitude}_{longitude}";
        
        if (_cache.TryGetValue(cacheKey, out RespostaPrevisaoTempo? cached))
        {
            _logger.LogInformation("Cache hit para previsão do tempo - Lat: {Latitude}, Lon: {Longitude}", latitude, longitude);
            return cached!;
        }

        // Formata os números com ponto como separador decimal (padrão internacional)
        var latStr = latitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
        var lonStr = longitude.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
        
        var url = $"{BaseUrl}?latitude={latStr}&longitude={lonStr}&daily=temperature_2m_max,temperature_2m_min&timezone=auto";
        
        try
        {
            _logger.LogInformation("Obtendo previsão do tempo - URL: {Url}, Parâmetros: lat={Latitude}, lon={Longitude}", url, latitude, longitude);
            
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30); // Adiciona timeout
            
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
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout ao obter previsão do tempo - URL: {Url}", url);
            throw new ExcecaoApiTempo("Timeout ao obter previsão do tempo. Tente novamente mais tarde.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter previsão do tempo - URL: {Url}", url);
            throw new ExcecaoApiTempo($"Erro ao obter previsão do tempo: {ex.Message}", ex);
        }
    }
}

