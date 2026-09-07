using Coelsa.Application.Common;
using Coelsa.Application.Dtos;
using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Coelsa.Domain.Validaciones;

namespace Coelsa.Application.CasosUso;

/// <summary>
/// Cambio de estado con máquina de estados (SPEC RF-05) e invalidación de cache
/// por CUIT del librador y del beneficiario.
/// </summary>
public abstract class CambiarEstadoHandler<TEntidad>(
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

    protected abstract void AplicarCambio(TEntidad entidad, EstadoInstrumento nuevoEstado, MotivoRechazo? motivoRechazo);

    public async Task Ejecutar(string identificador, CambiarEstadoRequest request, CancellationToken ct)
    {
        var entidad = await _repository.ObtenerPorIdentificadorAsync(identificador, ct)
            ?? throw new NoEncontradoException(MensajeNoEncontrado(identificador));

        var nuevoEstado = Mapeadores.ParseEstado(request.Estado);
        var motivoRechazo = Mapeadores.ParseMotivoRechazo(request.MotivoRechazo);

        if (nuevoEstado == EstadoInstrumento.Depositado)
        {
            ValidacionesInstrumento.ValidarVentanaPresentacion(
                entidad.FechaVencimiento, DateOnly.FromDateTime(DateTime.UtcNow));
        }

        AplicarCambio(entidad, nuevoEstado, motivoRechazo);

        await _unitOfWork.SaveChangesAsync(ct);

        await _cache.InvalidarAsync(Tipo, entidad.CuitLibrador, ct);
        await _cache.InvalidarAsync(Tipo, entidad.CuitBeneficiario, ct);
    }
}

public sealed class CambiarEstadoChequeHandler(
    IInstrumentoRepository<ChequeFisico> repository,
    IUnitOfWork unitOfWork,
    IGestorCacheConsultas cache)
    : CambiarEstadoHandler<ChequeFisico>(repository, unitOfWork, cache)
{
    protected override TipoInstrumento Tipo => TipoInstrumento.ChequeFisico;

    protected override string MensajeNoEncontrado(string identificador)
        => $"No existe ningún cheque físico con el CMC7 {identificador}.";

    protected override void AplicarCambio(ChequeFisico entidad, EstadoInstrumento nuevoEstado, MotivoRechazo? motivoRechazo)
        => entidad.CambiarEstado(nuevoEstado, motivoRechazo);
}

public sealed class CambiarEstadoEcheqHandler(
    IInstrumentoRepository<Echeq> repository,
    IUnitOfWork unitOfWork,
    IGestorCacheConsultas cache)
    : CambiarEstadoHandler<Echeq>(repository, unitOfWork, cache)
{
    protected override TipoInstrumento Tipo => TipoInstrumento.Echeq;

    protected override string MensajeNoEncontrado(string identificador)
        => $"No existe ningún echeq con el IDECHEQ {identificador}.";

    protected override void AplicarCambio(Echeq entidad, EstadoInstrumento nuevoEstado, MotivoRechazo? motivoRechazo)
        => entidad.CambiarEstado(nuevoEstado, motivoRechazo);
}
