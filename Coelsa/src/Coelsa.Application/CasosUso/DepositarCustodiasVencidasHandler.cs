using Coelsa.Application.Puertos;
using Coelsa.Domain.Entidades;

namespace Coelsa.Application.CasosUso;

/// <summary>
/// Débito automático de custodias vencidas (SPEC Fase A, ejecutado por el worker):
/// deposita los echeqs en custodia cuya fecha de vencimiento ya pasó.
/// Devuelve la cantidad depositada para observabilidad.
/// </summary>
public sealed class DepositarCustodiasVencidasHandler(
    IEcheqRepository echeqs,
    IUnitOfWork unitOfWork,
    IGestorCacheConsultas cache)
{
    public async Task<int> Ejecutar(DateOnly hoy, CancellationToken ct)
    {
        var vencidas = await echeqs.ListarCustodiasVencidasAsync(hoy, ct);

        foreach (var echeq in vencidas)
        {
            echeq.DepositarPorVencimiento(hoy);
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

        return vencidas.Count;
    }
}
