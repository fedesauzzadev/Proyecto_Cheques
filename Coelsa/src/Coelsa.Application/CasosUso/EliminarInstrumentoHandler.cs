using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;

namespace Coelsa.Application.CasosUso;

/// <summary>
/// Baja lógica (SPEC RF-06) con invalidación de cache por CUIT.
/// </summary>
public abstract class EliminarInstrumentoHandler<TEntidad>(
    IInstrumentoRepository<TEntidad> repository,
    IUnitOfWork unitOfWork,
    IGestorCacheConsultas cache)
    where TEntidad : class, IInstrumento
{
    private readonly IInstrumentoRepository<TEntidad> _repository = repository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGestorCacheConsultas _cache = cache;

    protected abstract TipoInstrumento Tipo { get; }

    protected abstract string MensajeNoEncontrado(string identificador);

    public async Task Ejecutar(string identificador, CancellationToken ct)
    {
        var entidad = await _repository.ObtenerPorIdentificadorAsync(identificador, ct)
            ?? throw new NoEncontradoException(MensajeNoEncontrado(identificador));

        entidad.Eliminar();

        await _unitOfWork.SaveChangesAsync(ct);

        await _cache.InvalidarAsync(Tipo, entidad.CuitLibrador, ct);
        await _cache.InvalidarAsync(Tipo, entidad.CuitBeneficiario, ct);
    }
}

public sealed class EliminarChequeHandler(
    IInstrumentoRepository<ChequeFisico> repository,
    IUnitOfWork unitOfWork,
    IGestorCacheConsultas cache)
    : EliminarInstrumentoHandler<ChequeFisico>(repository, unitOfWork, cache)
{
    protected override TipoInstrumento Tipo => TipoInstrumento.ChequeFisico;

    protected override string MensajeNoEncontrado(string identificador)
        => $"No existe ningún cheque físico con el CMC7 {identificador}.";
}

public sealed class EliminarEcheqHandler(
    IInstrumentoRepository<Echeq> repository,
    IUnitOfWork unitOfWork,
    IGestorCacheConsultas cache)
    : EliminarInstrumentoHandler<Echeq>(repository, unitOfWork, cache)
{
    protected override TipoInstrumento Tipo => TipoInstrumento.Echeq;

    protected override string MensajeNoEncontrado(string identificador)
        => $"No existe ningún echeq con el IDECHEQ {identificador}.";
}
