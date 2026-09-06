using IrmaRios.Cartera.Application.Puertos;
using IrmaRios.Cartera.Domain;
using IrmaRios.Cartera.Domain.Entidades;

namespace IrmaRios.Cartera.UnitTests;

public class CarteraRepositoryFake : ICarteraRepository
{
    public List<InstrumentoCartera> Instrumentos { get; } = [];

    public List<Deposito> Depositos { get; } = [];

    public Task<InstrumentoCartera?> ObtenerPorIdentificadorAsync(string identificador, CancellationToken ct)
        => Task.FromResult(Instrumentos.FirstOrDefault(i => i.Identificador == identificador));

    public Task<bool> ExisteEnCarteraAsync(TipoInstrumento tipo, string identificador, CancellationToken ct)
        => Task.FromResult(Instrumentos.Any(i =>
            i.Tipo == tipo && i.Identificador == identificador && i.Estado == EstadoCartera.EnCartera));

    public Task<InstrumentoCartera?> ObtenerEnCarteraAsync(TipoInstrumento tipo, string identificador, CancellationToken ct)
        => Task.FromResult(Instrumentos.FirstOrDefault(i =>
            i.Tipo == tipo && i.Identificador == identificador && i.Estado == EstadoCartera.EnCartera));

    public Task<(IReadOnlyList<InstrumentoCartera> Items, int TotalCount)> ListarPorTitularAsync(
        TipoInstrumento tipo, string cuitTitular, int page, int pageSize, CancellationToken ct)
    {
        var filtrados = Instrumentos
            .Where(i => i.TitularCuit == cuitTitular && i.Tipo == tipo && i.Estado == EstadoCartera.EnCartera)
            .OrderBy(i => i.FechaVencimiento)
            .ToList();

        var items = filtrados.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult<(IReadOnlyList<InstrumentoCartera>, int)>((items, filtrados.Count));
    }

    public void Agregar(InstrumentoCartera instrumento) => Instrumentos.Add(instrumento);

    public void Agregar(Deposito deposito) => Depositos.Add(deposito);
}

public class ClearingFake : IClearing
{
    /// <summary>Instrumentos que "existen" en el clearing, indexados por identificador.</summary>
    public Dictionary<string, InstrumentoClearing> Datos { get; } = [];

    public Task<InstrumentoClearing?> ObtenerAsync(TipoInstrumento tipo, string identificador, CancellationToken ct)
        => Task.FromResult(Datos.TryGetValue(identificador, out var dato) ? dato : null);
}

public class EmpresasFake : IEmpresas
{
    public HashSet<string> CuitsExistentes { get; } = [];

    public Task<bool> ExisteAsync(string cuit, CancellationToken ct)
        => Task.FromResult(CuitsExistentes.Contains(cuit));
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

public class AlmacenIdempotenciaFake : IAlmacenIdempotencia
{
    public Dictionary<string, RegistroIdempotencia> Registros { get; } = [];

    public Task<RegistroIdempotencia?> ObtenerAsync(string key, CancellationToken ct)
        => Task.FromResult(Registros.TryGetValue(key, out var registro) ? registro : null);

    public void Registrar(string key, string bodyHash, string responseJson)
        => Registros[key] = new RegistroIdempotencia(key, bodyHash, responseJson);
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
