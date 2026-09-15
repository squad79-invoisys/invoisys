using InvoiSys.Application.Common.Responses;
using InvoiSys.Application.Fontes;
using InvoiSys.Application.Fontes.AlterarStatusFonte;
using InvoiSys.Application.Fontes.AtualizarFonte;
using InvoiSys.Application.Fontes.ConsultarFonte;
using InvoiSys.Application.Fontes.CriarFonte;
using InvoiSys.Application.Fontes.ListarFontes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InvoiSys.Api.Controllers;

[ApiController]
[Route("api/fontes")]
[Authorize]
public sealed class FontesController(
    ICriarFonteUseCase criarFonteUseCase,
    IConsultarFonteUseCase consultarFonteUseCase,
    IListarFontesUseCase listarFontesUseCase,
    IAtualizarFonteUseCase atualizarFonteUseCase,
    IAlterarStatusFonteUseCase alterarStatusFonteUseCase) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Administrador,Operador")]
    [SwaggerOperation(
        Summary = "Cadastra uma nova fonte",
        Description = "Cadastra uma fonte de informação para as rotinas de coleta.",
        OperationId = "CriarFonte",
        Tags = ["Fontes"])]
    [SwaggerResponse(
        StatusCodes.Status201Created,
        "Fonte criada com sucesso.",
        typeof(ApiResponse<FonteResponse>))]
    [SwaggerResponse(
        StatusCodes.Status400BadRequest,
        "Dados da fonte inválidos.",
        typeof(ApiResponse<object>))]
    public async Task<IActionResult> CriarAsync(
        [FromBody] CriarFonteRequest request,
        CancellationToken cancellationToken)
    {
        var response = await criarFonteUseCase.ExecutarAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(ConsultarPorId),
            new { id = response.Id },
            ApiResponse<FonteResponse>.Ok(
                "Fonte criada com sucesso.",
                response));
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Consulta uma fonte",
        Description = "Retorna os dados da fonte pelo identificador.",
        OperationId = "ConsultarFonte",
        Tags = ["Fontes"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Fonte consultada com sucesso.",
        typeof(ApiResponse<FonteResponse>))]
    [SwaggerResponse(
        StatusCodes.Status404NotFound,
        "Fonte não encontrada.",
        typeof(ApiResponse<object>))]
    public async Task<IActionResult> ConsultarPorId(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await consultarFonteUseCase.Execute(
            id,
            cancellationToken);

        return Ok(ApiResponse<FonteResponse>.Ok(
            "Fonte consultada com sucesso.",
            response));
    }

    [HttpGet]
    [SwaggerOperation(
        Summary = "Lista as fontes",
        Description = "Retorna as fontes com paginação e filtros opcionais.",
        OperationId = "ListarFontes",
        Tags = ["Fontes"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Fontes listadas com sucesso.",
        typeof(ApiResponse<PagedResponse<FonteResponse>>))]
    public async Task<IActionResult> ListarAsync(
        [FromQuery] ListarFontesRequest request,
        CancellationToken cancellationToken)
    {
        var response = await listarFontesUseCase.Execute(
            request,
            cancellationToken);

        return Ok(ApiResponse<PagedResponse<FonteResponse>>.Ok(
            "Fontes listadas com sucesso.",
            response));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrador,Operador")]
    [SwaggerOperation(
        Summary = "Atualiza uma fonte",
        Description = "Atualiza dados e periodicidade da fonte.",
        OperationId = "AtualizarFonte",
        Tags = ["Fontes"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Fonte atualizada com sucesso.",
        typeof(ApiResponse<FonteResponse>))]
    public async Task<IActionResult> AtualizarAsync(
        Guid id,
        [FromBody] AtualizarFonteRequest request,
        CancellationToken cancellationToken)
    {
        var response = await atualizarFonteUseCase.Execute(
            id,
            request,
            cancellationToken);

        return Ok(ApiResponse<FonteResponse>.Ok(
            "Fonte atualizada com sucesso.",
            response));
    }

    [HttpPatch("{id:guid}/ativar")]
    [Authorize(Roles = "Administrador,Operador")]
    [SwaggerOperation(
        Summary = "Ativa uma fonte",
        Description = "Habilita a fonte para novas execuções.",
        OperationId = "AtivarFonte",
        Tags = ["Fontes"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Fonte ativada com sucesso.",
        typeof(ApiResponse<object>))]
    public async Task<IActionResult> AtivarAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await alterarStatusFonteUseCase.Execute(
            id,
            true,
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(
            "Fonte ativada com sucesso."));
    }

    [HttpPatch("{id:guid}/desativar")]
    [Authorize(Roles = "Administrador,Operador")]
    [SwaggerOperation(
        Summary = "Desativa uma fonte",
        Description = "Retira a fonte das execuções automáticas.",
        OperationId = "DesativarFonte",
        Tags = ["Fontes"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Fonte desativada com sucesso.",
        typeof(ApiResponse<object>))]
    public async Task<IActionResult> DesativarAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await alterarStatusFonteUseCase.Execute(
            id,
            false,
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(
            "Fonte desativada com sucesso."));
    }
}
