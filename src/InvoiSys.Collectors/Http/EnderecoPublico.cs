using System.Net;
using System.Net.Sockets;

namespace InvoiSys.Collectors.Http;

/// <summary>
/// Decide se um endereço IP pode ser acessado a pedido de um usuário. Barra
/// loopback, redes privadas, link-local (inclui o serviço de metadados de
/// nuvem, 169.254.169.254) e demais faixas que não são internet pública.
/// </summary>
public static class EnderecoPublico
{
    public static bool Permitido(IPAddress endereco)
    {
        if (endereco.IsIPv4MappedToIPv6)
            endereco = endereco.MapToIPv4();

        if (IPAddress.IsLoopback(endereco) ||
            endereco.Equals(IPAddress.Any) ||
            endereco.Equals(IPAddress.IPv6Any) ||
            endereco.Equals(IPAddress.Broadcast))
        {
            return false;
        }

        return endereco.AddressFamily switch
        {
            AddressFamily.InterNetwork => Ipv4Permitido(endereco.GetAddressBytes()),
            AddressFamily.InterNetworkV6 => Ipv6Permitido(endereco),
            _ => false
        };
    }

    private static bool Ipv4Permitido(byte[] b) =>
        !(b[0] == 0                                          // 0.0.0.0/8
          || b[0] == 10                                      // 10.0.0.0/8
          || (b[0] == 100 && (b[1] & 0xC0) == 64)            // 100.64.0.0/10 (CGNAT)
          || b[0] == 127                                     // 127.0.0.0/8
          || (b[0] == 169 && b[1] == 254)                    // 169.254.0.0/16
          || (b[0] == 172 && (b[1] & 0xF0) == 16)            // 172.16.0.0/12
          || (b[0] == 192 && b[1] == 0 && b[2] == 0)         // 192.0.0.0/24
          || (b[0] == 192 && b[1] == 168)                    // 192.168.0.0/16
          || (b[0] == 198 && (b[1] & 0xFE) == 18)            // 198.18.0.0/15
          || b[0] >= 224);                                   // multicast e reservado

    private static bool Ipv6Permitido(IPAddress endereco)
    {
        if (endereco.IsIPv6LinkLocal || endereco.IsIPv6SiteLocal || endereco.IsIPv6Multicast)
            return false;

        var b = endereco.GetAddressBytes();

        if ((b[0] & 0xFE) == 0xFC)                           // fc00::/7 (unique local)
            return false;

        // NAT64 (64:ff9b::/96): vale o IPv4 embutido.
        if (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xFF && b[3] == 0x9B &&
            b[4..12].All(valor => valor == 0))
        {
            return Ipv4Permitido(b[12..16]);
        }

        // 6to4 (2002::/16): o IPv4 vem nos bytes 2 a 5.
        if (b[0] == 0x20 && b[1] == 0x02)
            return Ipv4Permitido(b[2..6]);

        return true;
    }
}
