using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace IrmaRios.Identidad.Api;

/// <summary>
/// Observabilidad (HU-10, RFC-004): OpenTelemetry con traces + metrics exportando
/// OTLP si OTEL_EXPORTER_OTLP_ENDPOINT está configurado (p. ej. Grafana Cloud).
/// Sin endpoint configurado (desarrollo local) solo queda logging de consola.
/// </summary>
public static class Observabilidad
{
    public static IServiceCollection AddObservabilidadIdentidad(
        this IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return services;
        }

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(
                configuration["OTEL_SERVICE_NAME"] ?? "irmarios-identidad"))
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
