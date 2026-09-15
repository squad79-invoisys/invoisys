namespace InvoiSys.Infrastructure.Identity;

public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiraEm { get; set; }
    public DateTimeOffset? RevogadoEm { get; set; }

    public ApplicationUser Usuario { get; set; } = null!;
}
