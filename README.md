# trabalho_finalLPV2

Trabalho Final da disciplina de Linguagem de Programação Visual II

## Documento de Requisitos do Sistema (DRS) — Catálogo de Filmes + Previsão do Tempo

Integrações: TMDb + Open-Meteo + Nominatim API  
Plataforma: ASP.NET Core 9.0 (MVC) 
Equipes: Duplas  
Versão: 1.5  
Data: 26/11/2025

## 1. Configuração do Projeto

### Plataforma e Requisitos

- **Plataforma:** ASP.NET Core 9.0 (MVC) - [Download .NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Banco de Dados:** SQLite (criado automaticamente na primeira execução em `Data/CatalogoFilmes.db`)
- **Integrações:** TMDb API + Open-Meteo API + Nominatim API


### Como Executar o Projeto

1. Clone o repositório
2. Configure a API Key do TMDb (veja instruções abaixo)
3. Execute o projeto:
   ```bash
   dotnet run
   ```
   ou através do IDE (Visual Studio, Rider, etc.)

### API Key do TMDb

Para executar o projeto, é necessário configurar a API Key do TMDb. **Nunca commite segredos no repositório.**

#### Opção 1: Arquivo de configuração local (Recomendado)

1. Copie o arquivo `appsettings.Development.json.example` para `appsettings.Development.json`
2. Abra o arquivo `appsettings.Development.json` e adicione sua API Key no campo `TMDb:ApiKey`
3. O arquivo `appsettings.Development.json` está no `.gitignore` e não será commitado

**Exemplo:**
```json
{
  "TMDb": {
    "ApiKey": "sua-chave-aqui"
  }
}
```

#### Opção 2: Variável de ambiente

Configure a variável de ambiente `TMDB_API_KEY` com sua chave:

**Windows (PowerShell):**
```powershell
$env:TMDB_API_KEY="sua-chave-aqui"
```

**Windows (CMD):**
```cmd
set TMDB_API_KEY=sua-chave-aqui
```

**Linux/Mac:**
```bash
export TMDB_API_KEY="sua-chave-aqui"
```

#### Como obter uma API Key

1. Acesse https://www.themoviedb.org/
2. Crie uma conta gratuita
3. Vá em Configurações → API
4. Solicite uma API Key (aprovada imediatamente para contas gratuitas)
5. Use a API Key v3 (não a v4 Bearer Token)

### Banco de Dados

O projeto usa SQLite e o banco será criado automaticamente na primeira execução em `Data/CatalogoFilmes.db`.

## 2. Visão Geral

Desenvolver uma aplicação MVC em ASP.NET Core 9.0 que:

- Pesquise filmes no TMDb,
- Importe títulos selecionados para uma base local,
- Gerencie um catálogo de filmes (CRUD),
- Exiba detalhes enriquecidos (poster, sinopse, elenco, nota),
- Mostre a previsão do tempo da cidade associada ao filme utilizando a API Open-Meteo,
- Exporte o catálogo local para CSV/Excel.

O projeto também exige workflow Git profissional: branches individuais e PRs obrigatórios.

## 3. Escopo do Sistema

A aplicação deverá:

- Realizar buscas no TMDb.
- Importar dados selecionados do TMDb para armazenamento local.
- Gerenciar filmes (CRUD completo).
- Exibir poster via URL fornecida por /configuration do TMDb.
- Integrar previsão do tempo da cidade vinculada ao filme via Open-Meteo.
- Exibir previsão mínima e máxima diária.
- Exportar o catálogo local.
- Usar partial views reutilizáveis.
- Seguir rigorosamente o fluxo Git/PRs.

## 4. Requisitos Funcionais (RF)

### RF01 — Entidade Filme (obrigatório)

A entidade local deve conter:
Id, TmdbId, Titulo, TituloOriginal, Sinopse, DataLancamento, Genero, PosterPath, Lingua, Duracao, NotaMedia, ElencoPrincipal, CidadeReferencia, Latitude, Longitude, DataCriacao, DataAtualizacao.

### RF02 — Busca de filmes (server-side)

Deve consumir /search/movie do TMDb.

Resultados exibidos em página server-side, com paginação TMDb.

Deve permitir botão "Importar".

### RF03 — Importação de filme

Persistir filme localmente após mapear DTO → Model.

Coordenadas geográficas (lat/long) devem vir do TMDb (quando possível) ou inseridas manualmente pelo usuário.

### RF04 — Exibição de detalhes (TMDb)

Consultar /movie/{id} e /configuration quando necessário.

Montar URL final do poster com base_url + tamanho + poster_path.

Exibir poster e dados completos do filme.

### RF05 — Cliente TMDb desacoplado

Criar:

- ITmdbApiService (interface)
- TmdbApiService (implementação)

Métodos obrigatórios:

- SearchMoviesAsync
- GetMovieDetailsAsync
- GetMovieImagesAsync
- GetConfigurationAsync

### RF06 — Integração com previsão do tempo (Open-Meteo)

Criar:

- Interface IWeatherApiService
- Implementação WeatherApiService

Consumir o endpoint:

https://api.open-meteo.com/v1/forecast
    ?latitude={lat}
    &longitude={lon}
    &daily=temperature_2m_max,temperature_2m_min
    &timezone=auto

Exibir na tela de detalhes:

- Temperatura mínima do dia
- Temperatura máxima do dia
- Data da previsão

Caso o filme não tenha lat/long, exibir instrução para o usuário preencher.

### RF07 — Autenticação TMDb

Deve suportar API Key v3 ou Bearer Token v4.

Segredos não podem ser commitados.

(Open-Meteo não exige autenticação.)

### RF08 — Cache obrigatório

Usar IMemoryCache para armazenar:

- Configuração do TMDb (obrigatório)
- Buscas TMDb (TTL 5 min)
- Detalhes TMDb (TTL 10 min)
- Previsão do tempo por lat/long (TTL 10 min)

### RF09 — Tratamento de erros e logs

Registrar em log:

- Endpoint consultado
- Parâmetros enviados
- Status code
- Data/hora
- Mensagem de erro

Exibir mensagens amigáveis no frontend.

### RF10 — Partial Views

Criar e usar Partial Views reutilizáveis para:

- cards/listagens
- bloco de previsão do tempo

Deve haver reutilização em pelo menos duas telas.

### RF11 — Persistência local (sem migrations)

Persistência local sem migrations — ou seja:

- O banco pode ser criado manualmente pelo aluno,
- Ou o aluno pode usar um banco extremamente simples como SQLite com criação de tabelas manual,
- Ou outro mecanismo de persistência à escolha, DESDE QUE seja permanente e funcional (não pode ser InMemory).

Criar interface:

- IFilmeRepository

Criar implementação concreta.

Operações obrigatórias: Create, Read, Update, Delete, List, GetById.

### RF12 — Exportação (obrigatória)

Exportar catálogo para CSV e Excel via FileResult.

### RF13 — Paginação externa TMDb

Utilizar o parâmetro page da API.

Exibir exatamente os resultados e numeração retornados pela API.

## 5. Hard Requirements (Obrigatórios)

A entrega só será aceita se TODOS os itens estiverem presentes:

- ASP.NET Core 9.0 funcionando.
- CRUD completo de filmes.
- Integração TMDb: /search/movie, /movie/{id}, /movie/{id}/images, /configuration.
- Integração Open-Meteo funcional na tela de detalhes.
- Persistência local real (qualquer tecnologia) sem migrations.
- Partial Views reutilizáveis.
- Cache obrigatório via IMemoryCache (TMDb + Open-Meteo).
- Exportação CSV e Excel.
- Logs detalhados.
- Segredos TMDb fora do repositório.
- Workflow Git obrigatório (branches individuais + PRs).
- README completo.

## 6. Critérios de Aceitação

- Busca TMDb funcionando com paginação.
- Importação salva filme localmente com dados completos.
- Poster exibido corretamente via configuração TMDb.
- Previsão do tempo exibida corretamente com dados do Open-Meteo.
- Cache reduz chamadas (verificável via logs).
- Exportação gera arquivos CSV e Excel válidos.
- Partial Views reutilizadas.
- Branch main executa sem ajustes.
- Cada aluno possui 3 PRs aprovados ou mais.

## 7. Workflow Git / PRs (obrigatório)

### 7.1 — Branches Individuais

Cada aluno deve trabalhar apenas em sua própria branch, seguindo o padrão:

feature/<nome>-<descricao>
ou

feature/<matricula>-<descricao>

### 7.2 — Pull Requests (PRs)

Toda alteração deve ser integrada via PR para main.

Merge direto na main é proibido.

Cada PR deve conter:

- título claro
- descrição objetiva
- arquivos alterados
- o que foi implementado

### 7.3 — Mínimo obrigatório de PRs

Cada aluno deve ter no mínimo 3 PRs aprovados no repositório.

PRs aceitos: features, correções, documentação, refatorações.
PRs que NÃO contam: vazios, triviais ou automáticos.

### 7.4 — Entrega final

A branch main será usada como validação final. Ela deve:

- compilar
- rodar
- não conter segredos
- ter persistência funcionando
- estar estável e limpa

## 8. Entregáveis

- Repositório Git com branches individuais.
- PRs (mínimo 3 por aluno).
- Código ASP.NET Core 9.0 completo.
- Persistência local funcional.
- README com instruções.
