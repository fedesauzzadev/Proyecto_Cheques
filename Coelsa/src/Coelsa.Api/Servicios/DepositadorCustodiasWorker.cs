using Coelsa.Application.CasosUso;

namespace Coelsa.Api.Servicios;

/// <summary>
/// Débito automático de custodias vencidas (SPEC Fase A): cada 5 minutos deposita
/// los echeqs en custodia cuya fecha de vencimiento ya pasó. Nunca tumba el host:
/// los errores se loguean y se reintentan en el próximo ciclo.
/// </summary>
public class DepositadorCustodiasWorker(IServiceProvider services, ILogger<DepositadorCustodiasWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var temporizador = new PeriodicTimer(Intervalo);

        while (await temporizador.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = services.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<DepositarCustodiasVencidasHandler>();
                var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
                var cantidad = await handler.Ejecutar(hoy, stoppingToken);

                if (cantidad > 0)
                {
                    logger.LogInformation(
                        "Débito automático: {Cantidad} echeqs en custodia depositados al vencer.", cantidad);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en el débito automático de custodias; se reintentará en el próximo ciclo.");
            }
        }
    }
}
