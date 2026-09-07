using Coelsa.Application.Common;
using Coelsa.Application.Dtos;
using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;

namespace Coelsa.Application.CasosUso;

/// <summary>
/// Aceptación o repudio del beneficiario sobre un echeq pendiente (SPEC Fase A + D2).
/// Aceptar lo pone en circulación (Emitido); repudiar lo deja terminal (Repudiado)
/// y exige el motivo del beneficiario.
/// </summary>
public sealed class AceptarEcheqHandler(
    IInstrumentoRepository<Echeq> repository,
    IUnitOfWork unitOfWork,
    IGestorCacheConsultas cache)
{
    private readonly IInstrumentoRepository<Echeq> _repository = repository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGestorCacheConsultas _cache = cache;

    public async Task<EcheqResponse> Ejecutar(string idecheq, AceptarEcheqRequest request, CancellationToken ct)
    {
        var echeq = await _repository.ObtenerPorIdentificadorAsync(idecheq, ct)
            ?? throw new NoEncontradoException($"No existe ningún echeq con el IDECHEQ {idecheq}.");

        if (request.Aceptada)
        {
            if (!string.IsNullOrWhiteSpace(request.Motivo))
            {
                throw new ValidacionException("La aceptación no admite motivo (solo el repudio lo exige).");
            }

            echeq.Aceptar();
        }
        else
        {
            echeq.Repudiar(request.Motivo);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        await _cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitLibrador, ct);
        await _cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitBeneficiario, ct);

        return echeq.AResponse();
    }
}
