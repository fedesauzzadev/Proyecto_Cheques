using Coelsa.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coelsa.Infrastructure.Persistence.Configuraciones;

public class ChequeraConfiguracion : IEntityTypeConfiguration<Chequera>
{
    public void Configure(EntityTypeBuilder<Chequera> builder)
    {
        builder.ToTable("chequeras");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Estado).HasConversion<int>();

        builder.HasIndex(c => new { c.CuentaId, c.Numero }).IsUnique();

        builder.HasIndex(c => new { c.CuentaId, c.Estado, c.ProximoNumero })
            .HasDatabaseName("ix_chequeras_cuenta_con_lugar");
    }
}
