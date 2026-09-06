using IrmaRios.Identidad.Application.Common;
using IrmaRios.Identidad.Application.Dtos;
using IrmaRios.Identidad.Application.Puertos;
using IrmaRios.Identidad.Domain;
using IrmaRios.Identidad.Domain.Entidades;
using IrmaRios.Identidad.Domain.Validaciones;

namespace IrmaRios.Identidad.Application.CasosUso;

/// <summary>Consulta de una empresa por CUIT, con validación del identificador.</summary>
public sealed class ObtenerEmpresaHandler(IEmpresaRepository empresas)
{
    public async Task<EmpresaResponse> Ejecutar(string cuit, CancellationToken ct)
    {
        if (!ValidadorCuit.EsValido(cuit))
        {
            throw new ValidacionException("El CUIT debe ser válido de 11 dígitos.");
        }

        var empresa = await empresas.ObtenerPorCuitAsync(cuit, ct)
            ?? throw new NoEncontradoException($"No existe una empresa registrada con el CUIT {cuit}.");

        return new EmpresaResponse(empresa.Cuit, empresa.RazonSocial, empresa.CreadoUtc);
    }
}

/// <summary>Personas autorizadas vigentes de una empresa, paginado (HU-01).</summary>
public sealed class ListarPersonasEmpresaHandler(IEmpresaRepository empresas, IPersonaRepository personas)
{
    public async Task<PagedResponse<PersonaResponse>> Ejecutar(
        string cuit, int? page, int? pageSize, CancellationToken ct)
    {
        if (!ValidadorCuit.EsValido(cuit))
        {
            throw new ValidacionException("El CUIT debe ser válido de 11 dígitos.");
        }

        var empresa = await empresas.ObtenerPorCuitAsync(cuit, ct)
            ?? throw new NoEncontradoException($"No existe una empresa registrada con el CUIT {cuit}.");

        var (pagina, tamanio) = Paginacion.Normalizar(page, pageSize);
        var (items, totalCount) = await personas.ListarPorEmpresaAsync(empresa.Id, pagina, tamanio, ct);

        return new PagedResponse<PersonaResponse>
        {
            Items = items.Select(par => new PersonaResponse(
                par.Persona.DocTipo, par.Persona.DocNumero, par.Persona.Nombre,
                par.Persona.Apellido, par.Persona.Email, par.Rol.ToString())).ToList(),
            Page = pagina,
            PageSize = tamanio,
            TotalCount = totalCount
        };
    }
}
