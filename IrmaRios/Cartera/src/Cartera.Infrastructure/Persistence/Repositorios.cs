using IrmaRios.Cartera.Application.Puertos;
using IrmaRios.Cartera.Domain;
using IrmaRios.Cartera.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IrmaRios.Cartera.Infrastructure.Persistence;

public class CarteraRepository(CarteraDbContext db) : ICarteraRepository
{
    private readonly CarteraDbContext _db = db;

    public async Task<Domain.Entidades.InstrumentoCartera?> ObtenerPorIdentificadorAsync(
        string identificador, CancellationToken ct)
        => await _db.Instrumentos.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Identificador == identificador, ct);

    public Task<bool> ExisteEnCarteraAsync(TipoInstrumento tipo, string identificador, CancellationToken ct)
        => _db.Instrumentos.AnyAsync(
            i => i.Tipo == tipo && i.Identificador == identificador && i.Estado == Domain.Entidades.EstadoCartera.EnCartera, ct);

    public async Task<Domain.Entidades.InstrumentoCartera?> ObtenerEnCarteraAsync(
        TipoInstrumento tipo, string identificador, CancellationToken ct)
        => await _db.Instrumentos.AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.Tipo == tipo && i.Identificador == identificador
                     && i.Estado == Domain.Entidades.EstadoCartera.EnCartera, ct);

    public async Task<(IReadOnlyList<Domain.Entidades.InstrumentoCartera> Items, int TotalCount)> ListarPorTitularAsync(
        TipoInstrumento tipo, string cuitTitular, int page, int pageSize, CancellationToken ct)
    {
        var consulta = _db.Instrumentos.AsNoTracking()
            .Where(i => i.TitularCuit == cuitTitular && i.Tipo == tipo
                        && i.Estado == Domain.Entidades.EstadoCartera.EnCartera)
            .OrderBy(i => i.FechaVencimiento).ThenBy(i => i.Identificador);

        var totalCount = await consulta.CountAsync(ct);
        var items = await consulta.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, totalCount);
    }

    public void Agregar(Domain.Entidades.InstrumentoCartera instrumento) => _db.Instrumentos.Add(instrumento);

    public void Agregar(Domain.Entidades.Deposito deposito) => _db.Depositos.Add(deposito);
}

/// <summary>Idempotencia en la misma transacción que el depósito (atómico).</summary>
public class AlmacenIdempotencia(CarteraDbContext db) : IAlmacenIdempotencia
{
    private readonly CarteraDbContext _db = db;

    public async Task<RegistroIdempotencia?> ObtenerAsync(string key, CancellationToken ct)
    {
        var entrada = await _db.IdempotenciaKeys.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Key == key, ct);

        return entrada is null
            ? null
            : new RegistroIdempotencia(entrada.Key, entrada.BodyHash, entrada.ResponseJson);
    }

    public void Registrar(string key, string bodyHash, string responseJson)
    {
        _db.IdempotenciaKeys.Add(new EntradaIdempotencia
        {
            Id = Guid.NewGuid(),
            Key = key,
            BodyHash = bodyHash,
            ResponseJson = responseJson,
            FechaCreacion = DateTime.UtcNow
        });
    }
}
