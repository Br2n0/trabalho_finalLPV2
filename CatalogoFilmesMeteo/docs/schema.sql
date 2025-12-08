-- Script SQL para criação manual da tabela Filmes
-- Este script pode ser executado manualmente antes de rodar a aplicação
-- OU a aplicação criará a tabela automaticamente na primeira execução

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

-- Índice para melhorar performance nas buscas por TmdbId
CREATE INDEX IF NOT EXISTS idx_filmes_tmdbid ON Filmes(TmdbId);
