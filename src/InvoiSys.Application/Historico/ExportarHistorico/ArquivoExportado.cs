namespace InvoiSys.Application.Historico.ExportarHistorico;

public sealed record ArquivoExportado(
    byte[] Conteudo,
    string NomeArquivo,
    string ContentType);
