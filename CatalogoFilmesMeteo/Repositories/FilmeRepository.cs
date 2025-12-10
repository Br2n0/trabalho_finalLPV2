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

        CriarTabelaSeNaoExistir();
    }

    private void CriarTabelaSeNaoExistir()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

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
        if (filme == null)
            throw new ArgumentNullException(nameof(filme));

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

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
            AdicionarParametrosFilme(command, filme);

            var novoId = Convert.ToInt32(await command.ExecuteScalarAsync());
            var filmeCriado = await GetByIdAsync(novoId);

            _logger.LogInformation("Filme criado com sucesso: Id={Id}, Titulo={Titulo}", novoId, filme.Titulo);

            return filmeCriado!;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
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

            var linhasAfetadas = await command.ExecuteNonQueryAsync();

            if (linhasAfetadas == 0)
            {
                throw new InvalidOperationException($"Filme com Id {filme.Id} não encontrado");
            }

            _logger.LogInformation("Filme atualizado: Id={Id}", filme.Id);

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

    private void AdicionarParametrosFilme(SqliteCommand command, Filme filme)
    {
        command.Parameters.AddWithValue("@TmdbId", filme.TmdbId);
        command.Parameters.AddWithValue("@Titulo", filme.Titulo);
        command.Parameters.AddWithValue("@TituloOriginal", filme.TituloOriginal);
        command.Parameters.AddWithValue("@Sinopse", filme.Sinopse ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DataLancamento",
            filme.DataLancamento?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
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
            filme.DataCriacao.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@DataAtualizacao",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
    }

    private Filme MapearFilmeDoBanco(SqliteDataReader reader)
    {
        return new Filme
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            TmdbId = reader.GetInt32(reader.GetOrdinal("TmdbId")),
            Titulo = reader.GetString(reader.GetOrdinal("Titulo")),
            TituloOriginal = reader.GetString(reader.GetOrdinal("TituloOriginal")),
            Sinopse = reader.IsDBNull(reader.GetOrdinal("Sinopse"))
                ? null
                : reader.GetString(reader.GetOrdinal("Sinopse")),
            DataLancamento = reader.IsDBNull(reader.GetOrdinal("DataLancamento"))
                ? null
                : DateTime.Parse(reader.GetString(reader.GetOrdinal("DataLancamento"))),
            Genero = reader.IsDBNull(reader.GetOrdinal("Genero"))
                ? null
                : reader.GetString(reader.GetOrdinal("Genero")),
            PosterPath = reader.IsDBNull(reader.GetOrdinal("PosterPath"))
                ? null
                : reader.GetString(reader.GetOrdinal("PosterPath")),
            Lingua = reader.IsDBNull(reader.GetOrdinal("Lingua"))
                ? null
                : reader.GetString(reader.GetOrdinal("Lingua")),
            Duracao = reader.IsDBNull(reader.GetOrdinal("Duracao"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("Duracao")),
            NotaMedia = reader.IsDBNull(reader.GetOrdinal("NotaMedia"))
                ? null
                : reader.GetDecimal(reader.GetOrdinal("NotaMedia")),
            ElencoPrincipal = reader.IsDBNull(reader.GetOrdinal("ElencoPrincipal"))
                ? null
                : reader.GetString(reader.GetOrdinal("ElencoPrincipal")),
            CidadeReferencia = reader.IsDBNull(reader.GetOrdinal("CidadeReferencia"))
                ? null
                : reader.GetString(reader.GetOrdinal("CidadeReferencia")),
            Latitude = reader.IsDBNull(reader.GetOrdinal("Latitude"))
                ? null
                : reader.GetDecimal(reader.GetOrdinal("Latitude")),
            Longitude = reader.IsDBNull(reader.GetOrdinal("Longitude"))
                ? null
                : reader.GetDecimal(reader.GetOrdinal("Longitude")),
            DataCriacao = DateTime.Parse(reader.GetString(reader.GetOrdinal("DataCriacao"))),
            DataAtualizacao = DateTime.Parse(reader.GetString(reader.GetOrdinal("DataAtualizacao")))
        };
    }
}