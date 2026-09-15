using InvoiSys.Application.Common.Authentication;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Identity;

internal sealed class UserManagementService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext) : IUserManagementService
{
    public async Task<UserSummary> CriarAsync(
        string nome,
        string email,
        string senha,
        string perfil,
        CancellationToken cancellationToken)
    {
        var existente = await userManager.FindByEmailAsync(email);
        if (existente is not null)
            throw new ConflictException("Já existe um usuário com este e-mail.");

        var perfilNormalizado = RoleNames.Todos.FirstOrDefault(
            item => string.Equals(
                item,
                perfil,
                StringComparison.OrdinalIgnoreCase));

        if (perfilNormalizado is null)
            throw new BusinessException("Perfil inválido.");

        var usuario = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Nome = nome.Trim(),
            UserName = email.Trim(),
            Email = email.Trim(),
            EmailConfirmed = true,
            Ativo = true
        };

        var result = await userManager.CreateAsync(usuario, senha);

        if (!result.Succeeded)
            throw new BusinessException(JuntarErros(result));

        var roleResult = await userManager.AddToRoleAsync(
            usuario,
            perfilNormalizado);

        if (!roleResult.Succeeded)
            throw new BusinessException(JuntarErros(roleResult));

        return new UserSummary(
            usuario.Id,
            usuario.Nome,
            usuario.Email ?? string.Empty,
            usuario.Ativo,
            [perfilNormalizado]);
    }

    public async Task<IReadOnlyList<UserSummary>> ListarAsync(
        CancellationToken cancellationToken)
    {
        var usuarios = await userManager.Users
            .AsNoTracking()
            .OrderBy(usuario => usuario.Nome)
            .ToListAsync(cancellationToken);

        var response = new List<UserSummary>(usuarios.Count);

        foreach (var usuario in usuarios)
        {
            var perfis = await userManager.GetRolesAsync(usuario);
            response.Add(new UserSummary(
                usuario.Id,
                usuario.Nome,
                usuario.Email ?? string.Empty,
                usuario.Ativo,
                perfis.ToList()));
        }

        return response;
    }

    public async Task DefinirStatusAsync(
        Guid usuarioId,
        bool ativo,
        CancellationToken cancellationToken)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new NotFoundException("Usuário não encontrado.");

        usuario.Ativo = ativo;

        var result = await userManager.UpdateAsync(usuario);

        if (!result.Succeeded)
            throw new BusinessException(JuntarErros(result));

        if (!ativo)
            await RevogarSessoesAsync(usuario.Id, cancellationToken);
    }

    public async Task RedefinirSenhaAsync(
        Guid usuarioId,
        string novaSenha,
        CancellationToken cancellationToken)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new NotFoundException("Usuário não encontrado.");

        var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
        var result = await userManager.ResetPasswordAsync(
            usuario,
            token,
            novaSenha);

        if (!result.Succeeded)
            throw new BusinessException(JuntarErros(result));

        await RevogarSessoesAsync(usuario.Id, cancellationToken);
    }

    private async Task RevogarSessoesAsync(
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        var sessoes = await dbContext.RefreshTokens
            .Where(token =>
                token.UserId == usuarioId &&
                token.RevogadoEm == null)
            .ToListAsync(cancellationToken);

        foreach (var sessao in sessoes)
            sessao.RevogadoEm = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string JuntarErros(IdentityResult result) =>
        string.Join(
            " ",
            result.Errors.Select(error => error.Description));
}
