namespace InvoiSys.Application.Common.Authentication;

public sealed record AuthenticatedUser(
    Guid Id,
    string Nome,
    string Email,
    IReadOnlyList<string> Perfis);
