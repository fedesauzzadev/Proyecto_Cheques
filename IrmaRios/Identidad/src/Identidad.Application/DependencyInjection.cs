using IrmaRios.Identidad.Application.CasosUso;
using Microsoft.Extensions.DependencyInjection;

namespace IrmaRios.Identidad.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationIdentidad(this IServiceCollection services)
    {
        services.AddScoped<AltaEmpresaHandler>();
        services.AddScoped<VincularPersonaHandler>();
        services.AddScoped<ObtenerEmpresaHandler>();
        services.AddScoped<ListarPersonasEmpresaHandler>();
        return services;
    }
}
