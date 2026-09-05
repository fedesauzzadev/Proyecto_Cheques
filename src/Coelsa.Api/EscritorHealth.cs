using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Coelsa.Api;

/// <summary>
/// Escribe el health check con el formato del contrato: { status, checks: { postgres, redis } }.
/// </summary>
public static class EscritorHealth
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public static Task EscribirAsync(HttpContext context, HealthReport report)
    {
        var estado = report.Status switch
        {
            HealthStatus.Healthy => "Healthy",
            HealthStatus.Degraded => "Degraded",
            _ => "Unhealthy"
        };

        var respuesta = new
        {
            status = estado,
            checks = new
            {
                postgres = NombreDeEstado(report, "postgres"),
                redis = NombreDeEstado(report, "redis")
            }
        };

        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(respuesta, JsonOpts));
    }

    private static string NombreDeEstado(HealthReport report, string nombre)
    {
        return report.Entries.TryGetValue(nombre, out var entrada)
            ? entrada.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy"
            : "Unhealthy";
    }
}
