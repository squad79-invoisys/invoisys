namespace InvoiSys.Infrastructure.Data.Entities;

internal sealed class FonteColetaLease
{
    public Guid FonteId { get; set; }
    public Guid ProprietarioId { get; set; }
    public long Geracao { get; set; }
    public DateTimeOffset ValidoAte { get; set; }
}
