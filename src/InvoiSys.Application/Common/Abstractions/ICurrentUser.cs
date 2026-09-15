namespace InvoiSys.Application.Common.Abstractions;

public interface ICurrentUser
{
    Guid? UsuarioId { get; }
    string? Email { get; }
    bool EstaAutenticado { get; }
}
