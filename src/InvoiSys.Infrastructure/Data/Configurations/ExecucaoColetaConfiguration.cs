using InvoiSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Data.Configurations;

public sealed class ExecucaoColetaConfiguration
    : IEntityTypeConfiguration<ExecucaoColeta>
{
    public void Configure(EntityTypeBuilder<ExecucaoColeta> builder)
    {
        builder.ToTable("execucoes_coleta");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.TipoExecucao)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.MensagemErro)
            .HasMaxLength(2000);

        builder.HasOne<Fonte>()
            .WithMany()
            .HasForeignKey(x => x.FonteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.FonteId);
        builder.HasIndex(x => x.Inicio);
    }
}
