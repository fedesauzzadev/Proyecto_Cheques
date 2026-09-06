using System.Threading.RateLimiting;
using Coelsa.Api;
using Coelsa.Application;
using Coelsa.Infrastructure;
using Coelsa.Infrastructure.Caching;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

// Capas (arquitectura hexagonal: composition root en el adaptador de entrada).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

// Fase A: worker de débito automático de custodias vencidas (cada 5 minutos).
builder.Services.AddHostedService<Coelsa.Api.Servicios.DepositadorCustodiasWorker>();

// CORS para el front web (Front_Coelsa, static sites en Render): orígenes
// configurables en Coelsa:AllowedOrigins (separados por ';').
var origenesFront = builder.Configuration.GetValue<string>("Coelsa:AllowedOrigins")
    ?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? ["https://front-coelsa-prod.onrender.com", "https://front-coelsa-dev.onrender.com"];

builder.Services.AddCors(opciones =>
    opciones.AddDefaultPolicy(politica =>
        politica.WithOrigins(origenesFront).AllowAnyHeader().AllowAnyMethod()));

// RNF-14: compresión de respuestas (brotli/gzip) para listados.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// RF-08: rate limiting token bucket con políticas separadas para consultas y creaciones.
var rateLimit = builder.Configuration.GetSection("Coelsa:RateLimit").Get<RateLimitOpciones>() ?? new RateLimitOpciones();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = rateLimit.PeriodoSegundos.ToString();
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = "Demasiadas solicitudes",
            status = StatusCodes.Status429TooManyRequests,
            detail = $"Se excedió el límite de solicitudes. Reintente en {rateLimit.PeriodoSegundos} segundos.",
            instance = context.HttpContext.Request.Path
        }, cancellationToken);
    };

    options.AddPolicy("consultas", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = rateLimit.ConsultasTokenLimit,
                TokensPerPeriod = rateLimit.ConsultasTokenLimit,
                ReplenishmentPeriod = TimeSpan.FromSeconds(rateLimit.PeriodoSegundos),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));

    options.AddPolicy("creaciones", context =>
        RateLimitPartition.GetTokenBucketLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = rateLimit.CreacionesTokenLimit,
                TokensPerPeriod = rateLimit.CreacionesTokenLimit,
                ReplenishmentPeriod = TimeSpan.FromSeconds(rateLimit.PeriodoSegundos),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});

// RF-07: health checks de PostgreSQL y Redis.
var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'Postgres'.");

// La cadena de Redis se normaliza (el health check no acepta URLs rediss:// de proveedores cloud)
// y usa abortConnect=false para no tirar /health si Redis está caído (fail-open, RNF-13).
var redisConnectionString = Coelsa.Infrastructure.DependencyInjection.NormalizarCadenaRedis(
    builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");

builder.Services.AddHealthChecks()
    .AddNpgSql(
        postgresConnectionString,
        name: "postgres",
        tags: ["ready"])
    .AddRedis(
        $"{redisConnectionString},abortConnect=false",
        name: "redis",
        tags: ["ready"]);

var app = builder.Build();

// CORS antes de los endpoints para que el navegador acepte las llamadas del front.
app.UseCors();

// RNF-07: errores como ProblemDetails en español.
app.UseMiddleware<Coelsa.Api.Middleware.ManejadorExcepcionesMiddleware>();

app.UseResponseCompression();
app.UseRateLimiter();

// El contrato design-first (docs/openapi.yaml) se sirve estático y alimenta Swagger UI.
// .yaml no está en el mapa MIME por defecto de StaticFiles, por eso se registra explícitamente.
var proveedorMime = new FileExtensionContentTypeProvider();
proveedorMime.Mappings[".yaml"] = "application/yaml";

app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = proveedorMime });

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Coelsa:Swagger"))
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi.yaml", "Simulador COELSA — Cheques y Echeqs v1.0");
        options.RoutePrefix = "swagger";
    });
}

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = EscritorHealth.EscribirAsync
});

// Migraciones + seed (sección 9 del SPEC).
await InicializacionBaseDeDatos.InicializarAsync(app.Services);

app.Run();
