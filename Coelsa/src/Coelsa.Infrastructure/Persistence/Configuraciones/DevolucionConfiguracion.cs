using Coelsa.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coelsa.Infrastructure.Persistence.Configuraciones;

public class DevolucionConfiguracion : IEntityTypeConfiguration<Devolucion>
{
    public void Configure(EntityTypeBuilder<Devolucion> builder)
    {
        builder.ToTable("devoluciones");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Numero).IsRequired();
        builder.Property(d => d.CuitSolicitante).HasMaxLength(11).IsRequired();
        builder.Property(d => d.Motivo).HasMaxLength(Devolucion.LongitudMaximaMotivo);
        builder.Property(d => d.Estado).HasConversion<int>();

        builder.HasIndex(d => new { d.EcheqId, d.Numero })
            .IsUnique()
            .HasDatabaseName("ix_devoluciones_echeq_numero_unicos");
    }
}
