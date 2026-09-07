using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Coelsa.Infrastructure.Persistence;

public class ChequeraRepository(CoelsaDbContext db) : IChequeraRepository
{
    private readonly CoelsaDbContext _db = db;

    public Task<Chequera?> ObtenerAsync(Guid cuentaId, int numero, CancellationToken ct)
        => _db.Chequeras.FirstOrDefaultAsync(c => c.CuentaId == cuentaId && c.Numero == numero && c.Activa, ct);

    public async Task<IReadOnlyList<Chequera>> ListarPorCuentaAsync(Guid cuentaId, CancellationToken ct)
        => await _db.Chequeras
            .AsNoTracking()
            .Where(c => c.CuentaId == cuentaId && c.Activa)
            .OrderBy(c => c.Numero)
            .ToListAsync(ct);

    public Task<int> ContarPorCuentaAsync(Guid cuentaId, CancellationToken ct)
        => _db.Chequeras.AsNoTracking().CountAsync(c => c.CuentaId == cuentaId, ct);

    public Task<Chequera?> ObtenerConLugarAsync(Guid cuentaId, CancellationToken ct)
        => _db.Chequeras
            .Where(c => c.CuentaId == cuentaId
                && c.Activa
                && c.Estado == EstadoChequera.Vigente
                && c.ProximoNumero <= Chequera.CantidadNumeros)
            .OrderBy(c => c.Numero)
            .FirstOrDefaultAsync(ct);

    public void Agregar(Chequera entidad) => _db.Chequeras.Add(entidad);
}
