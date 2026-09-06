using IrmaRios.Cartera.Domain;
using IrmaRios.Cartera.Domain.Entidades;

namespace IrmaRios.Cartera.Application.Puertos;

public interface ICarteraRepository
{
    Task<InstrumentoCartera?> ObtenerPorIdentificadorAsync(string identificador, CancellationToken ct);

    Task<bool> ExisteEnCarteraAsync(TipoInstrumento tipo, string identificador, CancellationToken ct);

    Task<InstrumentoCartera?> ObtenerEnCarteraAsync(TipoInstrumento tipo, string identificador, CancellationToken ct);

    Task<(IReadOnlyList<InstrumentoCartera> Items, int TotalCount)> ListarPorTitularAsync(
        Domain.TipoInstrumento tipo, string cuitTitular, int page, int pageSize, CancellationToken ct);

    void Agregar(InstrumentoCartera instrumento);

    void Agregar(Deposito deposito);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}

/// <summary>Registro de idempotencia persistido junto al depósito en la misma transacción.</summary>
public sealed record RegistroIdempotencia(string Key, string BodyHash, string ResponseJson);

public interface IAlmacenIdempotencia
{
    Task<RegistroIdempotencia?> ObtenerAsync(string key, CancellationToken ct);

    void Registrar(string key, string bodyHash, string responseJson);
}

/// <summary>
/// Antipuerto del clearing (simulador COELSA): única vía de consulta de la fuente
/// de verdad de instrumentos. Adaptado por HTTP en Infrastructure.
/// </summary>
public interface IClearing
{
    /// <summary>Datos del instrumento en el clearing; null si no existe.</summary>
    Task<InstrumentoClearing?> ObtenerAsync(Domain.TipoInstrumento tipo, string identificador, CancellationToken ct);
}

public sealed record InstrumentoClearing(
    string Identificador,
    string CuitLibrador,
    string CuitBeneficiario,
    decimal Monto,
    Domain.Moneda Moneda,
    DateOnly FechaVencimiento,
    Domain.EstadoClearing Estado);

/// <summary>Antipuerto del servicio Identidad: existencia de la empresa depositante.</summary>
public interface IEmpresas
{
    Task<bool> ExisteAsync(string cuit, CancellationToken ct);
}

/// <summary>
/// Puerto del cache de consultas de cartera (adaptado por Redis, fail-open):
/// mismo contrato versionado por CUIT que usa el clearing.
/// </summary>
public interface IGestorCacheConsultas
{
    Task<long> ObtenerVersionAsync(Domain.TipoInstrumento tipo, string cuit, CancellationToken ct);

    Task InvalidarAsync(Domain.TipoInstrumento tipo, string cuit, CancellationToken ct);

    Task<T> ObtenerOCargarAsync<T>(
        string clave,
        Func<CancellationToken, Task<T>> cargar,
        CancellationToken ct) where T : class;
}
