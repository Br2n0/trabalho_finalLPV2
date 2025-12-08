using CatalogoFilmesMeteo.Models;
using CatalogoFilmesMeteo.Models.DTOs;
using CatalogoFilmesMeteo.Repositories;

namespace CatalogoFilmesMeteo.Services;

/// <summary>
/// Serviço responsável por importar filmes do TMDb para o banco de dados local.
/// 
/// Este serviço faz a "ponte" entre os dados que vêm da API do TMDb (DTOs)
/// e o modelo que salvamos no nosso banco (Filme).
/// </summary>
public class FilmeImportService : IFilmeImportService
{
    private readonly IFilmeRepository _filmeRepository;
    private readonly ILogger<FilmeImportService> _logger;

    public FilmeImportService(
        IFilmeRepository filmeRepository,
        ILogger<FilmeImportService> logger)
    {
        _filmeRepository = filmeRepository;
        _logger = logger;
    }

    public async Task<Filme> ImportarFilmeAsync(DetalhesFilmeTmdb detalhesTmdb)
    {
        if (detalhesTmdb == null)
            throw new ArgumentNullException(nameof(detalhesTmdb));

        try
        {
            _logger.LogInformation("Iniciando importação do filme: {Titulo} (TmdbId: {Id})", 
                detalhesTmdb.Titulo, detalhesTmdb.Id);

            // Verifica se o filme já existe no banco (evita duplicatas)
            var filmeExistente = await _filmeRepository.GetByTmdbIdAsync(detalhesTmdb.Id);
            if (filmeExistente != null)
            {
                _logger.LogWarning("Filme já existe no banco: Id={Id}, TmdbId={TmdbId}", 
                    filmeExistente.Id, detalhesTmdb.Id);
                throw new InvalidOperationException(
                    $"Filme '{detalhesTmdb.Titulo}' já foi importado anteriormente.");
            }

            // Mapeia o DTO do TMDb para o modelo local
            var filme = MapearDetalhesTmdbParaFilme(detalhesTmdb);

            // Salva no banco de dados
            var filmeSalvo = await _filmeRepository.CreateAsync(filme);

            _logger.LogInformation("Filme importado com sucesso: Id={Id}, Titulo={Titulo}", 
                filmeSalvo.Id, filmeSalvo.Titulo);

            return filmeSalvo;
        }
        catch (InvalidOperationException)
        {
            // Re-lança exceções de negócio (como filme duplicado)
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao importar filme: {Titulo}", detalhesTmdb.Titulo);
            throw;
        }
    }

    /// <summary>
    /// Converte os dados do TMDb (DetalhesFilmeTmdb) para o modelo local (Filme).
    /// 
    /// Este método faz o "mapeamento" entre os campos da API e os campos do nosso banco.
    /// Alguns campos precisam de transformação (ex: lista de gêneros vira string).
    /// </summary>
    private Filme MapearDetalhesTmdbParaFilme(DetalhesFilmeTmdb detalhesTmdb)
    {
        var filme = new Filme
        {
            // Campos diretos (mesmo nome e tipo)
            TmdbId = detalhesTmdb.Id,
            Titulo = detalhesTmdb.Titulo,
            TituloOriginal = detalhesTmdb.TituloOriginal,
            Sinopse = detalhesTmdb.Sinopse,
            PosterPath = detalhesTmdb.CaminhoPoster,
            Duracao = detalhesTmdb.Duracao,
            NotaMedia = detalhesTmdb.MediaVotos > 0 ? detalhesTmdb.MediaVotos : null,
            
            // Data de lançamento: vem como string "yyyy-MM-dd", converte para DateTime
            DataLancamento = !string.IsNullOrEmpty(detalhesTmdb.DataLancamento)
                ? DateTime.TryParse(detalhesTmdb.DataLancamento, out var data) ? data : null
                : null,
            
            // Gêneros: vem como lista, pegamos o primeiro ou concatenamos
            Genero = detalhesTmdb.Generos?.Any() == true
                ? string.Join(", ", detalhesTmdb.Generos.Select(g => g.Nome))
                : null,
            
            // Idioma: vem como lista, pegamos o primeiro ou concatenamos
            Lingua = detalhesTmdb.Idiomas?.Any() == true
                ? string.Join(", ", detalhesTmdb.Idiomas.Select(i => i.Nome))
                : null,
            
            // Elenco principal: pega os primeiros 5 atores do cast
            ElencoPrincipal = detalhesTmdb.Creditos?.Elenco?
                .OrderBy(e => e.Ordem)
                .Take(5)
                .Select(e => $"{e.Nome} ({e.Personagem})")
                .ToList() is { Count: > 0 } elenco
                ? string.Join(", ", elenco)
                : null,
            
            // Coordenadas: deixamos null na importação
            // O usuário pode preencher depois manualmente
            Latitude = null,
            Longitude = null,
            CidadeReferencia = null,
            
            // Timestamps: data atual
            DataCriacao = DateTime.Now,
            DataAtualizacao = DateTime.Now
        };

        _logger.LogDebug("Mapeamento concluído: {Titulo} -> {Genero}, {Lingua}, {ElencoCount} atores", 
            filme.Titulo, filme.Genero, filme.Lingua, 
            detalhesTmdb.Creditos?.Elenco?.Count ?? 0);

        return filme;
    }
}
