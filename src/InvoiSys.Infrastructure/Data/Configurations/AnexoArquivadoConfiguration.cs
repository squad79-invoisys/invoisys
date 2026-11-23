using InvoiSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Data.Configurations;

public sealed class AnexoArquivadoConfiguration : IEntityTypeConfiguration<AnexoArquivado>
{
    public void Configure(EntityTypeBuilder<AnexoArquivado> builder)
    {
        builder.ToTable("anexos_arquivados");

        builder.HasKey(x => x.Id);

        // Mesmo limite do índice de documentos: o Postgres não indexa valores
        // maiores que isso com B-tree.
        builder.Property(x => x.Url)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.HashConteudo)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.ChaveObjeto)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.TipoConteudo)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.NomeArquivo)
            .HasMaxLength(260)
            .IsRequired();

        // Uma URL arquivada uma vez só; é o que permite o "já existe?" sair direto do banco.
        builder.HasIndex(x => x.Url)
            .IsUnique();

        builder.HasIndex(x => x.HashConteudo);
    }
}
