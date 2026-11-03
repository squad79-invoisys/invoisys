using InvoiSys.Domain.Entities;
using InvoiSys.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Data.Configurations;

internal sealed class FonteColetaLeaseConfiguration : IEntityTypeConfiguration<FonteColetaLease>
{
    public void Configure(EntityTypeBuilder<FonteColetaLease> builder)
    {
        builder.ToTable("fontes_coleta_lease");
        builder.HasKey(x => x.FonteId);
        builder.Property(x => x.FonteId).HasColumnName("fonte_id");
        builder.Property(x => x.ProprietarioId).HasColumnName("proprietario_id").IsRequired();
        builder.Property(x => x.Geracao).HasColumnName("geracao").IsRequired();
        builder.Property(x => x.ValidoAte).HasColumnName("valido_ate").IsRequired();
        builder.HasOne<Fonte>().WithMany().HasForeignKey(x => x.FonteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
