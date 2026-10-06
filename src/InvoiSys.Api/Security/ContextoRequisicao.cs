using InvoiSys.Application.Common.Abstractions;

namespace InvoiSys.Api.Security;

public sealed class ContextoRequisicao(
    IHttpContextAccessor httpContextAccessor) : IContextoRequisicao
{
    public string? EnderecoIp
    {
        get
        {
            var endereco = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;

            if (endereco is null)
                return null;

            return endereco.IsIPv4MappedToIPv6
                ? endereco.MapToIPv4().ToString()
                : endereco.ToString();
        }
    }
}
