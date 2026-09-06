using IrmaRios.Identidad.Application.Puertos;
using IrmaRios.Identidad.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IrmaRios.Identidad.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureIdentidad(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentidadDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentidadDbContext>());
        services.AddScoped<IEmpresaRepository, EmpresaRepository>();
        services.AddScoped<IPersonaRepository, PersonaRepository>();
        services.AddScoped<IVinculoRepository, VinculoRepository>();
        services.AddScoped<IAlmacenIdempotencia, AlmacenIdempotencia>();

        return services;
    }
}
