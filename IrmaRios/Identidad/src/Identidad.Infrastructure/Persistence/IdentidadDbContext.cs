using IrmaRios.Identidad.Application.Puertos;
using IrmaRios.Identidad.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IrmaRios.Identidad.Infrastructure.Persistence;

public class IdentidadDbContext(DbContextOptions<IdentidadDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Domain.Entidades.Empresa> Empresas => Set<Domain.Entidades.Empresa>();
    public DbSet<Domain.Entidades.Persona> Personas => Set<Domain.Entidades.Persona>();
    public DbSet<Domain.Entidades.Vinculo> Vinculos => Set<Domain.Entidades.Vinculo>();
    public DbSet<EntradaIdempotencia> IdempotenciaKeys => Set<EntradaIdempotencia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentidadDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Traduce violaciones de unicidad de PostgreSQL (23505) a conflictos de dominio (409),
    /// cubriendo carreras concurrentes que el chequeo previo no puede detectar.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            var constraint = pg.ConstraintName ?? string.Empty;
            var mensaje = constraint.Contains("Cuit", StringComparison.OrdinalIgnoreCase)
                ? "Ya existe una empresa registrada con ese CUIT."
                : constraint.Contains("Idempotencia", StringComparison.OrdinalIgnoreCase)
                    ? "La Idempotency-Key ya fue registrada por otra request concurrente."
                    : "Se detectó un conflicto de unicidad al guardar los datos.";

            throw new ConflictoDominioException(mensaje);
        }
    }
}
