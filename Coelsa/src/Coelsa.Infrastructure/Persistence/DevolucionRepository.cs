using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Coelsa.Infrastructure.Persistence;

public class DevolucionRepository(CoelsaDbContext db) : IDevolucionRepository
{
    private readonly CoelsaDbContext _db = db;

    public async Task<IReadOnlyList<Devolucion>> ListarPorEcheqAsync(Guid echeqId, CancellationToken ct)
        => await _db.Devoluciones.AsNoTracking()
            .Where(d => d.EcheqId == echeqId)
            .OrderBy(d => d.Numero)
            .ToListAsync(ct);

    public Task<Devolucion?> ObtenerPorNumeroAsync(Guid echeqId, int numero, CancellationToken ct)
        => _db.Devoluciones.FirstOrDefaultAsync(d => d.EcheqId == echeqId && d.Numero == numero, ct);

    public Task<bool> ExisteSolicitadaAsync(Guid echeqId, CancellationToken ct)
        => _db.Devoluciones.AsNoTracking()
            .AnyAsync(d => d.EcheqId == echeqId && d.Estado == EstadoDevolucion.Solicitada, ct);

    public void Agregar(Devolucion devolucion) => _db.Devoluciones.Add(devolucion);
}
