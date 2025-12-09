namespace CatalogoFilmesMeteo.Exceptions;

public class ExcecaoApiTempo : Exception
{
    public int? CodigoStatus { get; }

    public ExcecaoApiTempo(string mensagem) : base(mensagem)
    {
    }

    public ExcecaoApiTempo(string mensagem, int codigoStatus) : base(mensagem)
    {
        CodigoStatus = codigoStatus;
    }

    public ExcecaoApiTempo(string mensagem, Exception excecaoInterna) : base(mensagem, excecaoInterna)
    {
    }

    public ExcecaoApiTempo(string mensagem, int codigoStatus, Exception excecaoInterna)
        : base(mensagem, excecaoInterna)
    {
        CodigoStatus = codigoStatus;
    }
}