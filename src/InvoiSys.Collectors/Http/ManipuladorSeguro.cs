using System.Net;
using System.Net.Sockets;

namespace InvoiSys.Collectors.Http;

/// <summary>
/// Handler HTTP para buscar URLs informadas por usuários ou presentes em
/// conteúdo coletado. O endereço é validado no momento da conexão, sobre o IP
/// que será realmente usado; assim redirecionamentos e DNS rebinding também
/// são barrados.
/// </summary>
public static class ManipuladorSeguro
{
    public static SocketsHttpHandler Criar() =>
        new()
        {
            // Com proxy, o callback conectaria no proxy e não no destino.
            UseProxy = false,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = TimeSpan.FromSeconds(10),
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            ConnectCallback = ConectarAsync
        };

    private static async ValueTask<Stream> ConectarAsync(
        SocketsHttpConnectionContext contexto,
        CancellationToken cancellationToken)
    {
        var enderecos = await Dns.GetHostAddressesAsync(
            contexto.DnsEndPoint.Host,
            cancellationToken);

        var permitidos = enderecos.Where(EnderecoPublico.Permitido).ToArray();
        if (permitidos.Length == 0)
            throw new HttpRequestException("O destino não é um endereço público acessível.");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(permitidos, contexto.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
