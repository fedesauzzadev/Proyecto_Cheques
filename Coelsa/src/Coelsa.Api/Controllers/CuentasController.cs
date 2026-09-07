using Coelsa.Api.Middleware;
using Coelsa.Application.CasosUso;
using Coelsa.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coelsa.Api.Controllers;

[ApiController]
[Route("api/v1/cuentas")]
[Produces("application/json")]
public class CuentasController(
    CrearCuentaHandler crearHandler,
    ListarCuentasHandler listarHandler,
    ObtenerCuentaHandler obtenerHandler,
    SolicitarChequeraHandler solicitarChequeraHandler,
    ListarChequerasHandler listarChequerasHandler,
    ObtenerTitularHandler titularHandler) : ControllerBase
{
    /// <summary>Crea una cuenta corriente emisora de echeqs (idempotente por header Idempotency-Key).</summary>
    [HttpPost]
    [EnableRateLimiting("creaciones")]
    [RequiereIdempotencyKey]
    public async Task<IActionResult> Crear([FromBody] CrearCuentaRequest request, CancellationToken ct)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
        var (respuesta, esReplay) = await crearHandler.Ejecutar(request, idempotencyKey, ct);

        Response.Headers["Idempotent-Replay"] = esReplay.ToString().ToLowerInvariant();

        return esReplay
            ? Ok(respuesta)
            : CreatedAtAction(nameof(Obtener), new { cbu = respuesta.Cbu }, respuesta);
    }

    /// <summary>Lista las cuentas activas de un CUIT/CUIL titular.</summary>
    [HttpGet]
    [EnableRateLimiting("consultas")]
    public async Task<ActionResult<IReadOnlyList<CuentaResponse>>> Listar(
        [FromQuery] string? cuit, CancellationToken ct)
    {
        return Ok(await listarHandler.Ejecutar(cuit, ct));
    }

    /// <summary>Obtiene una cuenta por su CBU (22 dígitos).</summary>
    [HttpGet("{cbu}")]
    [EnableRateLimiting("consultas")]
    public async Task<ActionResult<CuentaResponse>> Obtener(string cbu, CancellationToken ct)
    {
        return Ok(await obtenerHandler.Ejecutar(cbu, ct));
    }

    /// <summary>Solicita una e-chequera nueva para la cuenta (idempotente; reintentar con la misma key no duplica).</summary>
    [HttpPost("{cbu}/chequeras")]
    [EnableRateLimiting("creaciones")]
    [RequiereIdempotencyKey]
    public async Task<IActionResult> SolicitarChequera(string cbu, CancellationToken ct)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
        var (respuesta, esReplay) = await solicitarChequeraHandler.Ejecutar(cbu, idempotencyKey, ct);

        Response.Headers["Idempotent-Replay"] = esReplay.ToString().ToLowerInvariant();

        return esReplay
            ? Ok(respuesta)
            : CreatedAtAction(nameof(ListarChequeras), new { cbu }, respuesta);
    }

    /// <summary>Lista las chequeras de la cuenta, ordenadas por número.</summary>
    [HttpGet("{cbu}/chequeras")]
    [EnableRateLimiting("consultas")]
    public async Task<ActionResult<IReadOnlyList<ChequeraResponse>>> ListarChequeras(string cbu, CancellationToken ct)
    {
        return Ok(await listarChequerasHandler.Ejecutar(cbu, ct));
    }

    /// <summary>Padrón simulado de titulares ("lupa"): valida el documento y devuelve el nombre si tiene cuenta.</summary>
    [HttpGet("titulares")]
    [EnableRateLimiting("consultas")]
    public async Task<ActionResult<TitularResponse>> ObtenerTitular(
        [FromQuery] string? tipoDoc, [FromQuery] string? numero, CancellationToken ct)
    {
        return Ok(await titularHandler.Ejecutar(tipoDoc, numero, ct));
    }
}
