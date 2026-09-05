using System.Text.Json;
using Coelsa.Application.Common;
using Coelsa.Application.Dtos;
using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;

namespace Coelsa.Application.Estrategias;

/// <summary>
/// Estrategia de creación de cheques físicos: valida CMC7 único y bien formado (SPEC RF-01).
/// </summary>
public class ChequeFisicoCreationStrategy(
    IInstrumentoRepository<ChequeFisico> repository,
    IUnitOfWork unitOfWork,
    IAlmacenIdempotencia idempotencia,
    IGestorCacheConsultas cache)
    : CreacionInstrumentoStrategy<CrearChequeFisicoRequest, ChequeResponse>(idempotencia, cache)
{
    protected override TipoInstrumento Tipo => TipoInstrumento.ChequeFisico;

    protected override string MensajeDuplicado(CrearChequeFisicoRequest request)
        => $"Ya existe un cheque físico con el CMC7 {request.Cmc7}.";

    protected override Task<bool> ExisteDuplicadoDeNegocioAsync(CrearChequeFisicoRequest request, CancellationToken ct)
        => repository.ExistePorIdentificadorAsync(request.Cmc7, ct);

    protected override async Task<ChequeResponse> PersistirAsync(
        CrearChequeFisicoRequest request, string idempotencyKey, string bodyHash, CancellationToken ct)
    {
        var cheque = ChequeFisico.Crear(
            request.Cmc7,
            request.CuitLibrador,
            request.CuitBeneficiario,
            request.Monto,
            Mapeadores.CodigoAMoneda(request.Moneda),
            request.FechaEmision,
            request.FechaDiferimiento);

        repository.Agregar(cheque);

        var respuesta = cheque.AResponse();
        Idempotencia.Registrar(idempotencyKey, Tipo, bodyHash, JsonSerializer.Serialize(respuesta, JsonOpts));

        await unitOfWork.SaveChangesAsync(ct);
        return respuesta;
    }

    protected override IReadOnlyCollection<string> CuitsAfectados(CrearChequeFisicoRequest request)
        => [request.CuitLibrador, request.CuitBeneficiario];
}
