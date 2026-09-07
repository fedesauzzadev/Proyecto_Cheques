using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Coelsa.Infrastructure.Persistence.Configuraciones;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Coelsa.Infrastructure.Persistence;

public class CoelsaDbContext(DbContextOptions<CoelsaDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<ChequeFisico> ChequesFisicos => Set<ChequeFisico>();
    public DbSet<Echeq> Echeqs => Set<Echeq>();
    public DbSet<Cuenta> Cuentas => Set<Cuenta>();
    public DbSet<Chequera> Chequeras => Set<Chequera>();
    public DbSet<Endoso> Endosos => Set<Endoso>();
    public DbSet<Devolucion> Devoluciones => Set<Devolucion>();
    public DbSet<Cesion> Cesiones => Set<Cesion>();
    public DbSet<EntradaIdempotencia> IdempotenciaKeys => Set<EntradaIdempotencia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoelsaDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Traduce violaciones de unicidad de PostgreSQL (23505) a conflictos de dominio (409),
    /// cubriendo las carreras concurrentes que el chequeo previo no puede detectar.
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
            var mensaje = constraint switch
            {
                var c when c.Contains("Cmc7", StringComparison.OrdinalIgnoreCase)
                    => constraint.Contains("echeq", StringComparison.OrdinalIgnoreCase)
                        ? "Ya existe un echeq con ese CMC7."
                        : "Ya existe un cheque físico con ese CMC7.",
                var c when c.Contains("IdEcheq", StringComparison.OrdinalIgnoreCase)
                    => "Ya existe un echeq con ese IDECHEQ.",
                var c when c.Contains("cbu_chequera_numero", StringComparison.OrdinalIgnoreCase)
                    => "Ya existe un echeq con ese número de chequera.",
                var c when c.Contains("Cbu", StringComparison.OrdinalIgnoreCase)
                    => "Ya existe una cuenta con ese CBU.",
                var c when c.Contains("CuentaId", StringComparison.OrdinalIgnoreCase)
                    => "Ya existe esa chequera para la cuenta.",
                var c when c.Contains("Idempotencia", StringComparison.OrdinalIgnoreCase)
                    => "La Idempotency-Key ya fue registrada por otra request concurrente.",
                _ => "Se detectó un conflicto de unicidad al guardar los datos."
            };

            throw new ConflictoDominioException(mensaje);
        }
    }
}
