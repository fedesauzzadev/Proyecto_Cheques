using System.Text.Json;
using IrmaRios.Identidad.Application.Common;
using IrmaRios.Identidad.Application.Dtos;
using IrmaRios.Identidad.Application.Puertos;
using IrmaRios.Identidad.Domain;
using IrmaRios.Identidad.Domain.Entidades;

namespace IrmaRios.Identidad.Application.CasosUso;

/// <summary>
/// HU-01: autoriza (o reautoriza con otro rol) una persona sobre una empresa existente.
/// Si la persona no existe se crea; si ya existe en el banco se reutiliza (misma persona
/// puede operar varias empresas).
/// </summary>
public sealed class VincularPersonaHandler(
    IEmpresaRepository empresas,
    IPersonaRepository personas,
    IVinculoRepository vinculos,
    IAlmacenIdempotencia idempotencia,
    IUnitOfWork uow)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public async Task<ResultadoIdempotente<PersonaResponse>> Ejecutar(
        string cuitEmpresa, VincularPersonaRequest request, string idempotencyKey, CancellationToken ct)
    {
        var bodyHash = Hasher.DelObjeto((cuitEmpresa, request));

        var previo = await idempotencia.ObtenerAsync(idempotencyKey, ct);
        if (previo is not null)
        {
            if (previo.BodyHash != bodyHash)
            {
                throw new ConflictoDominioException(
                    "La Idempotency-Key ya fue usada con otro cuerpo de solicitud.");
            }

            return new ResultadoIdempotente<PersonaResponse>(
                JsonSerializer.Deserialize<PersonaResponse>(previo.ResponseJson, JsonOpts)!, EsReplay: true);
        }

        var empresa = await empresas.ObtenerPorCuitAsync(cuitEmpresa, ct)
            ?? throw new NoEncontradoException($"No existe una empresa registrada con el CUIT {cuitEmpresa}.");

        if (!Enum.TryParse<RolVinculo>(request.Rol, ignoreCase: true, out var rol))
        {
            throw new ValidacionException(
                $"El rol '{request.Rol}' no es válido. Válidos: {string.Join(", ", Enum.GetNames<RolVinculo>())}.");
        }

        var persona = await personas.ObtenerPorDocumentoAsync(request.DocTipo, request.DocNumero, ct);
        if (persona is null)
        {
            persona = Persona.Crear(request.DocTipo, request.DocNumero, request.Nombre, request.Apellido, request.Email);
            personas.Agregar(persona);
        }

        if (await vinculos.ExisteActivoAsync(persona.Id, empresa.Id, rol, ct))
        {
            throw new ConflictoDominioException(
                $"La persona ya está autorizada con el rol {rol} en la empresa {cuitEmpresa}.");
        }

        vinculos.Agregar(Vinculo.Vincular(persona.Id, empresa.Id, rol, DateTime.UtcNow));

        var respuesta = new PersonaResponse(
            persona.DocTipo, persona.DocNumero, persona.Nombre, persona.Apellido, persona.Email, rol.ToString());

        idempotencia.Registrar(idempotencyKey, bodyHash, JsonSerializer.Serialize(respuesta, JsonOpts));
        await uow.SaveChangesAsync(ct);

        return new ResultadoIdempotente<PersonaResponse>(respuesta, EsReplay: false);
    }
}
