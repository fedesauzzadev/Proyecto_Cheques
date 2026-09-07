using Coelsa.Application.Puertos;
using Coelsa.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Coelsa.Infrastructure.Persistence;

public class CuentaRepository(CoelsaDbContext db) : ICuentaRepository
{
    private readonly CoelsaDbContext _db = db;

    public Task<bool> ExisteCbuAsync(string cbu, CancellationToken ct)
        => _db.Cuentas.AsNoTracking().AnyAsync(c => c.Cbu == cbu, ct);

    public Task<Cuenta?> ObtenerPorCbuAsync(string cbu, CancellationToken ct)
        => _db.Cuentas.FirstOrDefaultAsync(c => c.Cbu == cbu && c.Activa, ct);

    public async Task<IReadOnlyList<Cuenta>> ListarPorCuitAsync(string cuit, CancellationToken ct)
        => await _db.Cuentas
            .AsNoTracking()
            .Where(c => c.Activa && c.CuitTitular == cuit)
            .OrderBy(c => c.Cbu)
            .ToListAsync(ct);

    public void Agregar(Cuenta entidad) => _db.Cuentas.Add(entidad);
}
