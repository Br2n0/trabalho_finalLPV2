using System.Text.Json;
using CatalogoFilmesMeteo.Models.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace CatalogoFilmesMeteo.Services;

/// <summary>
/// Serviço de geocodificação usando a API Open-Meteo para converter nomes de cidades em coordenadas.
/// </summary>
public class ServicoGeocodificacao : IServicoGeocodificacao
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ServicoGeocodificacao> _logger;
    private const string BaseUrl = "https://geocoding-api.open-meteo.com/v1/search";

    public ServicoGeocodificacao(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<ServicoGeocodificacao> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<(decimal latitude, decimal longitude)?> ObterCoordenadasAsync(string cidade)
    {
        if (string.IsNullOrWhiteSpace(cidade))
            return null;

        var cacheKey = $"geocode_{cidade.ToLowerInvariant()}";
        
        if (_cache.TryGetValue(cacheKey, out (decimal lat, decimal lon)? cached))
        {
            _logger.LogInformation("Cache hit para geocodificação - Cidade: {Cidade}", cidade);
            return cached;
        }

        var url = $"{BaseUrl}?name={Uri.EscapeDataString(cidade)}&count=1&language=pt";
        
        try
        {
            _logger.LogInformation("Buscando coordenadas para cidade: {Cidade} - URL: {Url}", cidade, url);
            
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("Resposta geocodificação - Status: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Erro na API de geocodificação - Status: {StatusCode}, Resposta: {Resposta}", 
                    response.StatusCode, content);
                return null;
            }

            var resultado = JsonSerializer.Deserialize<RespostaGeocodificacao>(content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (resultado?.Resultados == null || resultado.Resultados.Count == 0)
            {
                _logger.LogWarning("Cidade não encontrada: {Cidade}", cidade);
                return null;
            }

            var primeiroResultado = resultado.Resultados[0];
            var coordenadas = (primeiroResultado.Latitude, primeiroResultado.Longitude);
            
            _logger.LogInformation("Coordenadas encontradas para {Cidade}: Lat={Latitude}, Lon={Longitude}", 
                cidade, primeiroResultado.Latitude, primeiroResultado.Longitude);

            // Cache por 24 horas (coordenadas não mudam)
            _cache.Set(cacheKey, coordenadas, TimeSpan.FromHours(24));
            
            return coordenadas;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout ao buscar coordenadas para cidade: {Cidade}", cidade);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar coordenadas para cidade: {Cidade}", cidade);
            return null;
        }
    }
}

