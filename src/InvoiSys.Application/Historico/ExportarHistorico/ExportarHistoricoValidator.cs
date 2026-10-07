using FluentValidation;

namespace InvoiSys.Application.Historico.ExportarHistorico;

public sealed class ExportarHistoricoValidator : AbstractValidator<ExportarHistoricoRequest>
{
    public ExportarHistoricoValidator()
    {
        this.AplicarRegrasDeFiltro();
    }
}
