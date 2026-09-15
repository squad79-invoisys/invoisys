namespace InvoiSys.Application.Common.Authentication;

public sealed record UserSummary(
    Guid Id,
    string Nome,
    string Email,
    bool Ativo,
    IReadOnlyList<string> Perfis);
