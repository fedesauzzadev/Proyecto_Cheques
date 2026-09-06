using IrmaRios.Identidad.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IrmaRios.Identidad.Infrastructure.Persistence.Configuraciones;

internal class EmpresaConfiguracion : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("empresas");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Cuit).HasMaxLength(11).IsRequired();
        builder.Property(e => e.RazonSocial).HasMaxLength(200).IsRequired();
        // Índice único parcial: el CUIT es único solo entre empresas activas (baja lógica, ADR-007).
        builder.HasIndex(e => e.Cuit).IsUnique().HasFilter("\"Borrado\" = false");
    }
}

internal class PersonaConfiguracion : IEntityTypeConfiguration<Persona>
{
    public void Configure(EntityTypeBuilder<Persona> builder)
    {
        builder.ToTable("personas");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.DocTipo).HasMaxLength(8).IsRequired();
        builder.Property(p => p.DocNumero).HasMaxLength(12).IsRequired();
        builder.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Apellido).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(200).IsRequired();
        builder.HasIndex(p => new { p.DocTipo, p.DocNumero }).IsUnique().HasFilter("\"Borrado\" = false");
    }
}

internal class VinculoConfiguracion : IEntityTypeConfiguration<Vinculo>
{
    public void Configure(EntityTypeBuilder<Vinculo> builder)
    {
        builder.ToTable("vinculos");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Rol).HasConversion<int>();
        builder.HasIndex(v => v.EmpresaId);
        builder.HasIndex(v => v.PersonaId);
        builder.HasIndex(v => new { v.PersonaId, v.EmpresaId, v.Rol })
            .IsUnique()
            .HasFilter("\"HastaUtc\" IS NULL");
    }
}

internal class EntradaIdempotenciaConfiguracion : IEntityTypeConfiguration<EntradaIdempotencia>
{
    public void Configure(EntityTypeBuilder<EntradaIdempotencia> builder)
    {
        builder.ToTable("idempotencia");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Key).HasMaxLength(64).IsRequired();
        builder.Property(e => e.BodyHash).HasMaxLength(64).IsRequired();
        builder.Property(e => e.ResponseJson).IsRequired();
        builder.HasIndex(e => e.Key).IsUnique();
    }
}
