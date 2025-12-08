using System.Data;
using CatalogoFilmesMeteo.Models;
using Microsoft.Data.Sqlite;

namespace CatalogoFilmesMeteo.Repositories;

/// <summary>
/// Implementação do repositório de filmes usando SQLite.
/// 
/// IMPORTANTE: Este repositório NÃO usa Entity Framework ou Migrations.
/// A tabela é criada automaticamente na primeira execução usando SQL direto.
/// </summary>
public class FilmeRepository : IFilmeRepository
{
    private readonly string _connectionString;
    private readonly ILogger<FilmeRepository> _logger;

    public FilmeRepository(IConfiguration configuration, ILogger<FilmeRepository> logger)
    {
        // Pega a connection string do appsettings.json ou usa uma padrão
        // SQLite usa um arquivo .db como banco de dados (muito simples!)
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=Data/CatalogoFilmes.db";
        _logger = logger;
        
        // Garante que a pasta Data existe
        var dataDir = Path.GetDirectoryName(_connectionString.Replace("Data Source=", ""));
        if (!string.IsNullOrEmpty(dataDir) && !Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
            _logger.LogInformation("Pasta Data criada: {DataDir}", dataDir);
        }
        
        // Cria a tabela automaticamente se não existir
        // Isso evita ter que rodar migrations manualmente
        CriarTabelaSeNaoExistir();
    }

