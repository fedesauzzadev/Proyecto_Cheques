using Coelsa.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coelsa.Infrastructure.Persistence.Configuraciones;

public class CesionConfiguracion : IEntityTypeConfiguration<Cesion>
{
    public void Configure(EntityTypeBuilder<Cesion> builder)
    {
        builder.ToTable("cesiones");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Numero).IsRequired();
        builder.Property(c => c.CuitCedente).HasMaxLength(11).IsRequired();
        builder.Property(c => c.CuitCesionario).HasMaxLength(11).IsRequired();
        builder.Property(c => c.DomicilioCesionario).HasMaxLength(Cesion.LongitudMaximaDomicilio).IsRequired();
        builder.Property(c => c.Estado).HasConversion<int>();

        builder.HasIndex(c => new { c.EcheqId, c.Numero })
            .IsUnique()
            .HasDatabaseName("ix_cesiones_echeq_numero_unicos");
    }
}
