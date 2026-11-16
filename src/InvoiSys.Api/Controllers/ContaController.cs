using InvoiSys.Application.Common.Responses;
using InvoiSys.Application.Usuarios.AlterarPropriaSenha;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InvoiSys.Api.Controllers;

[ApiController]
[Route("api/conta")]
[Authorize]
public sealed class ContaController(
    IAlterarPropriaSenhaUseCase alterarPropriaSenhaUseCase) : ControllerBase
{
    [HttpPut("senha")]
    [SwaggerOperation(
        Summary = "Altera a senha do usuário logado",
        Description = "Exige a senha atual e encerra as outras sessões do usuário.",
        OperationId = "AlterarPropriaSenha",
        Tags = ["Conta"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Senha alterada com sucesso.",
        typeof(ApiResponse<object>))]
    [SwaggerResponse(
        StatusCodes.Status400BadRequest,
        "Senha atual incorreta ou senha nova fora da política.",
        typeof(ApiResponse<object>))]
    public async Task<IActionResult> AlterarSenhaAsync(
        [FromBody] AlterarPropriaSenhaRequest request,
        CancellationToken cancellationToken)
    {
        // O "sid" do token identifica a sessão atual, que continua válida após a troca.
        var sessaoAtualId = Guid.TryParse(User.FindFirst("sid")?.Value, out var sid)
            ? sid
            : (Guid?)null;

        await alterarPropriaSenhaUseCase.Execute(
            request,
            sessaoAtualId,
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(
            "Senha alterada com sucesso."));
    }
}
