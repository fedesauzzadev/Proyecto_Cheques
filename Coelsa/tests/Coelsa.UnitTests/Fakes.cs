using Coelsa.Application;
using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Coelsa.Domain.Validaciones;
using Coelsa.Domain.ValueObjects;

namespace Coelsa.UnitTests;

public class AlmacenIdempotenciaFake : IAlmacenIdempotencia
{
    public Dictionary<string, RegistroIdempotencia> Registros { get; } = [];

    public Task<RegistroIdempotencia?> ObtenerAsync(string key, CancellationToken ct)
        => Task.FromResult(Registros.TryGetValue(key, out var registro) ? registro : null);

    public void Registrar(string key, TipoInstrumento tipo, string bodyHash, string responseJson)
        => Registros[key] = new RegistroIdempotencia(key, bodyHash, responseJson);
}

public class GestorCacheFake : IGestorCacheConsultas
{
    public Dictionary<string, long> Versiones { get; } = [];
    public List<string> Invalidaciones { get; } = [];

    public Task<long> ObtenerVersionAsync(TipoInstrumento tipo, string cuit, CancellationToken ct)
        => Task.FromResult(Versiones.TryGetValue(Clave(tipo, cuit), out var v) ? v : 0L);

    public Task InvalidarAsync(TipoInstrumento tipo, string cuit, CancellationToken ct)
    {
        var clave = Clave(tipo, cuit);
        Versiones[clave] = (Versiones.TryGetValue(clave, out var v) ? v : 0L) + 1;
        Invalidaciones.Add(clave);
        return Task.CompletedTask;
    }

    public async Task<T> ObtenerOCargarAsync<T>(string clave, Func<CancellationToken, Task<T>> cargar, CancellationToken ct)
        where T : class
        => await cargar(ct);

    private static string Clave(TipoInstrumento tipo, string cuit)
        => $"{(tipo == TipoInstrumento.ChequeFisico ? "cheques" : "echeqs")}:{cuit}";
}

public class RepositorioChequesFake : IInstrumentoRepository<ChequeFisico>
{
    public List<ChequeFisico> Datos { get; } = [];

    public Task<bool> ExistePorIdentificadorAsync(string identificador, CancellationToken ct)
        => Task.FromResult(Datos.Any(c => c.Cmc7 == identificador));

    public Task<ChequeFisico?> ObtenerPorIdentificadorAsync(string identificador, CancellationToken ct)
        => Task.FromResult(Datos.FirstOrDefault(c => c.Cmc7 == identificador && c.Activo));

    public Task<(IReadOnlyList<ChequeFisico> Items, int TotalCount)> ListarPorCuitAsync(
        string cuit, int page, int pageSize, CancellationToken ct)
    {
        var filtrados = Datos
            .Where(c => c.Activo && (c.CuitLibrador == cuit || c.CuitBeneficiario == cuit))
            .OrderByDescending(c => c.FechaCreacion)
            .ThenBy(c => c.Cmc7)
            .ToList();

        var items = filtrados.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult<(IReadOnlyList<ChequeFisico>, int)>((items, filtrados.Count));
    }

    public void Agregar(ChequeFisico entidad) => Datos.Add(entidad);
}

public class RepositorioEcheqsFake : IEcheqRepository
{
    public List<Echeq> Datos { get; } = [];

    public Task<bool> ExistePorIdentificadorAsync(string identificador, CancellationToken ct)
        => Task.FromResult(Datos.Any(e => e.IdEcheq == identificador));

    public Task<Echeq?> ObtenerPorIdentificadorAsync(string identificador, CancellationToken ct)
        => Task.FromResult(Datos.FirstOrDefault(e => e.IdEcheq == identificador && e.Activo));

    public Task<bool> ExisteCmc7Async(string cmc7, CancellationToken ct)
        => Task.FromResult(Datos.Any(e => e.Cmc7 == cmc7));

    public Task<bool> ExisteIdEcheqAsync(string idEcheq, CancellationToken ct)
        => Task.FromResult(Datos.Any(e => e.IdEcheq == idEcheq));

