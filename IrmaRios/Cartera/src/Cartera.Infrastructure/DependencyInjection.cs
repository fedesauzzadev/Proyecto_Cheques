using IrmaRios.Cartera.Application.Puertos;
using IrmaRios.Cartera.Infrastructure.Caching;
using IrmaRios.Cartera.Infrastructure.Integracion;
using IrmaRios.Cartera.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace IrmaRios.Cartera.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureCartera(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CarteraDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CarteraDbContext>());
        services.AddScoped<ICarteraRepository, CarteraRepository>();
        services.AddScoped<IAlmacenIdempotencia, AlmacenIdempotencia>();

        // Cache de cartera (Upstash Redis en la nube, redis local en compose); fail-open.
        var redisConnectionString = NormalizarCadenaRedis(configuration.GetConnectionString("Redis") ?? "localhost:6379");
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect($"{redisConnectionString},abortConnect=false"));
        services.Configure<CacheOpciones>(configuration.GetSection("IrmaRios:Cache"));
        services.AddSingleton<IGestorCacheConsultas, GestorCacheRedis>();

        // Integraciones HTTP (clearing COELSA + servicio Identidad) con resiliencia
        // estándar (retry con backoff + circuit breaker, RFC-004).
        services.AddHttpClient<IClearing, ClearingCoelsaApi>(cliente =>
            {
                cliente.BaseAddress = new Uri(configuration["Integraciones:Coelsa:BaseUrl"]
                    ?? "https://coelsa-api-dev.onrender.com");
                cliente.Timeout = TimeSpan.FromSeconds(20);
            })
            .AddStandardResilienceHandler();

        services.AddHttpClient<IEmpresas, EmpresasIdentidadApi>(cliente =>
            {
                cliente.BaseAddress = new Uri(configuration["Integraciones:Identidad:BaseUrl"]
                    ?? "http://localhost:8081");
                cliente.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddStandardResilienceHandler();

        return services;
    }

    /// <summary>
    /// Acepta el formato nativo de StackExchange.Redis (host:port,password=...) y URLs
    /// de proveedores cloud (redis:// / rediss://user:pass@host:port), igual que Coelsa.
    /// </summary>
    public static string NormalizarCadenaRedis(string cadena)
    {
        if (!Uri.TryCreate(cadena, UriKind.Absolute, out var uri) ||
            string.IsNullOrEmpty(uri.Host) ||
            (uri.Scheme != "redis" && uri.Scheme != "rediss"))
        {
            return cadena;
        }

        var password = uri.UserInfo.Contains(':') ? uri.UserInfo.Split(':')[1] : uri.UserInfo;
        var puerto = uri.Port > 0 ? uri.Port : 6379;
        var opciones = $"{uri.Host}:{puerto},password={password}";

        return uri.Scheme == "rediss" ? $"{opciones},ssl=true" : opciones;
    }
}