    /// <summary>
    /// Cria a tabela Filmes se ela não existir.
    /// Isso é executado automaticamente na primeira vez que o repositório é instanciado.
    /// </summary>
    private void CriarTabelaSeNaoExistir()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            
            // SQLite não tem tipo DateTime nativo, então usamos TEXT
            // e convertemos quando lemos/escrevemos
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS Filmes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TmdbId INTEGER NOT NULL UNIQUE,
                    Titulo TEXT NOT NULL,
                    TituloOriginal TEXT NOT NULL,
                    Sinopse TEXT,
                    DataLancamento TEXT,
                    Genero TEXT,
                    PosterPath TEXT,
                    Lingua TEXT,
                    Duracao INTEGER,
                    NotaMedia REAL,
                    ElencoPrincipal TEXT,
                    CidadeReferencia TEXT,
                    Latitude REAL,
                    Longitude REAL,
                    DataCriacao TEXT NOT NULL,
                    DataAtualizacao TEXT NOT NULL
                );
                
                CREATE INDEX IF NOT EXISTS idx_filmes_tmdbid ON Filmes(TmdbId);
            ";
            
            using var command = new SqliteCommand(createTableSql, connection);
            command.ExecuteNonQuery();
            
            _logger.LogInformation("Tabela Filmes verificada/criada com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar tabela Filmes");
            throw;
        }
    }

    public async Task<Filme> CreateAsync(Filme filme)
    {
        // Validação básica
        if (filme == null)
            throw new ArgumentNullException(nameof(filme));
        
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            // SQL para inserir um novo filme
            // Note que não inserimos o Id - ele é gerado automaticamente (AUTOINCREMENT)
            var sql = @"
                INSERT INTO Filmes (
                    TmdbId, Titulo, TituloOriginal, Sinopse, DataLancamento,
                    Genero, PosterPath, Lingua, Duracao, NotaMedia,
                    ElencoPrincipal, CidadeReferencia, Latitude, Longitude,
                    DataCriacao, DataAtualizacao
                ) VALUES (
                    @TmdbId, @Titulo, @TituloOriginal, @Sinopse, @DataLancamento,
                    @Genero, @PosterPath, @Lingua, @Duracao, @NotaMedia,
                    @ElencoPrincipal, @CidadeReferencia, @Latitude, @Longitude,
                    @DataCriacao, @DataAtualizacao
                );
                SELECT last_insert_rowid();";
            
            using var command = new SqliteCommand(sql, connection);
            
            // Adiciona os parâmetros (evita SQL injection)
            AdicionarParametrosFilme(command, filme);
            
            // Executa e pega o ID gerado
            var novoId = Convert.ToInt32(await command.ExecuteScalarAsync());
            
            // Busca o filme criado para retornar completo
            var filmeCriado = await GetByIdAsync(novoId);
            
            _logger.LogInformation("Filme criado com sucesso: Id={Id}, Titulo={Titulo}", novoId, filme.Titulo);
            
            return filmeCriado!;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // UNIQUE constraint failed
        {
            _logger.LogWarning("Tentativa de criar filme duplicado: TmdbId={TmdbId}", filme.TmdbId);
            throw new InvalidOperationException($"Filme com TmdbId {filme.TmdbId} já existe", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar filme: {Titulo}", filme.Titulo);
            throw;
        }
    }

    public async Task<Filme?> GetByIdAsync(int id)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            var sql = "SELECT * FROM Filmes WHERE Id = @Id";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return MapearFilmeDoBanco(reader);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar filme por Id: {Id}", id);
            throw;
        }
    }

    public async Task<Filme?> GetByTmdbIdAsync(int tmdbId)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            var sql = "SELECT * FROM Filmes WHERE TmdbId = @TmdbId";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@TmdbId", tmdbId);
            
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return MapearFilmeDoBanco(reader);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar filme por TmdbId: {TmdbId}", tmdbId);
            throw;
        }
    }

    public async Task<IEnumerable<Filme>> ListAsync()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            var sql = "SELECT * FROM Filmes ORDER BY Titulo";
            using var command = new SqliteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            var filmes = new List<Filme>();
            
            while (await reader.ReadAsync())
            {
                filmes.Add(MapearFilmeDoBanco(reader));
            }
            
            _logger.LogInformation("Listados {Count} filmes", filmes.Count);
            
            return filmes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar filmes");
            throw;
        }
    }

    public async Task<Filme> UpdateAsync(Filme filme)
    {
        if (filme == null)
            throw new ArgumentNullException(nameof(filme));
        
        if (filme.Id <= 0)
            throw new ArgumentException("Id do filme deve ser maior que zero", nameof(filme));
        
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            // Atualiza todos os campos (exceto Id e DataCriacao)
            var sql = @"
                UPDATE Filmes SET
                    TmdbId = @TmdbId,
                    Titulo = @Titulo,
                    TituloOriginal = @TituloOriginal,
                    Sinopse = @Sinopse,
                    DataLancamento = @DataLancamento,
                    Genero = @Genero,
                    PosterPath = @PosterPath,
                    Lingua = @Lingua,
                    Duracao = @Duracao,
                    NotaMedia = @NotaMedia,
                    ElencoPrincipal = @ElencoPrincipal,
                    CidadeReferencia = @CidadeReferencia,
                    Latitude = @Latitude,
                    Longitude = @Longitude,
                    DataAtualizacao = @DataAtualizacao
                WHERE Id = @Id";
            
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", filme.Id);
            AdicionarParametrosFilme(command, filme);
            command.Parameters.AddWithValue("@DataAtualizacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            
            var linhasAfetadas = await command.ExecuteNonQueryAsync();
            
            if (linhasAfetadas == 0)
            {
                throw new InvalidOperationException($"Filme com Id {filme.Id} não encontrado");
            }
            
            _logger.LogInformation("Filme atualizado: Id={Id}", filme.Id);
            
            // Retorna o filme atualizado
            return (await GetByIdAsync(filme.Id))!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar filme: Id={Id}", filme.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            
            var sql = "DELETE FROM Filmes WHERE Id = @Id";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            
            var linhasAfetadas = await command.ExecuteNonQueryAsync();
            
            if (linhasAfetadas > 0)
            {
                _logger.LogInformation("Filme removido: Id={Id}", id);
                return true;
            }
            
            _logger.LogWarning("Filme não encontrado para remoção: Id={Id}", id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover filme: Id={Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Adiciona os parâmetros do filme ao comando SQL.
    /// Isso evita SQL injection e facilita a manutenção.
    /// </summary>
    private void AdicionarParametrosFilme(SqliteCommand command, Filme filme)
    {
        // SQLite não tem DateTime nativo, então convertemos para string
        // Formato: "yyyy-MM-dd HH:mm:ss" (padrão ISO)
        command.Parameters.AddWithValue("@TmdbId", filme.TmdbId);
        command.Parameters.AddWithValue("@Titulo", filme.Titulo);
        command.Parameters.AddWithValue("@TituloOriginal", filme.TituloOriginal);
        command.Parameters.AddWithValue("@Sinopse", filme.Sinopse ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DataLancamento", 
            filme.DataLancamento?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Genero", filme.Genero ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PosterPath", filme.PosterPath ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Lingua", filme.Lingua ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Duracao", filme.Duracao ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@NotaMedia", filme.NotaMedia ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ElencoPrincipal", filme.ElencoPrincipal ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CidadeReferencia", filme.CidadeReferencia ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Latitude", filme.Latitude ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Longitude", filme.Longitude ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DataCriacao", 
            filme.DataCriacao.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@DataAtualizacao", 
            filme.DataAtualizacao.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    /// <summary>
    /// Converte uma linha do banco de dados (DataReader) em um objeto Filme.
    /// Faz a conversão de TEXT para DateTime onde necessário.
    /// </summary>
    private Filme MapearFilmeDoBanco(SqliteDataReader reader)
    {
        return new Filme
        {
            Id = reader.GetInt32("Id"),
            TmdbId = reader.GetInt32("TmdbId"),
            Titulo = reader.GetString("Titulo"),
            TituloOriginal = reader.GetString("TituloOriginal"),
            Sinopse = reader.IsDBNull("Sinopse") ? null : reader.GetString("Sinopse"),
            // Converte TEXT de volta para DateTime
            DataLancamento = reader.IsDBNull("DataLancamento") 
                ? null 
                : DateTime.Parse(reader.GetString("DataLancamento")),
            Genero = reader.IsDBNull("Genero") ? null : reader.GetString("Genero"),
            PosterPath = reader.IsDBNull("PosterPath") ? null : reader.GetString("PosterPath"),
            Lingua = reader.IsDBNull("Lingua") ? null : reader.GetString("Lingua"),
            Duracao = reader.IsDBNull("Duracao") ? null : reader.GetInt32("Duracao"),
            NotaMedia = reader.IsDBNull("NotaMedia") ? null : reader.GetDecimal("NotaMedia"),
            ElencoPrincipal = reader.IsDBNull("ElencoPrincipal") ? null : reader.GetString("ElencoPrincipal"),
            CidadeReferencia = reader.IsDBNull("CidadeReferencia") ? null : reader.GetString("CidadeReferencia"),
            Latitude = reader.IsDBNull("Latitude") ? null : reader.GetDecimal("Latitude"),
            Longitude = reader.IsDBNull("Longitude") ? null : reader.GetDecimal("Longitude"),
            // Converte TEXT de volta para DateTime
            DataCriacao = DateTime.Parse(reader.GetString("DataCriacao")),
            DataAtualizacao = DateTime.Parse(reader.GetString("DataAtualizacao"))
        };
    }
}
