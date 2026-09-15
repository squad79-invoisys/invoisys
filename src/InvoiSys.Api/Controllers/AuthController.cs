using InvoiSys.Application.Autenticacao;
using InvoiSys.Application.Autenticacao.Login;
using InvoiSys.Application.Autenticacao.Logout;
using InvoiSys.Application.Autenticacao.Refresh;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Application.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InvoiSys.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    ILoginUseCase loginUseCase,
    IRefreshTokenUseCase refreshTokenUseCase,
    ILogoutUseCase logoutUseCase) : ControllerBase
{
    private const string RefreshCookieName = "invoisys.refresh_token";

    [HttpPost("login")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Autentica o usuário",
        Description = "Valida e-mail e senha e retorna o access token JWT.",
        OperationId = "Login",
        Tags = ["Autenticação"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Login realizado com sucesso.",
        typeof(ApiResponse<LoginResponse>))]
    [SwaggerResponse(
        StatusCodes.Status401Unauthorized,
        "Credenciais inválidas.",
        typeof(ApiResponse<object>))]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await loginUseCase.ExecutarAsync(
            request,
            cancellationToken);

        EscreverRefreshCookie(result);

        return Ok(ApiResponse<LoginResponse>.Ok(
            "Login realizado com sucesso.",
            result.Response));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Renova a sessão",
        Description = "Usa o refresh token HttpOnly para emitir um novo access token.",
        OperationId = "RefreshToken",
        Tags = ["Autenticação"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Token renovado com sucesso.",
        typeof(ApiResponse<LoginResponse>))]
    public async Task<IActionResult> RefreshAsync(
        CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName];

        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedException("Refresh token não encontrado.");

        var result = await refreshTokenUseCase.ExecutarAsync(
            refreshToken,
            cancellationToken);

        EscreverRefreshCookie(result);

        return Ok(ApiResponse<LoginResponse>.Ok(
            "Token renovado com sucesso.",
            result.Response));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Encerra a sessão",
        Description = "Revoga o refresh token atual e remove o cookie.",
        OperationId = "Logout",
        Tags = ["Autenticação"])]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "Logout realizado com sucesso.",
        typeof(ApiResponse<object>))]
    public async Task<IActionResult> LogoutAsync(
        CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshCookieName];

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await logoutUseCase.ExecutarAsync(
                refreshToken,
                cancellationToken);
        }

        Response.Cookies.Delete(RefreshCookieName);

        return Ok(ApiResponse<object>.Ok(
            "Logout realizado com sucesso."));
    }

    private void EscreverRefreshCookie(AuthUseCaseResult result)
    {
        Response.Cookies.Append(
            RefreshCookieName,
            result.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = Request.IsHttps
                    ? SameSiteMode.None
                    : SameSiteMode.Lax,
                Expires = result.RefreshTokenExpiraEm,
                IsEssential = true,
                Path = "/"
            });
    }
}
