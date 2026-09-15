using InvoiSys.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiSys.Infrastructure.Data.Configurations;

public sealed class FonteConfiguration : IEntityTypeConfiguration<Fonte>
{
    public void Configure(EntityTypeBuilder<Fonte> builder)
    {
        builder.ToTable("fontes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nome)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Url)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.Tipo)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(x => x.Url);
        builder.HasIndex(x => x.Status);
    }
}
