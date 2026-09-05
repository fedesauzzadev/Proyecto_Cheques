using Coelsa.Api.Middleware;
using Coelsa.Application.Dtos;
using Coelsa.Application.CasosUso;
using Coelsa.Application.Estrategias;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coelsa.Api.Controllers;

[ApiController]
[Route("api/v1/cheques")]
[Produces("application/json")]
public class ChequesController(
    ICrearInstrumentoStrategy<CrearChequeFisicoRequest, ChequeResponse> crearStrategy,
    ListarChequesHandler listarHandler,
    ObtenerChequeHandler obtenerHandler,
    CambiarEstadoChequeHandler cambiarEstadoHandler,
    EliminarChequeHandler eliminarHandler) : ControllerBase
{
    /// <summary>Crea un cheque físico (idempotente por header Idempotency-Key).</summary>
    [HttpPost]
    [EnableRateLimiting("creaciones")]
    [RequiereIdempotencyKey]
    public async Task<IActionResult> Crear([FromBody] CrearChequeFisicoRequest request, CancellationToken ct)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
        var resultado = await crearStrategy.CrearAsync(request, idempotencyKey, ct);

        Response.Headers["Idempotent-Replay"] = resultado.EsReplay.ToString().ToLowerInvariant();

        return resultado.EsReplay
            ? Ok(resultado.Respuesta)
            : CreatedAtAction(nameof(Obtener), new { cmc7 = resultado.Respuesta.Identificador }, resultado.Respuesta);
    }

    /// <summary>Lista cheques físicos por CUIT/CUIL (librador o beneficiario), paginado y cacheado.</summary>
    [HttpGet]
    [EnableRateLimiting("consultas")]
    public async Task<ActionResult<PagedResponse<ChequeResponse>>> Listar(
        [FromQuery] string? cuit,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        return Ok(await listarHandler.Ejecutar(cuit, page, pageSize, ct));
    }

    /// <summary>Obtiene un cheque físico por su CMC7 (30 dígitos).</summary>
    [HttpGet("{cmc7}")]
    [EnableRateLimiting("consultas")]
    public async Task<ActionResult<ChequeResponse>> Obtener(string cmc7, CancellationToken ct)
    {
        return Ok(await obtenerHandler.Ejecutar(cmc7, ct));
    }

    /// <summary>Cambia el estado del cheque físico según la máquina de estados.</summary>
    [HttpPatch("{cmc7}/estado")]
    public async Task<ActionResult<ChequeResponse>> CambiarEstado(
        string cmc7, [FromBody] CambiarEstadoRequest request, CancellationToken ct)
    {
        await cambiarEstadoHandler.Ejecutar(cmc7, request, ct);
        return Ok(await obtenerHandler.Ejecutar(cmc7, ct));
    }

    /// <summary>Baja lógica del cheque físico.</summary>
    [HttpDelete("{cmc7}")]
    public async Task<IActionResult> Eliminar(string cmc7, CancellationToken ct)
    {
        await eliminarHandler.Ejecutar(cmc7, ct);
        return NoContent();
    }
}
