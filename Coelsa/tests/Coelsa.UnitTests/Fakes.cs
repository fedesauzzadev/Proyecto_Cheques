using Coelsa.Application;
using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;

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

    public void Agregar(Echeq entidad) => Datos.Add(entidad);
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
