using Coelsa.Application.Common;
using Coelsa.Application.Dtos;
using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;

namespace Coelsa.Application.CasosUso;

/// <summary>
/// Aceptación o repudio del beneficiario sobre un echeq pendiente (SPEC Fase A).
/// Aceptar lo pone en circulación (Emitido); repudiar lo deja terminal (Repudiado).
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
            echeq.Aceptar();
        }
        else
        {
            echeq.Repudiar();
        }

        await _unitOfWork.SaveChangesAsync(ct);

        await _cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitLibrador, ct);
        await _cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitBeneficiario, ct);

        return echeq.AResponse();
    }
}
