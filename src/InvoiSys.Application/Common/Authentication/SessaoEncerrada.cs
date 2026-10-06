namespace InvoiSys.Application.Common.Authentication;

public sealed record SessaoEncerrada(
    Guid UsuarioId,
    string Email);
