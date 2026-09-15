using Microsoft.AspNetCore.Identity;

namespace InvoiSys.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;
}
