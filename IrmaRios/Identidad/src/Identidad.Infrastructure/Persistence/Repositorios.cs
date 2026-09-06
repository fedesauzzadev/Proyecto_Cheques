using IrmaRios.Identidad.Application.Puertos;
using IrmaRios.Identidad.Domain;
using IrmaRios.Identidad.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace IrmaRios.Identidad.Infrastructure.Persistence;

public class EmpresaRepository(IdentidadDbContext db) : IEmpresaRepository
{
    private readonly IdentidadDbContext _db = db;

    public Task<Empresa?> ObtenerPorCuitAsync(string cuit, CancellationToken ct)
        => _db.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Cuit == cuit && !e.Borrado, ct);

    public Task<bool> ExistePorCuitAsync(string cuit, CancellationToken ct)
        => _db.Empresas.AnyAsync(e => e.Cuit == cuit && !e.Borrado, ct);

    public void Agregar(Empresa empresa) => _db.Empresas.Add(empresa);
}

public class PersonaRepository(IdentidadDbContext db) : IPersonaRepository
{
    private readonly IdentidadDbContext _db = db;

    public Task<Persona?> ObtenerPorDocumentoAsync(string docTipo, string docNumero, CancellationToken ct)
        => _db.Personas.AsNoTracking().FirstOrDefaultAsync(
            p => p.DocTipo == docTipo.Trim().ToUpperInvariant() && p.DocNumero == docNumero && !p.Borrado, ct);

    public async Task<(IReadOnlyList<(Persona Persona, RolVinculo Rol)> Items, int TotalCount)> ListarPorEmpresaAsync(
        Guid empresaId, int page, int pageSize, CancellationToken ct)
    {
        var consulta = _db.Vinculos.AsNoTracking()
            .Where(v => v.EmpresaId == empresaId && v.HastaUtc == null)
            .Join(_db.Personas.AsNoTracking().Where(p => !p.Borrado),
                v => v.PersonaId, p => p.Id,
                (v, p) => new { Persona = p, v.Rol })
            .OrderBy(x => x.Persona.Apellido).ThenBy(x => x.Persona.Nombre);

        var totalCount = await consulta.CountAsync(ct);
        var items = await consulta
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Persona, x.Rol })
            .ToListAsync(ct);

        return (items.Select(x => (x.Persona, x.Rol)).ToList(), totalCount);
    }

    public void Agregar(Persona persona) => _db.Personas.Add(persona);
}

public class VinculoRepository(IdentidadDbContext db) : IVinculoRepository
{
    private readonly IdentidadDbContext _db = db;

    public Task<bool> ExisteActivoAsync(Guid personaId, Guid empresaId, RolVinculo rol, CancellationToken ct)
        => _db.Vinculos.AnyAsync(
            v => v.PersonaId == personaId && v.EmpresaId == empresaId && v.Rol == rol && v.HastaUtc == null, ct);

    public async Task<IReadOnlyList<Vinculo>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken ct)
        => await _db.Vinculos.AsNoTracking()
            .Where(v => v.EmpresaId == empresaId && v.HastaUtc == null)
            .ToListAsync(ct);

    public void Agregar(Vinculo vinculo) => _db.Vinculos.Add(vinculo);
}

/// <summary>
/// Almacén de idempotencia sobre el mismo DbContext: el registro se persiste
/// en la misma transacción que el negocio (atómico).
/// </summary>
public class AlmacenIdempotencia(IdentidadDbContext db) : IAlmacenIdempotencia
{
    private readonly IdentidadDbContext _db = db;

    public async Task<RegistroIdempotencia?> ObtenerAsync(string key, CancellationToken ct)
    {
        var entrada = await _db.IdempotenciaKeys.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Key == key, ct);

        return entrada is null
            ? null
            : new RegistroIdempotencia(entrada.Key, entrada.BodyHash, entrada.ResponseJson);
    }

    public void Registrar(string key, string bodyHash, string responseJson)
    {
        _db.IdempotenciaKeys.Add(new EntradaIdempotencia
        {
            Id = Guid.NewGuid(),
            Key = key,
            BodyHash = bodyHash,
            ResponseJson = responseJson,
            FechaCreacion = DateTime.UtcNow
        });
    }
}
