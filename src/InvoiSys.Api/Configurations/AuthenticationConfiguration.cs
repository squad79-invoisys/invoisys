using System.Security.Claims;
using System.Text;
using InvoiSys.Application.Common.Authentication;
using InvoiSys.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace InvoiSys.Api.Configurations;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddAuthenticationConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "A configuração JWT não foi encontrada.");

        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 64)
        {
            throw new InvalidOperationException(
                "Jwt:Key deve possuir no mínimo 64 caracteres.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt.Key)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userIdValue =
                            context.Principal?.FindFirstValue(
                                ClaimTypes.NameIdentifier);
                        var sessionIdValue =
                            context.Principal?.FindFirstValue("sid");

                        if (!Guid.TryParse(userIdValue, out var userId) ||
                            !Guid.TryParse(sessionIdValue, out var sessionId))
                        {
                            context.Fail("Token sem identificação de sessão.");
                            return;
                        }

                        var validator = context.HttpContext.RequestServices
                            .GetRequiredService<IAuthenticationSessionValidator>();

                        var valid = await validator.SessaoValidaAsync(
                            userId,
                            sessionId,
                            context.HttpContext.RequestAborted);

                        if (!valid)
                            context.Fail("Sessão revogada ou inválida.");
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
