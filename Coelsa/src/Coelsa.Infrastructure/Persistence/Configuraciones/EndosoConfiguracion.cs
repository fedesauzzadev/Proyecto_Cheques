using Coelsa.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coelsa.Infrastructure.Persistence.Configuraciones;

public class EndosoConfiguracion : IEntityTypeConfiguration<Endoso>
{
    public void Configure(EntityTypeBuilder<Endoso> builder)
    {
        builder.ToTable("endosos");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Orden).IsRequired();
        builder.Property(e => e.CuitEndosante).HasMaxLength(11).IsRequired();
        builder.Property(e => e.CuitEndosatario).HasMaxLength(11).IsRequired();
        builder.Property(e => e.Estado).HasConversion<int>();

        builder.HasIndex(e => new { e.EcheqId, e.Orden })
            .IsUnique()
            .HasDatabaseName("ix_endosos_echeq_orden_unicos");
    }
}
