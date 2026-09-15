using FluentValidation;

namespace InvoiSys.Application.Documentos.ListarDocumentos;

public sealed class ListarDocumentosValidator : AbstractValidator<ListarDocumentosRequest>
{
    public ListarDocumentosValidator()
    {
        RuleFor(x => x.Pagina).GreaterThan(0);
        RuleFor(x => x.TamanhoPagina).InclusiveBetween(1, 100);
        RuleFor(x => x.Busca).MaximumLength(200);
    }
}
