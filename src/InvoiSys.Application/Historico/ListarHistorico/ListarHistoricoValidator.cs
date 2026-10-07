using FluentValidation;

namespace InvoiSys.Application.Historico.ListarHistorico;

public sealed class ListarHistoricoValidator : AbstractValidator<ListarHistoricoRequest>
{
    public ListarHistoricoValidator()
    {
        RuleFor(x => x.Pagina).GreaterThan(0);
        RuleFor(x => x.TamanhoPagina).InclusiveBetween(1, 100);
        this.AplicarRegrasDeFiltro();
    }
}
