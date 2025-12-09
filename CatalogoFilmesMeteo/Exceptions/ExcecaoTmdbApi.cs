namespace CatalogoFilmesMeteo.Exceptions;

public class ExcecaoTmdbApi : Exception
{
    public int? CodigoStatus { get; }

    public ExcecaoTmdbApi(string mensagem) : base(mensagem)
    {
    }

    public ExcecaoTmdbApi(string mensagem, int codigoStatus) : base(mensagem)
    {
        CodigoStatus = codigoStatus;
    }

    public ExcecaoTmdbApi(string mensagem, Exception excecaoInterna) : base(mensagem, excecaoInterna)
    {
    }

    public ExcecaoTmdbApi(string mensagem, int codigoStatus, Exception excecaoInterna)
        : base(mensagem, excecaoInterna)
    {
        CodigoStatus = codigoStatus;
    }
}