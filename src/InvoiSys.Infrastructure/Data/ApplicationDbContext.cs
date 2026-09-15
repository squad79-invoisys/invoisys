using InvoiSys.Domain.Entities;
using InvoiSys.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Data;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Fonte> Fontes => Set<Fonte>();
    public DbSet<ExecucaoColeta> ExecucoesColeta => Set<ExecucaoColeta>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        builder.Entity<ApplicationUser>().ToTable("usuarios");
        builder.Entity<IdentityRole<Guid>>().ToTable("perfis");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("usuarios_perfis");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("usuarios_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("usuarios_logins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("perfis_claims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("usuarios_tokens");
    }
}
