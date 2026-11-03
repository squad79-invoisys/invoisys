using System.Text;

namespace InvoiSys.Collectors.Pdf;

internal static class PdfAssinatura
{
    // A especificação PDF permite que o cabeçalho "%PDF-" apareça em qualquer
    // posição dos primeiros 1024 bytes do arquivo.
    public const int LimiteCabecalho = 1024;

    private static readonly byte[] Cabecalho = Encoding.ASCII.GetBytes("%PDF-");

    public static bool Possui(ReadOnlySpan<byte> conteudo) =>
        conteudo[..Math.Min(conteudo.Length, LimiteCabecalho)]
            .IndexOf(Cabecalho) >= 0;
}
