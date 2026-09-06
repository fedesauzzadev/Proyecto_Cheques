using IrmaRios.Cartera.Application.Puertos;
using IrmaRios.Cartera.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IrmaRios.Cartera.Infrastructure.Persistence;

public class CarteraDbContext(DbContextOptions<CarteraDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Domain.Entidades.InstrumentoCartera> Instrumentos => Set<Domain.Entidades.InstrumentoCartera>();
    public DbSet<Domain.Entidades.Deposito> Depositos => Set<Domain.Entidades.Deposito>();
    public DbSet<EntradaIdempotencia> IdempotenciaKeys => Set<EntradaIdempotencia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Domain.Entidades.InstrumentoCartera>(builder =>
        {
            builder.ToTable("instrumentos_cartera");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Identificador).HasMaxLength(30).IsRequired();
            builder.Property(i => i.CuitLibrador).HasMaxLength(11).IsRequired();
            builder.Property(i => i.CuitBeneficiario).HasMaxLength(11).IsRequired();
            builder.Property(i => i.TitularCuit).HasMaxLength(11).IsRequired();
            builder.Property(i => i.Monto).HasPrecision(18, 2);
            builder.Property(i => i.Tipo).HasConversion<int>();
            builder.Property(i => i.Moneda).HasConversion<int>();
            builder.Property(i => i.EstadoClearing).HasConversion<int>();
            builder.Property(i => i.Estado).HasConversion<int>();
            builder.HasIndex(i => i.Identificador).IsUnique();
            // Listados por titular y tipo (HU-03): índice compuesto con el orden natural (vencimiento).
            builder.HasIndex(i => new { i.TitularCuit, i.Tipo, i.FechaVencimiento });
        });

        modelBuilder.Entity<Domain.Entidades.Deposito>(builder =>
        {
            builder.ToTable("depositos");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.CuitEmpresa).HasMaxLength(11).IsRequired();
            builder.HasMany(d => d.Detalles).WithOne().HasForeignKey("DepositoId").IsRequired();
            // La colección es de solo lectura con backing field privado.
            builder.Navigation(d => d.Detalles).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.HasIndex(d => d.CuitEmpresa);
        });

        modelBuilder.Entity<Domain.Entidades.DepositoDetalle>(builder =>
        {
            builder.ToTable("deposito_detalles");
            builder.HasKey("DepositoId", "InstrumentoCarteraId");
        });

        modelBuilder.Entity<EntradaIdempotencia>(builder =>
        {
            builder.ToTable("idempotencia");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Key).HasMaxLength(64).IsRequired();
            builder.Property(e => e.BodyHash).HasMaxLength(64).IsRequired();
            builder.Property(e => e.ResponseJson).IsRequired();
            builder.HasIndex(e => e.Key).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>Traduce unicidad violada de PostgreSQL (23505) a conflicto de dominio (409).</summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            var constraint = pg.ConstraintName ?? string.Empty;
            var mensaje = constraint.Contains("Identificador", StringComparison.OrdinalIgnoreCase)
                ? "El instrumento ya existe en la cartera."
                : constraint.Contains("Idempotencia", StringComparison.OrdinalIgnoreCase)
                    ? "La Idempotency-Key ya fue registrada por otra request concurrente."
                    : "Se detectó un conflicto de unicidad al guardar los datos.";

            throw new ConflictoDominioException(mensaje);
        }
    }
}
