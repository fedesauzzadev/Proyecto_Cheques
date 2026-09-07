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
    EliminarEcheqHandler eliminarHandler,
    AceptarEcheqHandler aceptarHandler,
    ProponerEndosoHandler proponerEndosoHandler,
    ResolverEndosoHandler resolverEndosoHandler,
    AnularEndosoHandler anularEndosoHandler,
    ListarEndososHandler listarEndososHandler,
    SolicitarDevolucionHandler solicitarDevolucionHandler,
    ResolverDevolucionHandler resolverDevolucionHandler,
    AnularDevolucionHandler anularDevolucionHandler,
    ListarDevolucionesHandler listarDevolucionesHandler,
    SolicitarCesionHandler solicitarCesionHandler,
    ResolverCesionHandler resolverCesionHandler,
    AnularCesionHandler anularCesionHandler,
    ListarCesionesHandler listarCesionesHandler,
    ObtenerCertificadoHandler certificadoHandler) : ControllerBase
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

    /// <summary>Lista echeqs por CUIT/CUIL (librador o beneficiario) con filtros opcionales, paginado y cacheado.</summary>
    [HttpGet]
    [EnableRateLimiting("consultas")]
    public async Task<ActionResult<PagedResponse<EcheqResponse>>> Listar(
        [FromQuery] string? cuit,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cbu,
        [FromQuery] string? estado,
        [FromQuery] DateOnly? desdeEmision,
        [FromQuery] DateOnly? hastaEmision,
        [FromQuery] DateOnly? desdeVencimiento,
        [FromQuery] DateOnly? hastaVencimiento,
        [FromQuery] int? numeroCheque,
        CancellationToken ct)
    {
        var filtros = new FiltrosEcheq(
            cbu, estado, desdeEmision, hastaEmision, desdeVencimiento, hastaVencimiento, numeroCheque);
        return Ok(await listarHandler.Ejecutar(cuit, filtros, page, pageSize, ct));
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

    /// <summary>Aceptación o repudio del beneficiario sobre un echeq pendiente.</summary>
    [HttpPost("{idecheq}/aceptacion")]
    [EnableRateLimiting("creaciones")]
    public async Task<ActionResult<EcheqResponse>> Aceptar(string idecheq, [FromBody] AceptarEcheqRequest request, CancellationToken ct)
    {
        return Ok(await aceptarHandler.Ejecutar(idecheq, request, ct));
    }

    /// <summary>Propone un endoso nominativo a un CUIT (queda pendiente de admisión).</summary>
    [HttpPost("{idecheq}/endosos")]
    [EnableRateLimiting("creaciones")]
    public async Task<ActionResult<EndosoResponse>> ProponerEndoso(string idecheq, [FromBody] ProponerEndosoRequest request, CancellationToken ct)
    {
        var respuesta = await proponerEndosoHandler.Ejecutar(idecheq, request, ct);
        return CreatedAtAction(nameof(ObtenerEndoso), new { idecheq, orden = respuesta.Orden }, respuesta);
    }

    /// <summary>Cadena de endosos del echeq para trazabilidad.</summary>
    [HttpGet("{idecheq}/endosos")]
    [EnableRateLimiting("consultas")]
    public async Task<ActionResult<IReadOnlyList<EndosoResponse>>> ListarEndosos(string idecheq, CancellationToken ct)
    {
        return Ok(await listarEndososHandler.Ejecutar(idecheq, ct));
    }

    /// <summary>Obtiene un endoso por su orden en la cadena.</summary>
    [HttpGet("{idecheq}/endosos/{orden:int}")]
    [EnableRateLimiting("consultas")]
    public async Task<ActionResult<EndosoResponse>> ObtenerEndoso(string idecheq, int orden, CancellationToken ct)
    {
        var cadena = await listarEndososHandler.Ejecutar(idecheq, ct);
        var endoso = cadena.FirstOrDefault(e => e.Orden == orden);
        return endoso is null ? NotFound() : Ok(endoso);
    }

    /// <summary>Admisión o repudio de un endoso propuesto (solo el endosatario).</summary>
    [HttpPost("{idecheq}/endosos/{orden:int}/admision")]
    [EnableRateLimiting("creaciones")]
    public async Task<ActionResult<EndosoResponse>> ResolverEndoso(string idecheq, int orden, [FromBody] ResolverEndosoRequest request, CancellationToken ct)
    {
        return Ok(await resolverEndosoHandler.Ejecutar(idecheq, orden, request, ct));
    }

    /// <summary>Anulación de un endoso propuesto por el endosante.</summary>
    [HttpDelete("{idecheq}/endosos/{orden:int}")]
    [EnableRateLimiting("creaciones")]
    public async Task<IActionResult> AnularEndoso(string idecheq, int orden, CancellationToken ct)
    {
        await anularEndosoHandler.Ejecutar(idecheq, orden, ct);
        return NoContent();
    }

    /// <summary>Solicita la devolución del echeq (cualquier integrante de la cadena).</summary>
    [HttpPost("{idecheq}/devoluciones")]
    [EnableRateLimiting("creaciones")]
    public async Task<ActionResult<DevolucionResponse>> SolicitarDevolucion(string idecheq, [FromBody] SolicitarDevolucionRequest request, CancellationToken ct)
    {
        var respuesta = await solicitarDevolucionHandler.Ejecutar(idecheq, request, ct);
        return CreatedAtAction(nameof(ObtenerDevolucion), new { idecheq, numero = respuesta.Numero }, respuesta);
    }

    /// <summary>Pedidos de devolución del echeq para trazabilidad.</summary>
    [HttpGet("{idecheq}/devoluciones")]
    [EnableRateLimiting("consultas")]
    public async Task<ActionResult<IReadOnlyList<DevolucionResponse>>> ListarDevoluciones(string idecheq, CancellationToken ct)
    {
        return Ok(await listarDevolucionesHandler.Ejecutar(idecheq, ct));
    }

    /// <summary>Obtiene un pedido de devolución por su número.</summary>
    [HttpGet("{idecheq}/devoluciones/{numero:int}")]
    [EnableRateLimiting("consultas")]
    public async Task<ActionResult<DevolucionResponse>> ObtenerDevolucion(string idecheq, int numero, CancellationToken ct)
    {
        var pedidos = await listarDevolucionesHandler.Ejecutar(idecheq, ct);
        var pedido = pedidos.FirstOrDefault(d => d.Numero == numero);
        return pedido is null ? NotFound() : Ok(pedido);
    }

    /// <summary>Aceptación o rechazo del pedido por el tenedor actual.</summary>
    [HttpPost("{idecheq}/devoluciones/{numero:int}/resolucion")]
    [EnableRateLimiting("creaciones")]
    public async Task<ActionResult<DevolucionResponse>> ResolverDevolucion(string idecheq, int numero, [FromBody] ResolverDevolucionRequest request, CancellationToken ct)
    {
        return Ok(await resolverDevolucionHandler.Ejecutar(idecheq, numero, request, ct));
    }

    /// <summary>Anulación de un pedido solicitado por el solicitante.</summary>
    [HttpDelete("{idecheq}/devoluciones/{numero:int}")]
    [EnableRateLimiting("creaciones")]
    public async Task<IActionResult> AnularDevolucion(string idecheq, int numero, CancellationToken ct)
    {
        await anularDevolucionHandler.Ejecutar(idecheq, numero, ct);
        return NoContent();
    }

    /// <summary>Solicita la cesión del echeq "no a la orden" a un tercero (el tenedor cede).</summary>
    [HttpPost("{idecheq}/cesiones")]
    [EnableRateLimiting("creaciones")]
    public async Task<ActionResult<CesionResponse>> SolicitarCesion(string idecheq, [FromBody] SolicitarCesionRequest request, CancellationToken ct)
    {
        var respuesta = await solicitarCesionHandler.Ejecutar(idecheq, request, ct);
        return CreatedAtAction(nameof(ObtenerCesion), new { idecheq, numero = respuesta.Numero }, respuesta);
    }

    /// <summary>Cesiones del echeq para trazabilidad.</summary>
    [HttpGet("{idecheq}/cesiones")]
    [EnableRateLimiting("consultas")]
    public async Task<ActionResult<IReadOnlyList<CesionResponse>>> ListarCesiones(string idecheq, CancellationToken ct)
    {
        return Ok(await listarCesionesHandler.Ejecutar(idecheq, ct));
    }

    /// <summary>Obtiene una cesión por su número.</summary>
    [HttpGet("{idecheq}/cesiones/{numero:int}")]
    [EnableRateLimiting("consultas")]
    public async Task<ActionResult<CesionResponse>> ObtenerCesion(string idecheq, int numero, CancellationToken ct)
    {
        var lista = await listarCesionesHandler.Ejecutar(idecheq, ct);
        var cesion = lista.FirstOrDefault(c => c.Numero == numero);
        return cesion is null ? NotFound() : Ok(cesion);
    }

    /// <summary>Aceptación o rechazo de la cesión por el cesionario.</summary>
    [HttpPost("{idecheq}/cesiones/{numero:int}/resolucion")]
    [EnableRateLimiting("creaciones")]
    public async Task<ActionResult<CesionResponse>> ResolverCesion(string idecheq, int numero, [FromBody] ResolverCesionRequest request, CancellationToken ct)
    {
        return Ok(await resolverCesionHandler.Ejecutar(idecheq, numero, request, ct));
    }

    /// <summary>Anulación de una cesión solicitada por el cedente.</summary>
    [HttpDelete("{idecheq}/cesiones/{numero:int}")]
    [EnableRateLimiting("creaciones")]
    public async Task<IActionResult> AnularCesion(string idecheq, int numero, CancellationToken ct)
    {
        await anularCesionHandler.Ejecutar(idecheq, numero, ct);
        return NoContent();
    }

    /// <summary>Certificado para ejercer acciones civiles (solo echeqs rechazados).</summary>
    [HttpGet("{idecheq}/certificado")]
    [EnableRateLimiting("consultas")]
    public async Task<ActionResult<CertificadoResponse>> ObtenerCertificado(string idecheq, CancellationToken ct)
    {
        return Ok(await certificadoHandler.Ejecutar(idecheq, ct));
    }
}
