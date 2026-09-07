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

    /// <summary>Echeqs activos en custodia cuya fecha de vencimiento ya pasó.</summary>
    Task<IReadOnlyList<Echeq>> ListarCustodiasVencidasAsync(DateOnly hoy, CancellationToken ct);

    /// <summary>Listado por CUIT con filtros opcionales (Fase B6).</summary>
    Task<(IReadOnlyList<Echeq> Items, int TotalCount)> ListarFiltradoAsync(
        string cuit, Dtos.FiltrosEcheq filtros, int page, int pageSize, CancellationToken ct);
}

/// <summary>
/// Puerto de persistencia de cuentas corrientes emisoras de echeqs (Fase B).
/// </summary>
public interface ICuentaRepository
{
    Task<Cuenta?> ObtenerPorCbuAsync(string cbu, CancellationToken ct);

    Task<bool> ExisteCbuAsync(string cbu, CancellationToken ct);

    Task<IReadOnlyList<Cuenta>> ListarPorCuitAsync(string cuit, CancellationToken ct);

    void Agregar(Cuenta entidad);
}

/// <summary>
/// Puerto de persistencia de e-chequeras (Fase B).
/// </summary>
public interface IChequeraRepository
{
    Task<Chequera?> ObtenerAsync(Guid cuentaId, int numero, CancellationToken ct);

    Task<IReadOnlyList<Chequera>> ListarPorCuentaAsync(Guid cuentaId, CancellationToken ct);

    Task<int> ContarPorCuentaAsync(Guid cuentaId, CancellationToken ct);

    /// <summary>Primera chequera vigente de la cuenta con números disponibles.</summary>
    Task<Chequera?> ObtenerConLugarAsync(Guid cuentaId, CancellationToken ct);

    void Agregar(Chequera entidad);
}

/// <summary>
/// Puerto de persistencia de endosos de echeq (adaptado por EF Core).
/// </summary>
public interface IEndosoRepository
{
    Task<IReadOnlyList<Endoso>> ListarPorEcheqAsync(Guid echeqId, CancellationToken ct);

    Task<Endoso?> ObtenerPorOrdenAsync(Guid echeqId, int orden, CancellationToken ct);

    void Agregar(Endoso endoso);
}

/// <summary>
/// Puerto de persistencia de pedidos de devolución de echeqs (adaptado por EF Core).
/// </summary>
public interface IDevolucionRepository
{
    Task<IReadOnlyList<Devolucion>> ListarPorEcheqAsync(Guid echeqId, CancellationToken ct);

    Task<Devolucion?> ObtenerPorNumeroAsync(Guid echeqId, int numero, CancellationToken ct);

    Task<bool> ExisteSolicitadaAsync(Guid echeqId, CancellationToken ct);

    void Agregar(Devolucion devolucion);
}

/// <summary>
/// Puerto de persistencia de cesiones de echeqs "no a la orden" (Fase D1).
/// </summary>
public interface ICesionRepository
{
    Task<IReadOnlyList<Cesion>> ListarPorEcheqAsync(Guid echeqId, CancellationToken ct);

    Task<Cesion?> ObtenerPorNumeroAsync(Guid echeqId, int numero, CancellationToken ct);

    Task<bool> ExisteSolicitadaAsync(Guid echeqId, CancellationToken ct);

    void Agregar(Cesion cesion);
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
