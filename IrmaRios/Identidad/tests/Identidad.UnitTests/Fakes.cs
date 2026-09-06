using IrmaRios.Identidad.Application.Puertos;
using IrmaRios.Identidad.Domain;
using IrmaRios.Identidad.Domain.Entidades;

namespace IrmaRios.Identidad.UnitTests;

public class EmpresaRepositoryFake : IEmpresaRepository
{
    public List<Empresa> Datos { get; } = [];

    public Task<Empresa?> ObtenerPorCuitAsync(string cuit, CancellationToken ct)
        => Task.FromResult(Datos.FirstOrDefault(e => e.Cuit == cuit && !e.Borrado));

    public Task<bool> ExistePorCuitAsync(string cuit, CancellationToken ct)
        => Task.FromResult(Datos.Any(e => e.Cuit == cuit && !e.Borrado));

    public void Agregar(Empresa empresa) => Datos.Add(empresa);
}

public class PersonaRepositoryFake : IPersonaRepository
{
    public List<Persona> Datos { get; } = [];

    public Task<Persona?> ObtenerPorDocumentoAsync(string docTipo, string docNumero, CancellationToken ct)
        => Task.FromResult(Datos.FirstOrDefault(p =>
            p.DocTipo == docTipo.Trim().ToUpperInvariant() && p.DocNumero == docNumero && !p.Borrado));

    public Task<(IReadOnlyList<(Persona Persona, RolVinculo Rol)> Items, int TotalCount)> ListarPorEmpresaAsync(
        Guid empresaId, int page, int pageSize, CancellationToken ct)
    {
        var filtrados = Datos
            .Where(p => !p.Borrado)
            .Select(p => (p, Rol: RolVinculo.Consultor))
            .ToList();

        var items = filtrados.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult<(IReadOnlyList<(Persona, RolVinculo)>, int)>((items, filtrados.Count));
    }

    public void Agregar(Persona persona) => Datos.Add(persona);
}

public class VinculoRepositoryFake : IVinculoRepository
{
    public List<Vinculo> Datos { get; } = [];

    public Task<bool> ExisteActivoAsync(Guid personaId, Guid empresaId, RolVinculo rol, CancellationToken ct)
        => Task.FromResult(Datos.Any(v =>
            v.PersonaId == personaId && v.EmpresaId == empresaId && v.Rol == rol && v.HastaUtc == null));

    public Task<IReadOnlyList<Vinculo>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Vinculo>>(Datos.Where(v => v.EmpresaId == empresaId && v.HastaUtc == null).ToList());

    public void Agregar(Vinculo vinculo) => Datos.Add(vinculo);
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
