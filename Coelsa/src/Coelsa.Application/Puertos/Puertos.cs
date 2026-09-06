using Coelsa.Domain;
using Coelsa.Domain.Entidades;

namespace Coelsa.Application.Puertos;

/// <summary>
/// Puerto de persistencia para cada tipo de instrumento (adaptado por EF Core en Infrastructure).
/// </summary>
public interface IInstrumentoRepository<TEntidad>
    where TEntidad : class, IInstrumento
{
    Task<TEntidad?> ObtenerPorIdentificadorAsync(string identificador, CancellationToken ct);

    Task<bool> ExistePorIdentificadorAsync(string identificador, CancellationToken ct);

    Task<(IReadOnlyList<TEntidad> Items, int TotalCount)> ListarPorCuitAsync(
        string cuit, int page, int pageSize, CancellationToken ct);

    void Agregar(TEntidad entidad);
}

public interface IEcheqRepository : IInstrumentoRepository<Echeq>
{
    Task<bool> ExisteCmc7Async(string cmc7, CancellationToken ct);

    Task<bool> ExisteIdEcheqAsync(string idEcheq, CancellationToken ct);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}

/// <summary>
/// Registro de idempotencia persistido junto al instrumento en la misma transacción.
/// </summary>
public sealed record RegistroIdempotencia(string Key, string BodyHash, string ResponseJson);

public interface IAlmacenIdempotencia
{
    Task<RegistroIdempotencia?> ObtenerAsync(string key, CancellationToken ct);

    /// <summary>Encola el registro; se persiste con el UnitOfWork de la creación.</summary>
    void Registrar(string key, TipoInstrumento tipo, string bodyHash, string responseJson);
}

/// <summary>
/// Puerto del cache de consultas por CUIT (adaptado por Redis en Infrastructure).
/// La implementación debe ser tolerante a fallos (fail-open) y proteger contra stampede.
/// </summary>
public interface IGestorCacheConsultas
{
    /// <summary>Versión de invalidación por CUIT; 0 si no hay versión o el cache no responde.</summary>
    Task<long> ObtenerVersionAsync(TipoInstrumento tipo, string cuit, CancellationToken ct);

    /// <summary>Incrementa la versión del CUIT (invalida todas las páginas cacheadas de ese CUIT).</summary>
    Task InvalidarAsync(TipoInstrumento tipo, string cuit, CancellationToken ct);

    /// <summary>
    /// Obtiene de cache o carga desde la fuente, con protección contra stampede.
    /// Si el cache no está disponible, ejecuta la carga directamente (fail-open).
    /// </summary>
    Task<T> ObtenerOCargarAsync<T>(
        string clave,
        Func<CancellationToken, Task<T>> cargar,
        CancellationToken ct) where T : class;
}
