namespace InvoiSys.Application.Common.Exceptions;

public abstract class InvoiSysException(
    string mensagem,
    int statusCode,
    params string[] erros) : Exception(mensagem)
{
    private readonly IReadOnlyList<string> _erros =
        erros.Length == 0 ? [mensagem] : erros;

    public int StatusCode { get; } = statusCode;

    public IReadOnlyList<string> GetErrors() => _erros;
}
