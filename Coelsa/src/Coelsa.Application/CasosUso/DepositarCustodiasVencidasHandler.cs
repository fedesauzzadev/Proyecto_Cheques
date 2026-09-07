using Coelsa.Application.Puertos;
using Coelsa.Domain.Entidades;

namespace Coelsa.Application.CasosUso;

/// <summary>
/// Débito automático de custodias vencidas (SPEC Fase A + B5, ejecutado por el worker):
/// deposita las vencidas dentro de la ventana de presentación (30 días) y caduca
/// las que la superaron. Devuelve la cantidad depositada para observabilidad.
/// </summary>
public sealed class DepositarCustodiasVencidasHandler(
    IEcheqRepository echeqs,
    IUnitOfWork unitOfWork,
    IGestorCacheConsultas cache)
{
    public async Task<int> Ejecutar(DateOnly hoy, CancellationToken ct)
    {
        var vencidas = await echeqs.ListarCustodiasVencidasAsync(hoy, ct);
        var depositadas = 0;

        foreach (var echeq in vencidas)
        {
            if (hoy.DayNumber - echeq.FechaVencimiento.DayNumber > Domain.Validaciones.ValidacionesInstrumento.PlazoPresentacionDias)
            {
                echeq.CambiarEstado(Domain.EstadoInstrumento.Caducado, null);
            }
            else
            {
                echeq.DepositarPorVencimiento(hoy);
                depositadas++;
            }
        }

        if (vencidas.Count == 0)
        {
            return 0;
        }

        await unitOfWork.SaveChangesAsync(ct);

        foreach (var echeq in vencidas)
        {
            await cache.InvalidarAsync(Domain.TipoInstrumento.Echeq, echeq.CuitLibrador, ct);
            await cache.InvalidarAsync(Domain.TipoInstrumento.Echeq, echeq.CuitBeneficiario, ct);
        }

        return depositadas;
    }
}
