using Coelsa.Api.Middleware;
using Coelsa.Application.Dtos;
using Coelsa.Application.CasosUso;
using Coelsa.Application.Estrategias;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coelsa.Api.Controllers;

[ApiController]
[Route("api/v1/echeqs")]
[Produces("application/json")]
public class EcheqsController(
    ICrearInstrumentoStrategy<CrearEcheqRequest, EcheqResponse> crearStrategy,
    ListarEcheqsHandler listarHandler,
    ObtenerEcheqHandler obtenerHandler,
    CambiarEstadoEcheqHandler cambiarEstadoHandler,
    EliminarEcheqHandler eliminarHandler) : ControllerBase
{
    /// <summary>Crea un echeq (idempotente por header Idempotency-Key); el simulador asigna el IDECHEQ.</summary>
    [HttpPost]
    [EnableRateLimiting("creaciones")]
    [RequiereIdempotencyKey]
    public async Task<IActionResult> Crear([FromBody] CrearEcheqRequest request, CancellationToken ct)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
        var resultado = await crearStrategy.CrearAsync(request, idempotencyKey, ct);

        Response.Headers["Idempotent-Replay"] = resultado.EsReplay.ToString().ToLowerInvariant();

        return resultado.EsReplay
            ? Ok(resultado.Respuesta)
            : CreatedAtAction(nameof(Obtener), new { idecheq = resultado.Respuesta.Identificador }, resultado.Respuesta);
    }

    /// <summary>Lista echeqs por CUIT/CUIL (librador o beneficiario), paginado y cacheado.</summary>
    [HttpGet]
    [EnableRateLimiting("consultas")]
    public async Task<ActionResult<PagedResponse<EcheqResponse>>> Listar(
        [FromQuery] string? cuit,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        return Ok(await listarHandler.Ejecutar(cuit, page, pageSize, ct));
    }

    /// <summary>Obtiene un echeq por su IDECHEQ.</summary>
    [HttpGet("{idecheq}")]
    [EnableRateLimiting("consultas")]
    public async Task<ActionResult<EcheqResponse>> Obtener(string idecheq, CancellationToken ct)
    {
        return Ok(await obtenerHandler.Ejecutar(idecheq, ct));
    }

    /// <summary>Cambia el estado del echeq según la máquina de estados.</summary>
    [HttpPatch("{idecheq}/estado")]
    public async Task<ActionResult<EcheqResponse>> CambiarEstado(
        string idecheq, [FromBody] CambiarEstadoRequest request, CancellationToken ct)
    {
        await cambiarEstadoHandler.Ejecutar(idecheq, request, ct);
        return Ok(await obtenerHandler.Ejecutar(idecheq, ct));
    }

    /// <summary>Baja lógica del echeq.</summary>
    [HttpDelete("{idecheq}")]
    public async Task<IActionResult> Eliminar(string idecheq, CancellationToken ct)
    {
        await eliminarHandler.Ejecutar(idecheq, ct);
        return NoContent();
    }
}
