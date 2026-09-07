using Coelsa.Application.CasosUso;
using Coelsa.Application.Estrategias;
using Coelsa.Domain.Entidades;
using Microsoft.Extensions.DependencyInjection;

namespace Coelsa.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Estrategias de creación (SPEC RF-01, patrón Strategy)
        services.AddScoped<ICrearInstrumentoStrategy<Dtos.CrearChequeFisicoRequest, Dtos.ChequeResponse>,
            ChequeFisicoCreationStrategy>();
        services.AddScoped<ICrearInstrumentoStrategy<Dtos.CrearEcheqRequest, Dtos.EcheqResponse>,
            EcheqCreationStrategy>();

        // Casos de uso de consulta
        services.AddScoped<ListarChequesHandler>();
        services.AddScoped<ListarEcheqsHandler>();
        services.AddScoped<ObtenerChequeHandler>();
        services.AddScoped<ObtenerEcheqHandler>();

        // Casos de uso de escritura
        services.AddScoped<CambiarEstadoChequeHandler>();
        services.AddScoped<CambiarEstadoEcheqHandler>();
        services.AddScoped<EliminarChequeHandler>();
        services.AddScoped<EliminarEcheqHandler>();

        // Fase B: cuentas corrientes emisoras y e-chequeras (RFC-005)
        services.AddScoped<CrearCuentaHandler>();
        services.AddScoped<ListarCuentasHandler>();
        services.AddScoped<ObtenerCuentaHandler>();
        services.AddScoped<SolicitarChequeraHandler>();
        services.AddScoped<ListarChequerasHandler>();
        services.AddScoped<ObtenerTitularHandler>();

        // Fase A: aceptación, endosos, devoluciones y débito de custodias vencidas
        services.AddScoped<AceptarEcheqHandler>();
        // Fase D3: certificado para acciones civiles
        services.AddScoped<ObtenerCertificadoHandler>();        services.AddScoped<ProponerEndosoHandler>();
        services.AddScoped<ResolverEndosoHandler>();
        services.AddScoped<AnularEndosoHandler>();
        services.AddScoped<ListarEndososHandler>();
        services.AddScoped<SolicitarDevolucionHandler>();
        services.AddScoped<ResolverDevolucionHandler>();
        services.AddScoped<AnularDevolucionHandler>();
        services.AddScoped<ListarDevolucionesHandler>();
        services.AddScoped<DepositarCustodiasVencidasHandler>();

        // Fase D1: cesiones de echeqs "no a la orden"
        services.AddScoped<SolicitarCesionHandler>();
        services.AddScoped<ResolverCesionHandler>();
        services.AddScoped<AnularCesionHandler>();
        services.AddScoped<ListarCesionesHandler>();

        return services;
    }
}
