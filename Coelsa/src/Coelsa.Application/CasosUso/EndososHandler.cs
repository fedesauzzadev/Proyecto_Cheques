using Coelsa.Application.Common;
using Coelsa.Application.Dtos;
using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;

namespace Coelsa.Application.CasosUso;

/// <summary>
/// Endosos de echeqs con ciclo completo (SPEC Fase A): el tenedor propone,
/// el endosatario admite o repudia, el endosante puede anular lo propuesto.
/// La cadena completa queda disponible para trazabilidad.
/// </summary>
public sealed class ProponerEndosoHandler(
    IInstrumentoRepository<Echeq> echeqs,
    IEndosoRepository endosos,
    IUnitOfWork unitOfWork,
    IGestorCacheConsultas cache)
{
    public async Task<EndosoResponse> Ejecutar(string idecheq, ProponerEndosoRequest request, CancellationToken ct)
    {
        var echeq = await echeqs.ObtenerPorIdentificadorAsync(idecheq, ct)
            ?? throw new NoEncontradoException($"No existe ningún echeq con el IDECHEQ {idecheq}.");

        if (echeq.Estado != EstadoInstrumento.Emitido)
        {
            throw new TransicionInvalidaException(
                $"Solo se puede endosar un echeq en estado Emitido (actual: {echeq.Estado}).");
        }

        var existentes = await endosos.ListarPorEcheqAsync(echeq.Id, ct);
        if (existentes.Count(e => e.Estado != EstadoEndoso.Anulado) >= Endoso.MaximoEndosos)
        {
            throw new ConflictoDominioException(
                $"El echeq con IDECHEQ {idecheq} ya alcanzó el máximo de {Endoso.MaximoEndosos} endosos.");
        }

        var orden = existentes.Count == 0 ? 1 : existentes.Max(e => e.Orden) + 1;
        var endoso = Endoso.Proponer(echeq.Id, orden, echeq.CuitBeneficiario, request.CuitEndosatario);
        endosos.Agregar(endoso);

        await unitOfWork.SaveChangesAsync(ct);

        await cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitLibrador, ct);
        await cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitBeneficiario, ct);
        await cache.InvalidarAsync(TipoInstrumento.Echeq, request.CuitEndosatario, ct);

        return endoso.AResponse();
    }
}

/// <summary>
/// Admisión o repudio de un endoso propuesto. Solo el endosatario puede resolver.
/// Al admitir, la tenencia pasa al endosatario y se refresca el contador.
/// </summary>
public sealed class ResolverEndosoHandler(
    IInstrumentoRepository<Echeq> echeqs,
    IEndosoRepository endosos,
    IUnitOfWork unitOfWork,
    IGestorCacheConsultas cache)
{
    public async Task<EndosoResponse> Ejecutar(string idecheq, int orden, ResolverEndosoRequest request, CancellationToken ct)
    {
        var echeq = await echeqs.ObtenerPorIdentificadorAsync(idecheq, ct)
            ?? throw new NoEncontradoException($"No existe ningún echeq con el IDECHEQ {idecheq}.");

        var endoso = await endosos.ObtenerPorOrdenAsync(echeq.Id, orden, ct)
            ?? throw new NoEncontradoException($"No existe el endoso {orden} del echeq con IDECHEQ {idecheq}.");

        if (request.Cuit != endoso.CuitEndosatario)
        {
            throw new ValidacionException("Solo el endosatario puede resolver el endoso.");
        }

        if (request.Admitido)
        {
            var tenedorAnterior = echeq.CuitBeneficiario;
            endoso.Admitir();
            echeq.CambiarTenencia(endoso.CuitEndosatario);
            var vigentes = (await endosos.ListarPorEcheqAsync(echeq.Id, ct))
                .Count(e => e.Estado == EstadoEndoso.Vigente);
            echeq.FijarCantidadEndosos(vigentes);

            await unitOfWork.SaveChangesAsync(ct);

            await cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitLibrador, ct);
            await cache.InvalidarAsync(TipoInstrumento.Echeq, tenedorAnterior, ct);
            await cache.InvalidarAsync(TipoInstrumento.Echeq, endoso.CuitEndosatario, ct);

            return endoso.AResponse();
        }

        endoso.Repudiar();

        await unitOfWork.SaveChangesAsync(ct);

        await cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitLibrador, ct);
        await cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitBeneficiario, ct);

        return endoso.AResponse();
    }
}

/// <summary>Anulación de un endoso propuesto por el endosante.</summary>
public sealed class AnularEndosoHandler(
    IInstrumentoRepository<Echeq> echeqs,
    IEndosoRepository endosos,
    IUnitOfWork unitOfWork,
    IGestorCacheConsultas cache)
{
    public async Task Ejecutar(string idecheq, int orden, CancellationToken ct)
    {
        var echeq = await echeqs.ObtenerPorIdentificadorAsync(idecheq, ct)
            ?? throw new NoEncontradoException($"No existe ningún echeq con el IDECHEQ {idecheq}.");

        var endoso = await endosos.ObtenerPorOrdenAsync(echeq.Id, orden, ct)
            ?? throw new NoEncontradoException($"No existe el endoso {orden} del echeq con IDECHEQ {idecheq}.");

        endoso.Anular();

        await unitOfWork.SaveChangesAsync(ct);

        await cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitLibrador, ct);
        await cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitBeneficiario, ct);
    }
}

/// <summary>Cadena de endosos del echeq ordenada para trazabilidad.</summary>
public sealed class ListarEndososHandler(
    IInstrumentoRepository<Echeq> echeqs,
    IEndosoRepository endosos)
{
    public async Task<IReadOnlyList<EndosoResponse>> Ejecutar(string idecheq, CancellationToken ct)
    {
        var echeq = await echeqs.ObtenerPorIdentificadorAsync(idecheq, ct)
            ?? throw new NoEncontradoException($"No existe ningún echeq con el IDECHEQ {idecheq}.");

        var cadena = await endosos.ListarPorEcheqAsync(echeq.Id, ct);
        return cadena.OrderBy(e => e.Orden).Select(e => e.AResponse()).ToList();
    }
}
