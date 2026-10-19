using System.Data.Common;

namespace InvoiSys.Application.Common.Coletas;

public interface IFonteColetaLock
{
    Task<IFonteColetaLease?> TentarAdquirirAsync(
        Guid fonteId,
        CancellationToken cancellationToken);

    Task VerificarSessaoAsync(CancellationToken cancellationToken);

    IFonteColetaLease? PosseAtual { get; }
}

public interface IFonteColetaLease : IAsyncDisposable
{
    bool EstaReivindicada { get; }

    Task<bool> TentarReivindicarAsync(CancellationToken cancellationToken);

    Task RenovarAsync(CancellationToken cancellationToken);

    Task ValidarTransacaoAsync(DbTransaction transaction, CancellationToken cancellationToken);

    Task RenovarTransacaoAsync(DbTransaction transaction, CancellationToken cancellationToken);

    Task VerificarSessaoAsync(CancellationToken cancellationToken);
}