    public Task<IReadOnlyList<Echeq>> ListarCustodiasVencidasAsync(DateOnly hoy, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Echeq>>(Datos
            .Where(e => e.Activo && e.Estado == EstadoInstrumento.EnCustodia && e.FechaVencimiento <= hoy)
            .OrderBy(e => e.FechaVencimiento)
            .ToList());

    public Task<(IReadOnlyList<Echeq> Items, int TotalCount)> ListarPorCuitAsync(
        string cuit, int page, int pageSize, CancellationToken ct)
    {
        var filtrados = Datos
            .Where(e => e.Activo && (e.CuitLibrador == cuit || e.CuitBeneficiario == cuit))
            .OrderByDescending(e => e.FechaCreacion)
            .ThenBy(e => e.IdEcheq)
            .ToList();

        var items = filtrados.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult<(IReadOnlyList<Echeq>, int)>((items, filtrados.Count));
    }

    public Task<(IReadOnlyList<Echeq> Items, int TotalCount)> ListarFiltradoAsync(
        string cuit, Application.Dtos.FiltrosEcheq filtros, int page, int pageSize, CancellationToken ct)
    {
        var query = Datos.Where(e => e.Activo && (e.CuitLibrador == cuit || e.CuitBeneficiario == cuit));

        if (filtros.Cbu is not null)
        {
            query = query.Where(e => e.CbuEmisor == filtros.Cbu);
        }

        if (filtros.EstadoParseado is not null)
        {
            query = query.Where(e => e.Estado == filtros.EstadoParseado);
        }

        if (filtros.DesdeEmision is not null)
        {
            query = query.Where(e => e.FechaEmision >= filtros.DesdeEmision);
        }

        if (filtros.HastaEmision is not null)
        {
            query = query.Where(e => e.FechaEmision <= filtros.HastaEmision);
        }

        if (filtros.DesdeVencimiento is not null)
        {
            query = query.Where(e => e.FechaVencimiento >= filtros.DesdeVencimiento);
        }

        if (filtros.HastaVencimiento is not null)
        {
            query = query.Where(e => e.FechaVencimiento <= filtros.HastaVencimiento);
        }

        if (filtros.NumeroCheque is not null)
        {
            query = query.Where(e => e.NumeroCheque == filtros.NumeroCheque);
        }

        var filtrados = query
            .OrderByDescending(e => e.FechaCreacion)
            .ThenBy(e => e.IdEcheq)
            .ToList();

        var items = filtrados.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult<(IReadOnlyList<Echeq>, int)>((items, filtrados.Count));
    }

    public void Agregar(Echeq entidad) => Datos.Add(entidad);
}

public class RepositorioCesionesFake : ICesionRepository
{
    public List<Cesion> Datos { get; } = [];

    public Task<IReadOnlyList<Cesion>> ListarPorEcheqAsync(Guid echeqId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Cesion>>(Datos
            .Where(c => c.EcheqId == echeqId)
            .OrderBy(c => c.Numero)
            .ToList());

    public Task<Cesion?> ObtenerPorNumeroAsync(Guid echeqId, int numero, CancellationToken ct)
        => Task.FromResult(Datos.FirstOrDefault(c => c.EcheqId == echeqId && c.Numero == numero));

    public Task<bool> ExisteSolicitadaAsync(Guid echeqId, CancellationToken ct)
        => Task.FromResult(Datos.Any(c => c.EcheqId == echeqId && c.Estado == EstadoCesion.Solicitada));

    public void Agregar(Cesion entidad) => Datos.Add(entidad);
}

public class RepositorioEndososFake : IEndosoRepository
{
    public List<Endoso> Datos { get; } = [];

    public Task<IReadOnlyList<Endoso>> ListarPorEcheqAsync(Guid echeqId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Endoso>>(Datos
            .Where(e => e.EcheqId == echeqId)
            .OrderBy(e => e.Orden)
            .ToList());

    public Task<Endoso?> ObtenerPorOrdenAsync(Guid echeqId, int orden, CancellationToken ct)
        => Task.FromResult(Datos.FirstOrDefault(e => e.EcheqId == echeqId && e.Orden == orden));

    public void Agregar(Endoso endoso) => Datos.Add(endoso);
}

public class RepositorioDevolucionesFake : IDevolucionRepository
{
    public List<Devolucion> Datos { get; } = [];

