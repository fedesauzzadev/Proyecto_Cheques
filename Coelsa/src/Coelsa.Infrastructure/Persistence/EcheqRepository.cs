using Coelsa.Application.Puertos;
using Coelsa.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Coelsa.Infrastructure.Persistence;

public class EcheqRepository(CoelsaDbContext db) : IEcheqRepository
{
    private readonly CoelsaDbContext _db = db;

    public Task<bool> ExistePorIdentificadorAsync(string identificador, CancellationToken ct)
        => _db.Echeqs.AsNoTracking().AnyAsync(e => e.IdEcheq == identificador, ct);

    public Task<Echeq?> ObtenerPorIdentificadorAsync(string identificador, CancellationToken ct)
        => _db.Echeqs.FirstOrDefaultAsync(e => e.IdEcheq == identificador && e.Activo, ct);

    public Task<bool> ExisteCmc7Async(string cmc7, CancellationToken ct)
        => _db.Echeqs.AsNoTracking().AnyAsync(e => e.Cmc7 == cmc7, ct);

    public Task<bool> ExisteIdEcheqAsync(string idEcheq, CancellationToken ct)
        => _db.Echeqs.AsNoTracking().AnyAsync(e => e.IdEcheq == idEcheq, ct);

    public async Task<IReadOnlyList<Echeq>> ListarCustodiasVencidasAsync(DateOnly hoy, CancellationToken ct)
        => await _db.Echeqs
            .Where(e => e.Activo
                && e.Estado == Domain.EstadoInstrumento.EnCustodia
                && e.FechaVencimiento <= hoy)
            .OrderBy(e => e.FechaVencimiento)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Echeq> Items, int TotalCount)> ListarPorCuitAsync(
        string cuit, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Echeqs
            .AsNoTracking()
            .Where(e => e.Activo && (e.CuitLibrador == cuit || e.CuitBeneficiario == cuit));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.FechaCreacion)
            .ThenBy(e => e.IdEcheq)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public void Agregar(Echeq entidad) => _db.Echeqs.Add(entidad);
}
