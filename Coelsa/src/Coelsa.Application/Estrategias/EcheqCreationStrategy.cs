using System.Text.Json;
using Coelsa.Application.Common;
using Coelsa.Application.Dtos;
using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;

namespace Coelsa.Application.Estrategias;

/// <summary>
/// Estrategia de creación de echeqs: valida CUD bien formado y asigna el IDECHEQ generado (SPEC RF-01).
/// </summary>
public class EcheqCreationStrategy(
    IEcheqRepository repository,
    IUnitOfWork unitOfWork,
    IAlmacenIdempotencia idempotencia,
    IGestorCacheConsultas cache,
    IGeneradorIdEcheq generadorIdEcheq)
    : CreacionInstrumentoStrategy<CrearEcheqRequest, EcheqResponse>(idempotencia, cache)
{
    protected override TipoInstrumento Tipo => TipoInstrumento.Echeq;

    protected override string MensajeDuplicado(CrearEcheqRequest request)
        => $"Ya existe un echeq con el CUD {request.Cud}.";

    protected override Task<bool> ExisteDuplicadoDeNegocioAsync(CrearEcheqRequest request, CancellationToken ct)
        => repository.ExisteCudAsync(request.Cud, ct);

    protected override async Task<EcheqResponse> PersistirAsync(
        CrearEcheqRequest request, string idempotencyKey, string bodyHash, CancellationToken ct)
    {
        string idEcheq;
        do
        {
            idEcheq = generadorIdEcheq.Generar();
        }
        while (await repository.ExisteIdEcheqAsync(idEcheq, ct));

        var echeq = Echeq.Crear(
            idEcheq,
            request.Cud,
            request.CodigoBanco,
            request.NumeroCuenta,
            request.CuitLibrador,
            request.CuitBeneficiario,
            request.Monto,
            Mapeadores.CodigoAMoneda(request.Moneda),
            request.FechaEmision,
            request.FechaDiferimiento);

        repository.Agregar(echeq);

        var respuesta = echeq.AResponse();
        Idempotencia.Registrar(idempotencyKey, Tipo, bodyHash, JsonSerializer.Serialize(respuesta, JsonOpts));

        await unitOfWork.SaveChangesAsync(ct);
        return respuesta;
    }

    protected override IReadOnlyCollection<string> CuitsAfectados(CrearEcheqRequest request)
        => [request.CuitLibrador, request.CuitBeneficiario];
}
