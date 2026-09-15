namespace InvoiSys.Infrastructure.Identity;

public static class RoleNames
{
    public const string Administrador = "Administrador";
    public const string Operador = "Operador";
    public const string Consulta = "Consulta";

    public static readonly string[] Todos =
        [Administrador, Operador, Consulta];
}
