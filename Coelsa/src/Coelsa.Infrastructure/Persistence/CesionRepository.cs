using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Coelsa.Infrastructure.Persistence;

public class CesionRepository(CoelsaDbContext db) : ICesionRepository
{
    private readonly CoelsaDbContext _db = db;

    public async Task<IReadOnlyList<Cesion>> ListarPorEcheqAsync(Guid echeqId, CancellationToken ct)
        => await _db.Cesiones.AsNoTracking()
            .Where(c => c.EcheqId == echeqId)
            .OrderBy(c => c.Numero)
            .ToListAsync(ct);

    public Task<Cesion?> ObtenerPorNumeroAsync(Guid echeqId, int numero, CancellationToken ct)
        => _db.Cesiones.FirstOrDefaultAsync(c => c.EcheqId == echeqId && c.Numero == numero, ct);

    public Task<bool> ExisteSolicitadaAsync(Guid echeqId, CancellationToken ct)
        => _db.Cesiones.AsNoTracking()
            .AnyAsync(c => c.EcheqId == echeqId && c.Estado == EstadoCesion.Solicitada, ct);

    public void Agregar(Cesion cesion) => _db.Cesiones.Add(cesion);
}
