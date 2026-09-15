using InvoiSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Data.Configurations;

public sealed class DocumentoConfiguration : IEntityTypeConfiguration<Documento>
{
    public void Configure(EntityTypeBuilder<Documento> builder)
    {
        builder.ToTable("documentos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Titulo)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.UrlOriginal)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.Tipo)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Origem)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.ConteudoTextual)
            .HasColumnType("text");

        builder.Property(x => x.Metadados)
            .HasColumnType("jsonb");

        builder.Property(x => x.Hash)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasOne<ExecucaoColeta>()
            .WithMany()
            .HasForeignKey(x => x.ExecucaoColetaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ExecucaoColetaId);
        builder.HasIndex(x => x.Hash);
        builder.HasIndex(x => x.UrlOriginal);
    }
}
