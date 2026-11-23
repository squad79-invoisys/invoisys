namespace InvoiSys.Application.Documentos.Anexos;

/// <param name="Id">Identifica o anexo dentro do documento; é o que a API aceita para baixá-lo.</param>
/// <param name="Nome">Texto para exibição.</param>
/// <param name="NomeArquivo">Nome sugerido ao salvar, já com extensão e sem caracteres inválidos.</param>
/// <param name="Url">Endereço do arquivo no site de origem.</param>
/// <param name="Visualizavel">Verdadeiro para PDF e imagens, que o navegador exibe sem riscos.</param>
public sealed record AnexoResponse(
    string Id,
    string Nome,
    string NomeArquivo,
    string Url,
    string Extensao,
    string TipoConteudo,
    bool Visualizavel);

/// <param name="VerificacaoCompleta">
/// Falso quando a página de origem não pôde ser consultada; a lista pode estar incompleta.
/// </param>
public sealed record AnexosDocumentoResponse(
    IReadOnlyList<AnexoResponse> Itens,
    bool VerificacaoCompleta);
