using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Coelsa.Application.Puertos;
using Coelsa.Domain;

namespace Coelsa.Application.Estrategias;

/// <summary>
/// Flujo común de creación idempotente (SPEC RF-02):
/// 1. Replay: misma key + mismo body → respuesta original (EsReplay = true).
/// 2. Conflicto: misma key + otro body → 409.
/// 3. Unicidad de negocio (CMC7 / CUD) → 409.
/// 4. Persistencia atómica (instrumento + registro de idempotencia en la misma transacción).
/// 5. Invalidación de cache por CUIT (librador y beneficiario).
/// </summary>
public abstract class CreacionInstrumentoStrategy<TRequest, TResponse> : ICrearInstrumentoStrategy<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    protected static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    protected IAlmacenIdempotencia Idempotencia { get; }

    private readonly IGestorCacheConsultas _cache;

    protected CreacionInstrumentoStrategy(IAlmacenIdempotencia idempotencia, IGestorCacheConsultas cache)
    {
        Idempotencia = idempotencia;
        _cache = cache;
    }

    protected abstract TipoInstrumento Tipo { get; }

    /// <summary>Mensaje de conflicto cuando el identificador de negocio ya existe.</summary>
    protected abstract string MensajeDuplicado(TRequest request);

    protected abstract Task<bool> ExisteDuplicadoDeNegocioAsync(TRequest request, CancellationToken ct);

    /// <summary>Persiste instrumento + registro de idempotencia y guarda con el UnitOfWork.</summary>
    protected abstract Task<TResponse> PersistirAsync(TRequest request, string idempotencyKey, string bodyHash, CancellationToken ct);

    /// <summary>CUITs cuyo cache de listado debe invalidarse (librador y beneficiario).</summary>
    protected abstract IReadOnlyCollection<string> CuitsAfectados(TRequest request);

    public async Task<ResultadoCreacion<TResponse>> CrearAsync(TRequest request, string idempotencyKey, CancellationToken ct)
    {
        var bodyHash = HashearBody(request);

        var registroExistente = await Idempotencia.ObtenerAsync(idempotencyKey, ct);
        if (registroExistente is not null)
        {
            return ReintentarOMConflicto(registroExistente, bodyHash, idempotencyKey);
        }

        if (await ExisteDuplicadoDeNegocioAsync(request, ct))
        {
            throw new ConflictoDominioException(MensajeDuplicado(request));
        }

        TResponse respuesta;
        try
        {
            respuesta = await PersistirAsync(request, idempotencyKey, bodyHash, ct);
        }
        catch (ConflictoDominioException)
        {
            // Carrera concurrente: otro request insertó primero (misma key u otro identificador igual).
            registroExistente = await Idempotencia.ObtenerAsync(idempotencyKey, ct);
            if (registroExistente is not null)
            {
                return ReintentarOMConflicto(registroExistente, bodyHash, idempotencyKey);
            }

            throw;
        }

        foreach (var cuit in CuitsAfectados(request))
        {
            await _cache.InvalidarAsync(Tipo, cuit, ct);
        }

        return new ResultadoCreacion<TResponse>(respuesta, EsReplay: false);
    }

    private ResultadoCreacion<TResponse> ReintentarOMConflicto(RegistroIdempotencia registro, string bodyHash, string idempotencyKey)
    {
        if (!string.Equals(registro.BodyHash, bodyHash, StringComparison.Ordinal))
        {
            throw new ConflictoDominioException(
                $"La Idempotency-Key '{idempotencyKey}' ya fue utilizada con otro cuerpo de request.");
        }

        var respuesta = JsonSerializer.Deserialize<TResponse>(registro.ResponseJson, JsonOpts)
            ?? throw new ValidacionException("El registro de idempotencia contiene una respuesta inválida.");

        return new ResultadoCreacion<TResponse>(respuesta, EsReplay: true);
    }

    private static string HashearBody(TRequest request)
    {
        var json = JsonSerializer.Serialize(request, JsonOpts);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }
}
