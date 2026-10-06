using InvoiSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Data.Configurations;

public sealed class RegistroAuditoriaConfiguration
    : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.ToTable("registros_auditoria");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Tipo)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Atividade)
            .HasMaxLength(RegistroAuditoria.TamanhoMaximoAtividade)
            .IsRequired();

        builder.Property(x => x.Objeto)
            .HasMaxLength(RegistroAuditoria.TamanhoMaximoObjeto)
            .IsRequired();

        builder.Property(x => x.Responsavel)
            .HasMaxLength(RegistroAuditoria.TamanhoMaximoResponsavel)
            .IsRequired();

        builder.Property(x => x.EnderecoIp)
            .HasMaxLength(RegistroAuditoria.TamanhoMaximoEnderecoIp);

        // Sem FK para usuários/fontes: o histórico deve sobreviver à remoção do objeto.
        builder.HasIndex(x => x.OcorridoEm);
        builder.HasIndex(x => x.Tipo);
        builder.HasIndex(x => x.UsuarioId);
    }
}
