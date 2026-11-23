using InvoiSys.Application.Common.Anexos;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Documentos.Anexos;

public sealed class BaixarAnexoDocumentoUseCase(
    IDocumentoRepository documentoRepository,
    AnexoCatalogo catalogo,
    ArquivadorAnexos arquivador) : IBaixarAnexoDocumentoUseCase
{
    public async Task<ArquivoAnexo> Execute(
        Guid documentoId,
        string anexoId,
        CancellationToken cancellationToken)
    {
        var documento = await documentoRepository.ObterPorIdAsync(documentoId, cancellationToken)
            ?? throw new NotFoundException("Documento não encontrado.");

        // Só baixa o que a própria lista do documento oferece: a API não aceita
        // uma URL qualquer do cliente.
        var anexos = await catalogo.ListarAsync(documento, cancellationToken);
        var anexo = anexos.Itens.FirstOrDefault(item =>
                string.Equals(item.Id, anexoId, StringComparison.OrdinalIgnoreCase))
            ?? throw new NotFoundException(
                "Anexo não encontrado neste documento. Atualize a lista de anexos.");

        return await arquivador.ObterAsync(anexo.Url, anexo.NomeArquivo, cancellationToken);
    }
}
