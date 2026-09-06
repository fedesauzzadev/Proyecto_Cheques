using Coelsa.Application.Puertos;
using Coelsa.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Coelsa.Infrastructure.Persistence;

public class EndosoRepository(CoelsaDbContext db) : IEndosoRepository
{
    private readonly CoelsaDbContext _db = db;

    public async Task<IReadOnlyList<Endoso>> ListarPorEcheqAsync(Guid echeqId, CancellationToken ct)
        => await _db.Endosos.AsNoTracking()
            .Where(e => e.EcheqId == echeqId)
            .OrderBy(e => e.Orden)
            .ToListAsync(ct);

    public Task<Endoso?> ObtenerPorOrdenAsync(Guid echeqId, int orden, CancellationToken ct)
        => _db.Endosos.FirstOrDefaultAsync(e => e.EcheqId == echeqId && e.Orden == orden, ct);

    public void Agregar(Endoso endoso) => _db.Endosos.Add(endoso);
}
