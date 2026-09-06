using System.Text.Json;
using IrmaRios.Identidad.Application.Common;
using IrmaRios.Identidad.Application.Dtos;
using IrmaRios.Identidad.Application.Puertos;
using IrmaRios.Identidad.Domain;
using IrmaRios.Identidad.Domain.Entidades;
using Microsoft.Extensions.Logging;

namespace IrmaRios.Identidad.Application.CasosUso;

/// <summary>
/// HU-01: alta de empresa con personas autorizadas opcionales.
/// Idempotente por Idempotency-Key: reenvío con mismo body devuelve la respuesta
/// original; misma key con otro body es conflicto.
/// </summary>
public sealed class AltaEmpresaHandler(
    IEmpresaRepository empresas,
    IPersonaRepository personas,
    IVinculoRepository vinculos,
    IAlmacenIdempotencia idempotencia,
    IUnitOfWork uow,
    ILogger<AltaEmpresaHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public async Task<ResultadoIdempotente<EmpresaConPersonasResponse>> Ejecutar(
        AltaEmpresaRequest request, string idempotencyKey, CancellationToken ct)
    {
        var bodyHash = Hasher.DelObjeto(request);

        var previo = await idempotencia.ObtenerAsync(idempotencyKey, ct);
        if (previo is not null)
        {
            if (previo.BodyHash != bodyHash)
            {
                throw new ConflictoDominioException(
                    "La Idempotency-Key ya fue usada con otro cuerpo de solicitud.");
            }

            return new ResultadoIdempotente<EmpresaConPersonasResponse>(
                JsonSerializer.Deserialize<EmpresaConPersonasResponse>(previo.ResponseJson, JsonOpts)!, EsReplay: true);
        }

        var empresa = Empresa.Crear(request.Cuit, request.RazonSocial, DateTime.UtcNow);

        if (await empresas.ExistePorCuitAsync(empresa.Cuit, ct))
        {
            throw new ConflictoDominioException($"Ya existe una empresa registrada con el CUIT {empresa.Cuit}.");
        }

        var personasDeAlta = NormalizarPersonas(request.Personas);

        empresas.Agregar(empresa);

        var respuestas = new List<PersonaResponse>();
        foreach (var (personaRequest, persona, rol) in personasDeAlta)
        {
            var personaExistente = await personas.ObtenerPorDocumentoAsync(
                personaRequest.DocTipo, personaRequest.DocNumero, ct);

            var personaFinal = personaExistente ?? persona!;
            if (personaExistente is null)
            {
                personas.Agregar(personaFinal);
            }

            if (await vinculos.ExisteActivoAsync(personaFinal.Id, empresa.Id, rol, ct))
            {
                throw new ConflictoDominioException(
                    $"La persona ya está autorizada con el rol {rol} en esta empresa.");
            }

            vinculos.Agregar(Vinculo.Vincular(personaFinal.Id, empresa.Id, rol, DateTime.UtcNow));
            respuestas.Add(personaFinal.AResponse(rol));
        }

        var respuesta = new EmpresaConPersonasResponse(empresa.Cuit, empresa.RazonSocial, respuestas);
        idempotencia.Registrar(idempotencyKey, bodyHash, JsonSerializer.Serialize(respuesta, JsonOpts));
        await uow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Empresa {Cuit} dada de alta con {Personas} persona(s) autorizada(s).", empresa.Cuit, respuestas.Count);

        return new ResultadoIdempotente<EmpresaConPersonasResponse>(respuesta, EsReplay: false);
    }

    private static List<(PersonaRequest Request, Persona? Persona, RolVinculo Rol)> NormalizarPersonas(
        List<PersonaRequest>? personas)
    {
        if (personas is { Count: 0 })
        {
            throw new ValidacionException("Si se envía 'personas' debe incluir al menos una.");
        }

        var resultado = new List<(PersonaRequest, Persona?, RolVinculo)>();
        foreach (var p in personas ?? [])
        {
            if (!Enum.TryParse<RolVinculo>(p.Rol, ignoreCase: true, out var rol))
            {
                throw new ValidacionException(
                    $"El rol '{p.Rol}' no es válido. Válidos: {string.Join(", ", Enum.GetNames<RolVinculo>())}.");
            }

            // Valida los datos de la persona en dominio (falla temprano aunque sea existente).
            var nueva = Persona.Crear(p.DocTipo, p.DocNumero, p.Nombre, p.Apellido, p.Email);
            resultado.Add((p, nueva, rol));
        }

        return resultado;
    }
}

file static class Mapeo
{
    public static PersonaResponse AResponse(this Persona persona, RolVinculo rol)
        => new(persona.DocTipo, persona.DocNumero, persona.Nombre, persona.Apellido, persona.Email, rol.ToString());
}
