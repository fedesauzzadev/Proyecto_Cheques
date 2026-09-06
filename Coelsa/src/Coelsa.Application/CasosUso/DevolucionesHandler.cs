using Coelsa.Application.Common;
using Coelsa.Application.Dtos;
using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;

namespace Coelsa.Application.CasosUso;

/// <summary>
/// Pedidos de devolución de echeqs (SPEC Fase A): cualquier integrante de la
/// cadena distinto del tenedor actual puede solicitarla; el tenedor la acepta
/// o rechaza; el solicitante puede anularla mientras está solicitada.
/// Al aceptarse, la tenencia vuelve al solicitante y se revierten los endosos
/// vigentes posteriores a su posición en la cadena.
/// </summary>
public sealed class SolicitarDevolucionHandler(
    IInstrumentoRepository<Echeq> echeqs,
    IEndosoRepository endosos,
    IDevolucionRepository devoluciones,
    IUnitOfWork unitOfWork,
    IGestorCacheConsultas cache)
{
    public async Task<DevolucionResponse> Ejecutar(string idecheq, SolicitarDevolucionRequest request, CancellationToken ct)
    {
        var echeq = await echeqs.ObtenerPorIdentificadorAsync(idecheq, ct)
            ?? throw new NoEncontradoException($"No existe ningún echeq con el IDECHEQ {idecheq}.");

        if (echeq.Estado != EstadoInstrumento.Emitido)
        {
            throw new TransicionInvalidaException(
                $"Solo se puede pedir la devolución de un echeq en estado Emitido (actual: {echeq.Estado}).");
        }

        if (request.CuitSolicitante == echeq.CuitBeneficiario)
        {
            throw new ValidacionException("El tenedor actual no puede pedirse la devolución a sí mismo.");
        }

        var cadena = await endosos.ListarPorEcheqAsync(echeq.Id, ct);
        var enCadena = request.CuitSolicitante == echeq.CuitLibrador
            || cadena.Any(e => e.CuitEndosante == request.CuitSolicitante || e.CuitEndosatario == request.CuitSolicitante);
        if (!enCadena)
        {
            throw new ValidacionException("Solo un integrante de la cadena del echeq puede pedir su devolución.");
        }

        if (await devoluciones.ExisteSolicitadaAsync(echeq.Id, ct))
        {
            throw new ConflictoDominioException($"El echeq con IDECHEQ {idecheq} ya tiene un pedido de devolución pendiente.");
        }

        var devolucionesPrevias = await devoluciones.ListarPorEcheqAsync(echeq.Id, ct);
        var numero = devolucionesPrevias.Count == 0 ? 1 : devolucionesPrevias.Max(d => d.Numero) + 1;

        var devolucion = Devolucion.Solicitar(echeq.Id, numero, request.CuitSolicitante, request.Motivo);
        devoluciones.Agregar(devolucion);

        await unitOfWork.SaveChangesAsync(ct);

        await cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitLibrador, ct);
        await cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitBeneficiario, ct);
        await cache.InvalidarAsync(TipoInstrumento.Echeq, request.CuitSolicitante, ct);

        return devolucion.AResponse();
    }
}

/// <summary>
/// Resolución de un pedido por el tenedor actual. Al aceptar, la tenencia vuelve
/// al solicitante y se revierten los endosos vigentes posteriores a su posición.
/// </summary>
public sealed class ResolverDevolucionHandler(
    IInstrumentoRepository<Echeq> echeqs,
    IEndosoRepository endosos,
    IDevolucionRepository devoluciones,
    IUnitOfWork unitOfWork,
    IGestorCacheConsultas cache)
{
    public async Task<DevolucionResponse> Ejecutar(string idecheq, int numero, ResolverDevolucionRequest request, CancellationToken ct)
    {
        var echeq = await echeqs.ObtenerPorIdentificadorAsync(idecheq, ct)
            ?? throw new NoEncontradoException($"No existe ningún echeq con el IDECHEQ {idecheq}.");

        var devolucion = await devoluciones.ObtenerPorNumeroAsync(echeq.Id, numero, ct)
            ?? throw new NoEncontradoException($"No existe el pedido de devolución {numero} del echeq con IDECHEQ {idecheq}.");

        if (request.CuitResolutor != echeq.CuitBeneficiario)
        {
            throw new ValidacionException("Solo el tenedor actual puede resolver el pedido de devolución.");
        }

        var tenedorAnterior = echeq.CuitBeneficiario;

        if (request.Aceptada)
        {
            devolucion.Aceptar();

            var cadena = await endosos.ListarPorEcheqAsync(echeq.Id, ct);
            var posicionSolicitante = cadena
                .Where(e => e.CuitEndosatario == devolucion.CuitSolicitante)
                .Select(e => (int?)e.Orden)
                .DefaultIfEmpty(0)
                .Max();

            foreach (var endoso in cadena.Where(e => e.Estado == EstadoEndoso.Vigente && e.Orden > posicionSolicitante))
            {
                endoso.RevertirPorDevolucion();
            }

            echeq.CambiarTenencia(devolucion.CuitSolicitante);
            echeq.FijarCantidadEndosos(cadena.Count(e => e.Estado == EstadoEndoso.Vigente));
        }
        else
        {
            devolucion.Rechazar();
        }

        await unitOfWork.SaveChangesAsync(ct);

        await cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitLibrador, ct);
        await cache.InvalidarAsync(TipoInstrumento.Echeq, tenedorAnterior, ct);
        await cache.InvalidarAsync(TipoInstrumento.Echeq, devolucion.CuitSolicitante, ct);

        return devolucion.AResponse();
    }
}

/// <summary>Anulación de un pedido solicitado por el solicitante.</summary>
public sealed class AnularDevolucionHandler(
    IInstrumentoRepository<Echeq> echeqs,
    IDevolucionRepository devoluciones,
    IUnitOfWork unitOfWork,
    IGestorCacheConsultas cache)
{
    public async Task Ejecutar(string idecheq, int numero, CancellationToken ct)
    {
        var echeq = await echeqs.ObtenerPorIdentificadorAsync(idecheq, ct)
            ?? throw new NoEncontradoException($"No existe ningún echeq con el IDECHEQ {idecheq}.");

        var devolucion = await devoluciones.ObtenerPorNumeroAsync(echeq.Id, numero, ct)
            ?? throw new NoEncontradoException($"No existe el pedido de devolución {numero} del echeq con IDECHEQ {idecheq}.");

        devolucion.Anular();

        await unitOfWork.SaveChangesAsync(ct);

        await cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitLibrador, ct);
        await cache.InvalidarAsync(TipoInstrumento.Echeq, echeq.CuitBeneficiario, ct);
    }
}

/// <summary>Pedidos de devolución del echeq ordenados para trazabilidad.</summary>
public sealed class ListarDevolucionesHandler(
    IInstrumentoRepository<Echeq> echeqs,
    IDevolucionRepository devoluciones)
{
    public async Task<IReadOnlyList<DevolucionResponse>> Ejecutar(string idecheq, CancellationToken ct)
    {
        var echeq = await echeqs.ObtenerPorIdentificadorAsync(idecheq, ct)
            ?? throw new NoEncontradoException($"No existe ningún echeq con el IDECHEQ {idecheq}.");

        var pedidos = await devoluciones.ListarPorEcheqAsync(echeq.Id, ct);
        return pedidos.OrderBy(d => d.Numero).Select(d => d.AResponse()).ToList();
    }
}
