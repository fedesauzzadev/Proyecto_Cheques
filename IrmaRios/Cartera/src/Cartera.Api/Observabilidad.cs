using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace IrmaRios.Cartera.Api;

/// <summary>
/// Observabilidad (HU-10, RFC-004): OTLP si OTEL_EXPORTER_OTLP_ENDPOINT está
/// configurado; sin endpoint (desarrollo local) queda logging de consola.
/// La instrumentación HTTP agrega spans de las llamadas a COELSA e Identidad:
/// la traza de un depósito cruza los tres servicios.
/// </summary>
public static class Observabilidad
{
    public static IServiceCollection AddObservabilidadCartera(
        this IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return services;
        }

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(
                configuration["OTEL_SERVICE_NAME"] ?? "irmarios-cartera"))
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation())
            .UseOtlpExporter();

        return services;
    }
}
