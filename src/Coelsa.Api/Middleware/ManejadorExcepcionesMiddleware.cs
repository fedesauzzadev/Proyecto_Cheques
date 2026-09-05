using Coelsa.Domain;

namespace Coelsa.Api.Middleware;

/// <summary>
/// Convierte las excepciones de dominio en ProblemDetails (RFC 7807) en español.
/// </summary>
public class ManejadorExcepcionesMiddleware(RequestDelegate next, ILogger<ManejadorExcepcionesMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await EscribirRespuestaDeErrorAsync(context, ex);
        }
    }

    private async Task EscribirRespuestaDeErrorAsync(HttpContext context, Exception ex)
    {
        var (status, title) = ex switch
        {
            ValidacionException => (StatusCodes.Status400BadRequest, "Solicitud inválida"),
            NoEncontradoException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            ConflictoDominioException => (StatusCodes.Status409Conflict, "Conflicto"),
            TransicionInvalidaException => (StatusCodes.Status422UnprocessableEntity, "Transición de estado inválida"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(ex, "Error no controlado procesando {Metodo} {Ruta}", context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problema = new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title,
            status,
            detail = status == StatusCodes.Status500InternalServerError
                ? "Ocurrió un error inesperado. Intente nuevamente más tarde."
                : ex.Message,
            instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problema, context.RequestAborted);
    }
}
