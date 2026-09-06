using IrmaRios.Identidad.Domain;
using IrmaRios.Identidad.Domain.Entidades;

namespace IrmaRios.Identidad.Application.Puertos;

public interface IEmpresaRepository
{
    Task<Empresa?> ObtenerPorCuitAsync(string cuit, CancellationToken ct);

    Task<bool> ExistePorCuitAsync(string cuit, CancellationToken ct);

    void Agregar(Empresa empresa);
}

public interface IPersonaRepository
{
    Task<Persona?> ObtenerPorDocumentoAsync(string docTipo, string docNumero, CancellationToken ct);

    Task<(IReadOnlyList<(Persona Persona, RolVinculo Rol)> Items, int TotalCount)> ListarPorEmpresaAsync(
        Guid empresaId, int page, int pageSize, CancellationToken ct);

    void Agregar(Persona persona);
}

public interface IVinculoRepository
{
    Task<bool> ExisteActivoAsync(Guid personaId, Guid empresaId, RolVinculo rol, CancellationToken ct);

    Task<IReadOnlyList<Vinculo>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken ct);

    void Agregar(Vinculo vinculo);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}

/// <summary>Registro de idempotencia persistido junto al negocio en la misma transacción.</summary>
public sealed record RegistroIdempotencia(string Key, string BodyHash, string ResponseJson);

public interface IAlmacenIdempotencia
{
    Task<RegistroIdempotencia?> ObtenerAsync(string key, CancellationToken ct);

    /// <summary>Encola el registro; se persiste con el UnitOfWork de la operación.</summary>
    void Registrar(string key, string bodyHash, string responseJson);
}
