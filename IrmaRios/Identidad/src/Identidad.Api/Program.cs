using IrmaRios.Identidad.Api;
using IrmaRios.Identidad.Api.Middleware;
using IrmaRios.Identidad.Application;
using IrmaRios.Identidad.Application.CasosUso;
using IrmaRios.Identidad.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Capas (hexagonal: composition root en el adaptador de entrada).
builder.Services.AddApplicationIdentidad();
builder.Services.AddInfrastructureIdentidad(builder.Configuration);
builder.Services.AddObservabilidadIdentidad(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS configurable para el futuro front (Front_IrmaRios).
var origenesFront = builder.Configuration.GetValue<string>("IrmaRios:AllowedOrigins")
    ?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? ["http://localhost:5173"];

builder.Services.AddCors(opciones =>
    opciones.AddDefaultPolicy(politica =>
        politica.WithOrigins(origenesFront).AllowAnyHeader().AllowAnyMethod()));

// Rate limiting global simple por IP (mismo patrón token bucket que el ecosistema).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title = "Demasiadas solicitudes",
            status = StatusCodes.Status429TooManyRequests,
            detail = "Se excedió el límite de solicitudes. Reintente en 60 segundos.",
            instance = context.HttpContext.Request.Path
        }, cancellationToken);
    };
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(
        context => System.Threading.RateLimiting.RateLimitPartition.GetTokenBucketLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonimo",
            _ => new System.Threading.RateLimiting.TokenBucketRateLimiterOptions
            {
                TokenLimit = 60,
                TokensPerPeriod = 60,
                ReplenishmentPeriod = TimeSpan.FromSeconds(60),
                QueueLimit = 0
            }));
});

// Health check de PostgreSQL.
var postgres = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'Postgres'.");

builder.Services.AddHealthChecks()
    .AddNpgSql(postgres, name: "postgres", tags: ["ready"]);

var app = builder.Build();

app.UseCors();
app.UseMiddleware<ManejadorExcepcionesMiddleware>();
app.UseMiddleware<CorrelacionMiddleware>();
app.UseRateLimiter();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("IrmaRios:Swagger"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });

// Esquema: EnsureCreated en el slice (sin migraciones aún); migraciones EF cuando el modelo se estabilice.
using (var alcance = app.Services.CreateScope())
{
    await IrmaRios.Identidad.Api.InicializacionBaseDeDatos.InicializarAsync(alcance.ServiceProvider);
}

app.Run();
