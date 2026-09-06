using IrmaRios.Cartera.Application.CasosUso;
using Microsoft.Extensions.DependencyInjection;

namespace IrmaRios.Cartera.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationCartera(this IServiceCollection services)
    {
        services.AddScoped<DepositarHandler>();
        services.AddScoped<ListarCarteraHandler>();
        services.AddScoped<ObtenerInstrumentoHandler>();
        return services;
    }
}
