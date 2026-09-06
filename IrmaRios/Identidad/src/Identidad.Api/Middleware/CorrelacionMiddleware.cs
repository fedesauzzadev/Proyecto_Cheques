namespace IrmaRios.Identidad.Api.Middleware;

/// <summary>
/// Correlación de solicitudes (RFC-004): hace eco del X-Correlation-ID entrante o
/// genera uno; queda disponible para logs y respuestas. La propagación entre
/// servicios la resuelve W3C traceparent (instrumentación OTel de HTTP).
/// </summary>
public class CorrelacionMiddleware(RequestDelegate next)
{
    public const string Header = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[Header].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Items[Header] = correlationId;
        context.Response.Headers[Header] = correlationId;

        using (context.RequestServices
                   .GetRequiredService<ILoggerFactory>()
                   .CreateLogger("Correlacion")
                   .BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
