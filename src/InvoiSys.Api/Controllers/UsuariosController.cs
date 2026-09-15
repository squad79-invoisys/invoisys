using InvoiSys.Application.Common.Authentication;
using InvoiSys.Application.Common.Responses;
using InvoiSys.Application.Usuarios.AlterarStatusUsuario;
using InvoiSys.Application.Usuarios.CriarUsuario;
using InvoiSys.Application.Usuarios.ListarUsuarios;
using InvoiSys.Application.Usuarios.RedefinirSenha;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InvoiSys.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Administrador")]
public sealed class UsuariosController(
    ICriarUsuarioUseCase criarUsuarioUseCase,
    IListarUsuariosUseCase listarUsuariosUseCase,
    IAlterarStatusUsuarioUseCase alterarStatusUsuarioUseCase,
    IRedefinirSenhaUseCase redefinirSenhaUseCase) : ControllerBase
{
    [HttpPost]
    [SwaggerOperation(
        Summary = "Cria um usuário",
        Description = "Cria um usuário interno e associa o perfil informado.",
        OperationId = "CriarUsuario",
        Tags = ["Usuários"])]
    [SwaggerResponse(
        StatusCodes.Status201Created,
        "Usuário criado com sucesso.",
        typeof(ApiResponse<UserSummary>))]
    public async Task<IActionResult> CriarAsync(
        [FromBody] CriarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var response = await criarUsuarioUseCase.ExecutarAsync(
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse<UserSummary>.Ok(
                "Usuário criado com sucesso.",
                response));
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Lista usuários",
        Description = "Lista os usuários e seus perfis.",
        OperationId = "ListarUsuarios",
        Tags = ["Usuários"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Usuários listados com sucesso.",
        typeof(ApiResponse<IReadOnlyList<UserSummary>>))]
    public async Task<IActionResult> ListarAsync(
        CancellationToken cancellationToken)
    {
        var response = await listarUsuariosUseCase.Execute(
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<UserSummary>>.Ok(
            "Usuários listados com sucesso.",
            response));
    }

    [HttpPatch("{id:guid}/status")]
    [SwaggerOperation(
        Summary = "Ativa ou inativa um usuário",
        Description = "Altera o status e revoga sessões ao inativar.",
        OperationId = "AlterarStatusUsuario",
        Tags = ["Usuários"])]
    public async Task<IActionResult> AlterarStatusAsync(
        Guid id,
        [FromBody] AlterarStatusUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        await alterarStatusUsuarioUseCase.Execute(
            id,
            request,
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(
            "Status do usuário alterado com sucesso."));
    }

    [HttpPut("{id:guid}/senha")]
    [SwaggerOperation(
        Summary = "Redefine a senha de um usuário",
        Description = "Redefine a senha e revoga as sessões existentes.",
        OperationId = "RedefinirSenhaUsuario",
        Tags = ["Usuários"])]
    public async Task<IActionResult> RedefinirSenhaAsync(
        Guid id,
        [FromBody] RedefinirSenhaRequest request,
        CancellationToken cancellationToken)
    {
        await redefinirSenhaUseCase.Execute(
            id,
            request,
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(
            "Senha redefinida com sucesso."));
    }
}
