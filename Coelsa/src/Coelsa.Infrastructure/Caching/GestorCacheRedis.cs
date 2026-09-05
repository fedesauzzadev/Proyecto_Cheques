using System.Text.Json;
using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Coelsa.Infrastructure.Caching;

public class CacheOpciones
{
    /// <summary>TTL base de las entradas de cache por CUIT (SPEC sección 7: 30 minutos).</summary>
    public int TtlMinutos { get; set; } = 30;

    /// <summary>Jitter de ±10% aplicado al TTL para evitar expiración sincronizada masiva.</summary>
    public double JitterPorcentaje { get; set; } = 0.10;

    /// <summary>Expiración del lock anti-stampede.</summary>
    public int LockSegundos { get; set; } = 5;

    /// <summary>Espera entre reintentos de lectura mientras otro request recalienta la clave.</summary>
    public int EsperaStampedeMs { get; set; } = 100;

    /// <summary>Cantidad de reintentos de lectura antes de resolver por fail-open.</summary>
    public int ReintentosEspera { get; set; } = 20;
}

/// <summary>
/// Adaptador Redis del cache de consultas por CUIT (SPEC sección 7):
/// - Versionado por CUIT para invalidar escrituras.
/// - TTL con jitter.
/// - Protección anti-stampede con lock distribuido (SETNX) + double-check.
/// - Fail-open: si Redis no responde, la consulta va directo a PostgreSQL.
/// </summary>
public class GestorCacheRedis(
    IConnectionMultiplexer redis,
    IOptions<CacheOpciones> opciones,
    ILogger<GestorCacheRedis> logger) : IGestorCacheConsultas
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis = redis;
    private readonly ILogger<GestorCacheRedis> _logger = logger;
    private readonly CacheOpciones _opciones = opciones.Value;

    public async Task<long> ObtenerVersionAsync(TipoInstrumento tipo, string cuit, CancellationToken ct)
    {
        try
        {
            var valor = await _redis.GetDatabase().StringGetAsync(ClaveVersion(tipo, cuit));
            return valor.TryParse(out long version) ? version : 0L;
        }
        catch (Exception ex)
        {
            AdvertirFailOpen("obtener versión", ex);
            return 0L;
        }
    }

    public async Task InvalidarAsync(TipoInstrumento tipo, string cuit, CancellationToken ct)
    {
        try
        {
            await _redis.GetDatabase().StringIncrementAsync(ClaveVersion(tipo, cuit));
        }
        catch (Exception ex)
        {
            // Fail-open: las claves viejas expiran solas por TTL (stale acotado a TtlMinutos).
            AdvertirFailOpen("invalidar", ex);
        }
    }

    public async Task<T> ObtenerOCargarAsync<T>(
        string clave,
        Func<CancellationToken, Task<T>> cargar,
        CancellationToken ct) where T : class
    {
        try
        {
            var db = _redis.GetDatabase();

            var cacheado = await LeerAsync<T>(db, clave);
            if (cacheado is not null)
            {
                return cacheado;
            }

            var claveLock = $"coelsa:lock:{clave}";
            if (!await db.LockTakeAsync(claveLock, "1", TimeSpan.FromSeconds(_opciones.LockSegundos)))
            {
                return await EsperarRecalentamientoAsync<T>(db, clave, cargar, ct);
            }

            try
            {
                // Double-check: la clave pudo cargarse mientras adquiríamos el lock.
                cacheado = await LeerAsync<T>(db, clave);
                if (cacheado is not null)
                {
                    return cacheado;
                }

                var valor = await cargar(ct);
                await db.StringSetAsync(clave, JsonSerializer.Serialize(valor, JsonOpts), TtlConJitter());
                return valor;
            }
            finally
            {
                await db.LockReleaseAsync(claveLock, "1");
            }
        }
        catch (Exception ex)
        {
            // Fail-open: cualquier falla del cache no debe tumbar la consulta (RNF-13).
            AdvertirFailOpen("leer/escribir", ex);
            return await cargar(ct);
        }
    }

    private async Task<T> EsperarRecalentamientoAsync<T>(
        IDatabase db,
        string clave,
        Func<CancellationToken, Task<T>> cargar,
        CancellationToken ct) where T : class
    {
        for (var intento = 0; intento < _opciones.ReintentosEspera; intento++)
        {
            await Task.Delay(_opciones.EsperaStampedeMs, ct);

            var cacheado = await LeerAsync<T>(db, clave);
            if (cacheado is not null)
            {
                return cacheado;
            }
        }

        // El request que tomó el lock no terminó a tiempo: fail-open para no bloquear.
        return await cargar(ct);
    }

    private async Task<T?> LeerAsync<T>(IDatabase db, string clave) where T : class
    {
        var valor = await db.StringGetAsync(clave);
        return valor.HasValue
            ? JsonSerializer.Deserialize<T>(valor.ToString(), JsonOpts)
            : null;
    }

    private TimeSpan TtlConJitter()
    {
        var factor = 1.0 + (Random.Shared.NextDouble() * 2.0 - 1.0) * _opciones.JitterPorcentaje;
        return TimeSpan.FromMinutes(_opciones.TtlMinutos * factor);
    }

    private static string ClaveVersion(TipoInstrumento tipo, string cuit)
        => $"coelsa:ver:{(tipo == TipoInstrumento.ChequeFisico ? "cheques" : "echeqs")}:{cuit}";

    private void AdvertirFailOpen(string operacion, Exception ex)
        => _logger.LogWarning(ex, "Redis no disponible al intentar {Operacion}; se resuelve con fail-open directo a PostgreSQL.", operacion);
}
