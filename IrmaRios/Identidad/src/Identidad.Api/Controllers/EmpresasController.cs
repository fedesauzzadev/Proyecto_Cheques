using IrmaRios.Identidad.Api.Middleware;
using IrmaRios.Identidad.Application.CasosUso;
using IrmaRios.Identidad.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace IrmaRios.Identidad.Api.Controllers;

[ApiController]
[Route("api/v1/empresas")]
[Produces("application/json")]
public class EmpresasController(
    AltaEmpresaHandler altaHandler,
    VincularPersonaHandler vincularHandler,
    ObtenerEmpresaHandler obtenerHandler,
    ListarPersonasEmpresaHandler listarPersonasHandler) : ControllerBase
{
    /// <summary>Alta de empresa con personas autorizadas opcionales (idempotente por Idempotency-Key).</summary>
    [HttpPost]
    [RequiereIdempotencyKey]
    public async Task<IActionResult> Crear([FromBody] AltaEmpresaRequest request, CancellationToken ct)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
        var resultado = await altaHandler.Ejecutar(request, idempotencyKey, ct);

        Response.Headers["Idempotent-Replay"] = resultado.EsReplay.ToString().ToLowerInvariant();

        return resultado.EsReplay
            ? Ok(resultado.Respuesta)
            : CreatedAtAction(nameof(Obtener), new { cuit = resultado.Respuesta.Cuit }, resultado.Respuesta);
    }

    /// <summary>Obtiene una empresa por su CUIT.</summary>
    [HttpGet("{cuit}")]
    public async Task<ActionResult<EmpresaResponse>> Obtener(string cuit, CancellationToken ct)
        => Ok(await obtenerHandler.Ejecutar(cuit, ct));

    /// <summary>Autoriza (creando si no existe) una persona sobre la empresa (idempotente).</summary>
    [HttpPost("{cuit}/personas")]
    [RequiereIdempotencyKey]
    public async Task<IActionResult> VincularPersona(
        string cuit, [FromBody] VincularPersonaRequest request, CancellationToken ct)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
        var resultado = await vincularHandler.Ejecutar(cuit, request, idempotencyKey, ct);

        Response.Headers["Idempotent-Replay"] = resultado.EsReplay.ToString().ToLowerInvariant();

        return resultado.EsReplay ? Ok(resultado.Respuesta) : StatusCode(StatusCodes.Status201Created, resultado.Respuesta);
    }

    /// <summary>Personas autorizadas vigentes de la empresa, paginado.</summary>
    [HttpGet("{cuit}/personas")]
    public async Task<ActionResult<PagedResponse<PersonaResponse>>> ListarPersonas(
        string cuit, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
        => Ok(await listarPersonasHandler.Ejecutar(cuit, page, pageSize, ct));
}
