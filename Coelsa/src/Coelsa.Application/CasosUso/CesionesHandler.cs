using Coelsa.Application.Common;
using Coelsa.Application.Dtos;
using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;

namespace Coelsa.Application.CasosUso;

/// <summary>
/// Cesiones de echeqs "no a la orden" (Fase D1): el tenedor actual cede a un
/// tercero bancarizado indicando su domicilio; el cesionario la acepta o
/// rechaza; el cedente puede anularla mientras está solicitada. Al aceptarse,
/// la tenencia pasa al cesionario. Tope de 10 cesiones no anuladas.
/// </summary>
public sealed class SolicitarCesionHandler(
    IInstrumentoRepository<Echeq> echeqs,
    ICesionRepository cesiones,
    IUnitOfWork unitOfWork,
    IGestorCacheConsultas cache)
{
    public async Task<CesionResponse> Ejecutar(string idecheq, SolicitarCesionRequest request, CancellationToken ct)
    {
        var echeq = await echeqs.ObtenerPorIdentificadorAsync(idecheq, ct)
            ?? throw new NoEncontradoException($"No existe ningún echeq con el IDECHEQ {idecheq}.");

        if (echeq.Estado != EstadoInstrumento.Emitido)
        {
            throw new TransicionInvalidaException(
                $"Solo se puede ceder un echeq en estado Emitido (actual: {echeq.Estado}).");
        }

        if (echeq.Caracter != Caracter.NoAlaOrden)
        {
            throw new TransicionInvalidaException(
                "Solo se pueden ceder echeqs con carácter 'No a la orden' (los 'A la orden' se endosan).");
        }

        if (await cesiones.ExisteSolicitadaAsync(echeq.Id, ct))
        {
            throw new ConflictoDominioException($"El echeq con IDECHEQ {idecheq} ya tiene una cesión pendiente.");
        }

        var previas = await cesiones.ListarPorEcheqAsync(echeq.Id, ct);
        if (previas.Count(c => c.Estado != EstadoCesion.Anulada) >= Cesion.MaximoCesiones)
        {
            throw new ConflictoDominioException(
                $"El echeq con IDECHEQ {idecheq} ya alcanzó el máximo de {Cesion.MaximoCesiones} cesiones.");
        }

        var numero = previas.Count == 0 ? 1 : previas.Max(c => c.Numero) + 1;

        var cesion = Cesion.Solicitar(
            echeq.Id, numero, echeq.CuitBeneficiario, request.CuitCesionario, request.DomicilioCesionario);
        cesiones.Agregar(cesion);

        await unitOfWork.SaveChangesAsync(ct);

        await cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitLibrador, ct);
        await cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitBeneficiario, ct);
        await cache.InvalidarAsync(TipoInstrumento.Echeq, request.CuitCesionario, ct);

        return cesion.AResponse();
    }
}

/// <summary>
/// Resolución de una cesión por el cesionario. Al aceptar, la tenencia pasa al cesionario.
/// </summary>
public sealed class ResolverCesionHandler(
    IInstrumentoRepository<Echeq> echeqs,
    ICesionRepository cesiones,
    IUnitOfWork unitOfWork,
    IGestorCacheConsultas cache)
{
    public async Task<CesionResponse> Ejecutar(string idecheq, int numero, ResolverCesionRequest request, CancellationToken ct)
    {
        var echeq = await echeqs.ObtenerPorIdentificadorAsync(idecheq, ct)
            ?? throw new NoEncontradoException($"No existe ningún echeq con el IDECHEQ {idecheq}.");

        var cesion = await cesiones.ObtenerPorNumeroAsync(echeq.Id, numero, ct)
            ?? throw new NoEncontradoException($"No existe la cesión {numero} del echeq con IDECHEQ {idecheq}.");

        if (request.CuitResolutor != cesion.CuitCesionario)
        {
            throw new ValidacionException("Solo el cesionario puede resolver la cesión.");
        }

        var tenedorAnterior = echeq.CuitBeneficiario;

        if (request.Aceptada)
        {
            cesion.Aceptar();
            echeq.CambiarTenencia(cesion.CuitCesionario);
        }
        else
        {
            cesion.Rechazar();
        }

        await unitOfWork.SaveChangesAsync(ct);

        await cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitLibrador, ct);
        await cache.InvalidarAsync(TipoInstrumento.Echeq, tenedorAnterior, ct);
        await cache.InvalidarAsync(TipoInstrumento.Echeq, cesion.CuitCesionario, ct);

        return cesion.AResponse();
    }
}

/// <summary>Anulación de una cesión solicitada por el cedente.</summary>
public sealed class AnularCesionHandler(
    IInstrumentoRepository<Echeq> echeqs,
    ICesionRepository cesiones,
    IUnitOfWork unitOfWork,
    IGestorCacheConsultas cache)
{
    public async Task Ejecutar(string idecheq, int numero, CancellationToken ct)
    {
        var echeq = await echeqs.ObtenerPorIdentificadorAsync(idecheq, ct)
            ?? throw new NoEncontradoException($"No existe ningún echeq con el IDECHEQ {idecheq}.");

        var cesion = await cesiones.ObtenerPorNumeroAsync(echeq.Id, numero, ct)
            ?? throw new NoEncontradoException($"No existe la cesión {numero} del echeq con IDECHEQ {idecheq}.");

        cesion.Anular();

        await unitOfWork.SaveChangesAsync(ct);

        await cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitLibrador, ct);
        await cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitBeneficiario, ct);
    }
}

/// <summary>Cesiones del echeq ordenadas para trazabilidad.</summary>
public sealed class ListarCesionesHandler(
    IInstrumentoRepository<Echeq> echeqs,
    ICesionRepository cesiones)
{
    public async Task<IReadOnlyList<CesionResponse>> Ejecutar(string idecheq, CancellationToken ct)
    {
        var echeq = await echeqs.ObtenerPorIdentificadorAsync(idecheq, ct)
            ?? throw new NoEncontradoException($"No existe ningún echeq con el IDECHEQ {idecheq}.");

        var lista = await cesiones.ListarPorEcheqAsync(echeq.Id, ct);
        return lista.OrderBy(c => c.Numero).Select(c => c.AResponse()).ToList();
    }
}