    public Task<IReadOnlyList<Devolucion>> ListarPorEcheqAsync(Guid echeqId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Devolucion>>(Datos
            .Where(d => d.EcheqId == echeqId)
            .OrderBy(d => d.Numero)
            .ToList());

    public Task<Devolucion?> ObtenerPorNumeroAsync(Guid echeqId, int numero, CancellationToken ct)
        => Task.FromResult(Datos.FirstOrDefault(d => d.EcheqId == echeqId && d.Numero == numero));

    public Task<bool> ExisteSolicitadaAsync(Guid echeqId, CancellationToken ct)
        => Task.FromResult(Datos.Any(d => d.EcheqId == echeqId && d.Estado == EstadoDevolucion.Solicitada));

    public void Agregar(Devolucion devolucion) => Datos.Add(devolucion);
}

public class UnitOfWorkFake : IUnitOfWork
{
    public int Guardadas { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken ct)
    {
        Guardadas++;
        return Task.FromResult(1);
    }
}

public class GeneradorIdEcheqFijo : IGeneradorIdEcheq
{
    private int _contador;
    private const string Letras = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    // IDs deterministas de 11 letras para tests (AAA..., BAA..., etc.).
    public string Generar()
    {
        _contador++;
        var chars = new char[11];
        var n = _contador;
        for (var i = 10; i >= 0; i--)
        {
            chars[i] = Letras[n % 26];
            n /= 26;
        }
        return new string(chars);
    }
}

public class RepositorioCuentasFake : ICuentaRepository
{
    public List<Cuenta> Datos { get; } = [];

    public Task<bool> ExisteCbuAsync(string cbu, CancellationToken ct)
        => Task.FromResult(Datos.Any(c => c.Cbu == cbu));

    public Task<Cuenta?> ObtenerPorCbuAsync(string cbu, CancellationToken ct)
        => Task.FromResult(Datos.FirstOrDefault(c => c.Cbu == cbu && c.Activa));

    public Task<IReadOnlyList<Cuenta>> ListarPorCuitAsync(string cuit, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Cuenta>>(Datos
            .Where(c => c.Activa && c.CuitTitular == cuit)
            .OrderBy(c => c.Cbu)
            .ToList());

    public void Agregar(Cuenta entidad) => Datos.Add(entidad);
}

public class RepositorioChequerasFake : IChequeraRepository
{
    public List<Chequera> Datos { get; } = [];

    public Task<Chequera?> ObtenerAsync(Guid cuentaId, int numero, CancellationToken ct)
        => Task.FromResult(Datos.FirstOrDefault(c => c.CuentaId == cuentaId && c.Numero == numero && c.Activa));

    public Task<IReadOnlyList<Chequera>> ListarPorCuentaAsync(Guid cuentaId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Chequera>>(Datos
            .Where(c => c.CuentaId == cuentaId && c.Activa)
            .OrderBy(c => c.Numero)
            .ToList());

    public Task<int> ContarPorCuentaAsync(Guid cuentaId, CancellationToken ct)
        => Task.FromResult(Datos.Count(c => c.CuentaId == cuentaId));

    public Task<Chequera?> ObtenerConLugarAsync(Guid cuentaId, CancellationToken ct)
        => Task.FromResult(Datos
            .Where(c => c.CuentaId == cuentaId && c.TieneLugar)
            .OrderBy(c => c.Numero)
            .FirstOrDefault());

    public void Agregar(Chequera entidad) => Datos.Add(entidad);
}

/// <summary>
/// Fábrica de echeqs válidos para tests: CBU fijo + CMC7 derivado coherente.
/// </summary>
public static class FabricaEcheqs
{
    public static string Cbu => ValidadorCbu.Crear("011", "0001", "0000000000001");

    public static Echeq Crear(
        string idEcheq = "ABCDEFGHIJK",
        int numeroChequera = 1,
        int numeroCheque = 1,
        Caracter caracter = Caracter.AlaOrden,
        string baseLibrador = "2012345678",
        string baseBeneficiario = "2787654321",
        decimal monto = 250_000m,
        Moneda moneda = Moneda.Dolares,
        DateOnly? emision = null,
        DateOnly? vencimiento = null)
    {
        var hoy = new DateOnly(2026, 9, 5);
        var cbu = Cbu;
        return Echeq.Crear(
            idEcheq,
            cbu,
            numeroChequera,
            numeroCheque,
            caracter,
            TipoDocumento.Cuit,
            "Librador Demo S.A.",
            "Beneficiario Demo S.A.",
            Cmc7.Derivar(cbu, numeroCheque).Valor,
            ValidadorCuit.Completar(baseLibrador),
            ValidadorCuit.Completar(baseBeneficiario),
            monto,
            moneda,
            emision ?? hoy,
            null,
            vencimiento ?? hoy.AddDays(30),
            hoy);
    }
}
