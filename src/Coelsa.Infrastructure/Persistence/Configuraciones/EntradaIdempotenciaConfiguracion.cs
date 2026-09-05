using Coelsa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Coelsa.Infrastructure.Persistence.Configuraciones;

public class EntradaIdempotenciaConfiguracion : IEntityTypeConfiguration<EntradaIdempotencia>
{
    public void Configure(EntityTypeBuilder<EntradaIdempotencia> builder)
    {
        builder.ToTable("idempotency_keys");

        builder.HasKey(k => k.Key);

        builder.Property(k => k.Key).HasMaxLength(64).IsRequired();
        builder.Property(k => k.BodyHash).HasMaxLength(64).IsRequired();
        builder.Property(k => k.ResponseJson).IsRequired();
        builder.Property(k => k.Tipo).HasConversion<int>();
    }
}
