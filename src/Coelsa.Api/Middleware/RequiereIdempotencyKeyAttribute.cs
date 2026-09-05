using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Coelsa.Api.Middleware;

/// <summary>
/// Exige el header Idempotency-Key (GUID) en las creaciones (SPEC RF-02).
/// </summary>
public class RequiereIdempotencyKeyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue("Idempotency-Key", out var valor)
            || string.IsNullOrWhiteSpace(valor))
        {
            context.Result = new BadRequestObjectResult(new
            {
                type = "https://tools.ietf.org/html/rfc7807",
                title = "Solicitud inválida",
                status = 400,
                detail = "El header 'Idempotency-Key' (GUID) es obligatorio para las operaciones de creación.",
                instance = context.HttpContext.Request.Path
            })
            {
                ContentTypes = { "application/problem+json" }
            };
        }
    }
}
