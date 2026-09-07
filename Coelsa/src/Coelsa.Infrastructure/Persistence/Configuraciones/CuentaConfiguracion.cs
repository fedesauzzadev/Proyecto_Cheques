using Coelsa.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coelsa.Infrastructure.Persistence.Configuraciones;

public class CuentaConfiguracion : IEntityTypeConfiguration<Cuenta>
{
    public void Configure(EntityTypeBuilder<Cuenta> builder)
    {
        builder.ToTable("cuentas");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Cbu).HasMaxLength(22).IsRequired();
        builder.Property(c => c.CuitTitular).HasMaxLength(11).IsRequired();
        builder.Property(c => c.NombreTitular).HasMaxLength(120).IsRequired();
        builder.Property(c => c.Moneda).HasConversion<int>();

        builder.HasIndex(c => c.Cbu).IsUnique();

        builder.HasIndex(c => new { c.CuitTitular, c.Activa })
            .HasDatabaseName("ix_cuentas_cuit_titular");
    }
}
