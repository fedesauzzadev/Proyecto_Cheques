using Coelsa.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coelsa.Infrastructure.Persistence.Configuraciones;

public class EcheqConfiguracion : IEntityTypeConfiguration<Echeq>
{
    public void Configure(EntityTypeBuilder<Echeq> builder)
    {
        builder.ToTable("echeqs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IdEcheq).HasMaxLength(11).IsRequired();
        builder.Property(e => e.Cmc7).HasMaxLength(30).IsRequired();
        builder.Property(e => e.CuitLibrador).HasMaxLength(11).IsRequired();
        builder.Property(e => e.CuitBeneficiario).HasMaxLength(11).IsRequired();
        builder.Property(e => e.Monto).HasPrecision(18, 2);
        builder.Property(e => e.Moneda).HasConversion<int>();
        builder.Property(e => e.MotivoRechazo).HasConversion<int>();

        builder.HasIndex(e => e.IdEcheq).IsUnique();
        builder.HasIndex(e => e.Cmc7).IsUnique();

        builder.HasIndex(e => new { e.CuitLibrador, e.FechaCreacion })
            .HasFilter("\"Activo\" = true")
            .IncludeProperties(
                nameof(Echeq.IdEcheq),
                nameof(Echeq.CuitBeneficiario),
                nameof(Echeq.Monto),
                nameof(Echeq.Moneda),
                nameof(Echeq.FechaEmision),
                nameof(Echeq.FechaDiferimiento),
                nameof(Echeq.Estado),
                nameof(Echeq.MotivoRechazo))
            .HasDatabaseName("ix_echeqs_cuit_librador_activos");

        builder.HasIndex(e => new { e.CuitBeneficiario, e.FechaCreacion })
            .HasFilter("\"Activo\" = true")
            .HasDatabaseName("ix_echeqs_cuit_beneficiario_activos");
    }
}
