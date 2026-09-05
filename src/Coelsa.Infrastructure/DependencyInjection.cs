using Coelsa.Application;
using Coelsa.Application.Puertos;
using Coelsa.Infrastructure.Caching;
using Coelsa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Coelsa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CoelsaDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CoelsaDbContext>());

        services.AddScoped<IInstrumentoRepository<Domain.Entidades.ChequeFisico>, ChequeFisicoRepository>();
        services.AddScoped<IEcheqRepository, EcheqRepository>();
        services.AddScoped<IInstrumentoRepository<Domain.Entidades.Echeq>>(sp => sp.GetRequiredService<IEcheqRepository>());

        services.AddScoped<IAlmacenIdempotencia, AlmacenIdempotencia>();

        // abortConnect=false: la API arranca aunque Redis no esté disponible (fail-open, RNF-13).
        var redisConnectionString = NormalizarCadenaRedis(configuration.GetConnectionString("Redis") ?? "localhost:6379");
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect($"{redisConnectionString},abortConnect=false"));

        services.Configure<CacheOpciones>(configuration.GetSection("Coelsa:Cache"));
        services.AddSingleton<IGestorCacheConsultas, GestorCacheRedis>();

        services.AddSingleton<IGeneradorIdEcheq, GeneradorIdEcheq>();

        return services;
    }

    /// <summary>
    /// Acepta tanto el formato nativo de StackExchange.Redis (host:port,password=...)
    /// como URLs de proveedores cloud (redis:// / rediss://user:pass@host:port).
    /// </summary>
    private static string NormalizarCadenaRedis(string cadena)
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
