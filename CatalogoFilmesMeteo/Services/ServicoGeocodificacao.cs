using System.Text.Json;
using CatalogoFilmesMeteo.Models.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace CatalogoFilmesMeteo.Services;

/// <summary>
/// Serviço de geocodificação usando a API Nominatim (OpenStreetMap) para converter nomes de cidades em coordenadas.
/// </summary>
public class ServicoGeocodificacao : IServicoGeocodificacao
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ServicoGeocodificacao> _logger;
    private const string BaseUrl = "https://nominatim.openstreetmap.org/search";

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

        var cidadeNormalizada = cidade.Trim();
        var cacheKey = $"geocode_nominatim_{cidadeNormalizada.ToLowerInvariant()}";
        
        if (_cache.TryGetValue(cacheKey, out (decimal lat, decimal lon)? cached))
        {
            _logger.LogInformation("Cache hit para geocodificação Nominatim - Cidade: {Cidade}", cidade);
            return cached;
        }

        // Nominatim requer User-Agent obrigatório
        var url = $"{BaseUrl}?q={Uri.EscapeDataString(cidadeNormalizada)}&format=json&limit=1&addressdetails=1";
        
        try
        {
            _logger.LogInformation("Buscando coordenadas no Nominatim para cidade: {Cidade} - URL: {Url}", cidade, url);
            
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            
            // Nominatim requer User-Agent obrigatório (política de uso)
            client.DefaultRequestHeaders.Add("User-Agent", "CatalogoFilmesMeteo/1.0 (ASP.NET Core)");
            
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation("Resposta Nominatim - Status: {StatusCode}, Tamanho: {Tamanho} bytes", 
                response.StatusCode, content.Length);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Erro na API Nominatim - Status: {StatusCode}, Resposta: {Resposta}", 
                    response.StatusCode, content.Length > 500 ? content.Substring(0, 500) : content);
                return null;
            }

            // Remove PropertyNamingPolicy.CamelCase pois os campos já estão mapeados com [JsonPropertyName]
            // e o Nominatim retorna os campos exatamente como definidos (lat, lon)
            var resultados = JsonSerializer.Deserialize<List<ResultadoGeocodificacao>>(content, 
                new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true
                    // Não usar PropertyNamingPolicy aqui - os atributos [JsonPropertyName] já fazem o mapeamento
                });
            
            if (resultados == null || resultados.Count == 0)
            {
                _logger.LogWarning("Cidade não encontrada no Nominatim: {Cidade}", cidade);
                return null;
            }

            var primeiroResultado = resultados[0];
            
            // Log dos valores brutos recebidos da API antes da conversão
            _logger.LogDebug("Valores brutos do Nominatim - LatitudeStr: '{LatitudeStr}', LongitudeStr: '{LongitudeStr}'", 
                primeiroResultado.LatitudeStr, primeiroResultado.LongitudeStr);
            
            // Validar se as coordenadas foram parseadas corretamente
            if (!primeiroResultado.Latitude.HasValue || !primeiroResultado.Longitude.HasValue)
            {
                _logger.LogError("Erro ao fazer parse das coordenadas do Nominatim - LatitudeStr: '{LatitudeStr}', LongitudeStr: '{LongitudeStr}'", 
                    primeiroResultado.LatitudeStr, primeiroResultado.LongitudeStr);
                return null;
            }
            
            // Arredondar para 6 casas decimais para garantir precisão consistente
            var lat = Math.Round(primeiroResultado.Latitude.Value, 6);
            var lon = Math.Round(primeiroResultado.Longitude.Value, 6);
            
            // Criar tupla com nomes em minúscula para corresponder à interface
            var coordenadas = (latitude: lat, longitude: lon);
            
            _logger.LogInformation("Coordenadas encontradas no Nominatim para {Cidade}: Lat={Latitude}, Lon={Longitude}, Nome: {Nome}", 
                cidade, coordenadas.latitude, coordenadas.longitude, primeiroResultado.NomeExibicao);

            // Cache por 24 horas (coordenadas não mudam)
            _cache.Set(cacheKey, coordenadas, TimeSpan.FromHours(24));
            
            return coordenadas;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout ao buscar coordenadas no Nominatim para cidade: {Cidade}", cidade);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao deserializar resposta do Nominatim para cidade: {Cidade}", cidade);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar coordenadas no Nominatim para cidade: {Cidade}", cidade);
            return null;
        }
    }
}

