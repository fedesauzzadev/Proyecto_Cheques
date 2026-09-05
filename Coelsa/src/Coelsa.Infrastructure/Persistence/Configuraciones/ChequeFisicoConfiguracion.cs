using Coelsa.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coelsa.Infrastructure.Persistence.Configuraciones;

/// <summary>
/// Configuración de cheques físicos con índices para volumen (SPEC 5.5):
/// índices parciales sobre CUIT (solo instrumentos activos) con orden por fecha de creación.
/// </summary>
public class ChequeFisicoConfiguracion : IEntityTypeConfiguration<ChequeFisico>
{
    public void Configure(EntityTypeBuilder<ChequeFisico> builder)
    {
        builder.ToTable("cheques_fisicos");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Cmc7).HasMaxLength(30).IsRequired();
        builder.Property(c => c.CuitLibrador).HasMaxLength(11).IsRequired();
        builder.Property(c => c.CuitBeneficiario).HasMaxLength(11).IsRequired();
        builder.Property(c => c.Monto).HasPrecision(18, 2);
        builder.Property(c => c.Moneda).HasConversion<int>();
        builder.Property(c => c.MotivoRechazo).HasConversion<int>();

        builder.HasIndex(c => c.Cmc7).IsUnique();

        builder.HasIndex(c => new { c.CuitLibrador, c.FechaCreacion })
            .HasFilter("\"Activo\" = true")
            .IncludeProperties(
                nameof(ChequeFisico.Cmc7),
                nameof(ChequeFisico.CuitBeneficiario),
                nameof(ChequeFisico.Monto),
                nameof(ChequeFisico.Moneda),
                nameof(ChequeFisico.FechaEmision),
                nameof(ChequeFisico.FechaDiferimiento),
                nameof(ChequeFisico.Estado),
                nameof(ChequeFisico.MotivoRechazo))
            .HasDatabaseName("ix_cheques_cuit_librador_activos");

        builder.HasIndex(c => new { c.CuitBeneficiario, c.FechaCreacion })
            .HasFilter("\"Activo\" = true")
            .HasDatabaseName("ix_cheques_cuit_beneficiario_activos");
    }
}
