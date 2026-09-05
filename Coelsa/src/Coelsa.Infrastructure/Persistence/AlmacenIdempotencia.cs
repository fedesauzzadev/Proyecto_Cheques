using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Microsoft.EntityFrameworkCore;

namespace Coelsa.Infrastructure.Persistence;

/// <summary>
/// Almacen de idempotencia sobre el mismo DbContext de la creación:
/// el registro se guarda en la misma transacción que el instrumento (atómico).
/// </summary>
public class AlmacenIdempotencia(CoelsaDbContext db) : IAlmacenIdempotencia
{
    private readonly CoelsaDbContext _db = db;

    public async Task<RegistroIdempotencia?> ObtenerAsync(string key, CancellationToken ct)
    {
        var entrada = await _db.IdempotenciaKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Key == key, ct);

        return entrada is null
            ? null
            : new RegistroIdempotencia(entrada.Key, entrada.BodyHash, entrada.ResponseJson);
    }

    public void Registrar(string key, TipoInstrumento tipo, string bodyHash, string responseJson)
    {
        _db.IdempotenciaKeys.Add(new EntradaIdempotencia
        {
            Key = key,
            BodyHash = bodyHash,
            ResponseJson = responseJson,
            Tipo = tipo,
            FechaCreacion = DateTime.UtcNow
        });
    }
}
