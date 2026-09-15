namespace InvoiSys.Application.Usuarios.CriarUsuario;

public sealed record CriarUsuarioRequest(
    string Nome,
    string Email,
    string Senha,
    string Perfil);
