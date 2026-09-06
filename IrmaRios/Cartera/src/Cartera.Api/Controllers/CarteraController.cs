using IrmaRios.Cartera.Api.Middleware;
using IrmaRios.Cartera.Application.CasosUso;
using IrmaRios.Cartera.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace IrmaRios.Cartera.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class CarteraController(
    DepositarHandler depositarHandler,
    ListarCarteraHandler listarHandler,
    ObtenerInstrumentoHandler obtenerHandler) : ControllerBase
{
    /// <summary>Deposita una tanda de instrumentos a cobro, validando cada uno contra el clearing (HU-02).</summary>
    [HttpPost("depositos")]
    [RequiereIdempotencyKey]
    public async Task<IActionResult> Depositar([FromBody] DepositoRequest request, CancellationToken ct)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
        var resultado = await depositarHandler.Ejecutar(request, idempotencyKey, ct);

        Response.Headers["Idempotent-Replay"] = resultado.EsReplay.ToString().ToLowerInvariant();

        return resultado.EsReplay
            ? Ok(resultado.Respuesta)
            : CreatedAtAction(
                nameof(Obtener),
                new { identificador = resultado.Respuesta.Aceptados.FirstOrDefault()?.Identificador },
                resultado.Respuesta);
    }

    /// <summary>Cartera de cheques físicos de la empresa, paginada y cacheada (HU-03).</summary>
    [HttpGet("cartera/{cuit}/cheques")]
    public async Task<ActionResult<PagedResponse<InstrumentoCarteraResponse>>> ListarCheques(
        string cuit, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
        => Ok(await listarHandler.Ejecutar("ChequeFisico", cuit, page, pageSize, ct));

    /// <summary>Cartera de echeqs de la empresa, paginada y cacheada (HU-03).</summary>
    [HttpGet("cartera/{cuit}/echeqs")]
    public async Task<ActionResult<PagedResponse<InstrumentoCarteraResponse>>> ListarEcheqs(
        string cuit, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
        => Ok(await listarHandler.Ejecutar("Echeq", cuit, page, pageSize, ct));

    /// <summary>Detalle de un instrumento de la cartera por su identificador (CMC7 o IDECHEQ).</summary>
    [HttpGet("cartera/instrumentos/{identificador}")]
    public async Task<ActionResult<InstrumentoCarteraResponse>> Obtener(string identificador, CancellationToken ct)
        => Ok(await obtenerHandler.Ejecutar(identificador, ct));
}
