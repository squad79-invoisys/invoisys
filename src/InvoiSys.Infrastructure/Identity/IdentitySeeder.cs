using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiSys.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        var roleManager =
            serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager =
            serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in RoleNames.Todos)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant()
                });
            }
        }

        var email = configuration["BootstrapAdmin:Email"];
        var password = configuration["BootstrapAdmin:Password"];
        var nome = configuration["BootstrapAdmin:Nome"] ?? "Administrador";

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var existente = await userManager.FindByEmailAsync(email);
        if (existente is not null)
            return;

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Ativo = true
        };

        var createResult = await userManager.CreateAsync(admin, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Não foi possível criar o administrador inicial: " +
                string.Join(" ", createResult.Errors.Select(error => error.Description)));
        }

        await userManager.AddToRoleAsync(admin, RoleNames.Administrador);
    }
}
