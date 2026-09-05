using Coelsa.Application.Puertos;
using Coelsa.Domain.Entidades;
using Coelsa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Coelsa.Infrastructure.Persistence;

public class ChequeFisicoRepository(CoelsaDbContext db) : IInstrumentoRepository<ChequeFisico>
{
    private readonly CoelsaDbContext _db = db;

    public Task<bool> ExistePorIdentificadorAsync(string identificador, CancellationToken ct)
        => _db.ChequesFisicos.AsNoTracking().AnyAsync(c => c.Cmc7 == identificador, ct);

    public Task<ChequeFisico?> ObtenerPorIdentificadorAsync(string identificador, CancellationToken ct)
        => _db.ChequesFisicos.FirstOrDefaultAsync(c => c.Cmc7 == identificador && c.Activo, ct);

    public async Task<(IReadOnlyList<ChequeFisico> Items, int TotalCount)> ListarPorCuitAsync(
        string cuit, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.ChequesFisicos
            .AsNoTracking()
            .Where(c => c.Activo && (c.CuitLibrador == cuit || c.CuitBeneficiario == cuit));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.FechaCreacion)
            .ThenBy(c => c.Cmc7)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public void Agregar(ChequeFisico entidad) => _db.ChequesFisicos.Add(entidad);
}
