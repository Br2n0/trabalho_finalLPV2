using System.Text;
using CatalogoFilmesMeteo.Models;
using CsvHelper;
using CsvHelper.Configuration;
using OfficeOpenXml;

namespace CatalogoFilmesMeteo.Services;

/// <summary>
/// Serviço responsável por exportar filmes para formatos CSV e Excel.
/// 
/// Este serviço pega os dados do banco e converte para arquivos que podem ser
/// baixados pelo usuário.
/// </summary>
public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger;
        // EPPlus requer licença para uso comercial, mas é free para uso não comercial
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public async Task<byte[]> ExportToCsvAsync(IEnumerable<Filme> filmes)
    {
        try
        {
            var listaFilmes = filmes.ToList();
            _logger.LogInformation("Iniciando exportação CSV de {Count} filmes", listaFilmes.Count);

            // Usa MemoryStream para criar o arquivo em memória
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            
            // Configuração do CsvHelper
            var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                // Usa vírgula como separador (padrão CSV)
                Delimiter = ",",
                // Adiciona BOM para Excel reconhecer UTF-8 corretamente
                HasHeaderRecord = true
            };

            using var csv = new CsvWriter(writer, config);
            
            //colunas do csv
            csv.WriteField("Id");
            csv.WriteField("TmdbId");
            csv.WriteField("Titulo");
            csv.WriteField("TituloOriginal");
            csv.WriteField("Sinopse");
            csv.WriteField("DataLancamento");
            csv.WriteField("Genero");
            csv.WriteField("PosterPath");
            csv.WriteField("Lingua");
            csv.WriteField("Duracao");
            csv.WriteField("NotaMedia");
            csv.WriteField("ElencoPrincipal");
            csv.WriteField("CidadeReferencia");
            csv.WriteField("Latitude");
            csv.WriteField("Longitude");
            csv.WriteField("DataCriacao");
            csv.WriteField("DataAtualizacao");
            await csv.NextRecordAsync();

            // Escreve os dados de cada filme
            foreach (var filme in listaFilmes)
            {
                csv.WriteField(filme.Id);
                csv.WriteField(filme.TmdbId);
                csv.WriteField(filme.Titulo);
                csv.WriteField(filme.TituloOriginal);
                csv.WriteField(filme.Sinopse ?? "");
                csv.WriteField(filme.DataLancamento?.ToString("yyyy-MM-dd") ?? "");
                csv.WriteField(filme.Genero ?? "");
                csv.WriteField(filme.PosterPath ?? "");
                csv.WriteField(filme.Lingua ?? "");
                csv.WriteField(filme.Duracao?.ToString() ?? "");
                csv.WriteField(filme.NotaMedia?.ToString() ?? "");
                csv.WriteField(filme.ElencoPrincipal ?? "");
                csv.WriteField(filme.CidadeReferencia ?? "");
                csv.WriteField(filme.Latitude?.ToString() ?? "");
                csv.WriteField(filme.Longitude?.ToString() ?? "");
                csv.WriteField(filme.DataCriacao.ToString("yyyy-MM-dd HH:mm:ss"));
                csv.WriteField(filme.DataAtualizacao.ToString("yyyy-MM-dd HH:mm:ss"));
                await csv.NextRecordAsync();
            }

            await writer.FlushAsync();
            
            _logger.LogInformation("Exportação CSV concluída: {Count} filmes, {Size} bytes", 
                listaFilmes.Count, memoryStream.Length);

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar para CSV");
            throw;
        }
    }

    public async Task<byte[]> ExportToExcelAsync(IEnumerable<Filme> filmes)
    {
        try
        {
            var listaFilmes = filmes.ToList();
            _logger.LogInformation("Iniciando exportação Excel de {Count} filmes", listaFilmes.Count);

            // EPPlus cria arquivos Excel (.xlsx) em memória
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Filmes");

            // Define os cabeçalhos (primeira linha)
            var headers = new[]
            {
                "Id", "TmdbId", "Titulo", "TituloOriginal", "Sinopse",
                "DataLancamento", "Genero", "PosterPath", "Lingua", "Duracao",
                "NotaMedia", "ElencoPrincipal", "CidadeReferencia",
                "Latitude", "Longitude", "DataCriacao", "DataAtualizacao"
            };

            // Escreve cabeçalhos na primeira linha
            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = worksheet.Cells[1, col];
                cell.Value = headers[col - 1];
                // Formatação: negrito e fundo cinza
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Escreve os dados (começando na linha 2)
            int row = 2;
            foreach (var filme in listaFilmes)
            {
                worksheet.Cells[row, 1].Value = filme.Id;
                worksheet.Cells[row, 2].Value = filme.TmdbId;
                worksheet.Cells[row, 3].Value = filme.Titulo;
                worksheet.Cells[row, 4].Value = filme.TituloOriginal;
                worksheet.Cells[row, 5].Value = filme.Sinopse;
                worksheet.Cells[row, 6].Value = filme.DataLancamento?.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 7].Value = filme.Genero;
                worksheet.Cells[row, 8].Value = filme.PosterPath;
                worksheet.Cells[row, 9].Value = filme.Lingua;
                worksheet.Cells[row, 10].Value = filme.Duracao;
                worksheet.Cells[row, 11].Value = filme.NotaMedia;
                worksheet.Cells[row, 12].Value = filme.ElencoPrincipal;
                worksheet.Cells[row, 13].Value = filme.CidadeReferencia;
                worksheet.Cells[row, 14].Value = filme.Latitude;
                worksheet.Cells[row, 15].Value = filme.Longitude;
                worksheet.Cells[row, 16].Value = filme.DataCriacao.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cells[row, 17].Value = filme.DataAtualizacao.ToString("yyyy-MM-dd HH:mm:ss");
                row++;
            }

            // Ajusta a largura das colunas automaticamente
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // Retorna os bytes do arquivo Excel
            var bytes = await package.GetAsByteArrayAsync();
            
            _logger.LogInformation("Exportação Excel concluída: {Count} filmes, {Size} bytes", 
                listaFilmes.Count, bytes.Length);

            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao exportar para Excel");
            throw;
        }
    }
}
