namespace InvoiSys.Application.Common.Responses;

public sealed record ApiResponse<T>(
    string Mensagem,
    T? Dados,
    bool Sucesso)
{
    public static ApiResponse<T> Ok(string mensagem, T? dados = default) =>
        new(mensagem, dados, true);

    public static ApiResponse<T> Erro(string mensagem) =>
        new(mensagem, default, false);
}
