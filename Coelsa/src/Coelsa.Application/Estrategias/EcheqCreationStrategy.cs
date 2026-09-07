using System.Text.Json;
using Coelsa.Application.Common;
using Coelsa.Application.Dtos;
using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Coelsa.Domain.ValueObjects;

namespace Coelsa.Application.Estrategias;

/// <summary>
/// Estrategia de creación de echeqs (Fase B, RFC-005): exige cuenta de débito
/// con chequera vigente, verifica titularidad (librador = titular) y moneda,
/// reserva el número de la chequera y deriva CMC7 + IDECHEQ. En la operatoria
/// real ambos identificadores los informa el sistema al emitir.
/// </summary>
public class EcheqCreationStrategy(
    IEcheqRepository repository,
    ICuentaRepository cuentas,
    IChequeraRepository chequeras,
    IUnitOfWork unitOfWork,
    IAlmacenIdempotencia idempotencia,
    IGestorCacheConsultas cache,
    IGeneradorIdEcheq generadorIdEcheq)
    : CreacionInstrumentoStrategy<CrearEcheqRequest, EcheqResponse>(idempotencia, cache)
{
    protected override TipoInstrumento Tipo => TipoInstrumento.Echeq;

    protected override string MensajeDuplicado(CrearEcheqRequest request)
        => "Conflicto de identificadores generados al crear el echeq (reintentar).";

    protected override Task<bool> ExisteDuplicadoDeNegocioAsync(CrearEcheqRequest request, CancellationToken ct)
        => Task.FromResult(false);

    protected override async Task<EcheqResponse> PersistirAsync(
        CrearEcheqRequest request, string idempotencyKey, string bodyHash, CancellationToken ct)
    {
        var cuenta = await cuentas.ObtenerPorCbuAsync(request.CbuEmisor, ct)
            ?? throw new ValidacionException(
                $"La cuenta con CBU {request.CbuEmisor} no existe o no está habilitada para emitir echeqs.");

        if (!string.Equals(cuenta.CuitTitular, request.CuitLibrador, StringComparison.Ordinal))
        {
            throw new ValidacionException(
                "El CUIT/CUIL del librador debe coincidir con el titular de la cuenta de débito.");
        }

        var moneda = Mapeadores.CodigoAMoneda(request.Moneda);
        if (cuenta.Moneda != moneda)
        {
            throw new ValidacionException(
                "La moneda del echeq debe coincidir con la moneda de la cuenta de débito.");
        }

        var chequera = await chequeras.ObtenerConLugarAsync(cuenta.Id, ct)
            ?? throw new ConflictoDominioException(
                "La cuenta no tiene chequera con números disponibles. Solicite una chequera nueva.");

        var numeroCheque = chequera.ReservarNumero();
        var cmc7 = Cmc7.Derivar(request.CbuEmisor, numeroCheque).Valor;

        string idEcheq;
        do
        {
            idEcheq = generadorIdEcheq.Generar();
        }
        while (await repository.ExisteIdEcheqAsync(idEcheq, ct));

        var echeq = Echeq.Crear(
            idEcheq,
            request.CbuEmisor,
            chequera.Numero,
            numeroCheque,
            Mapeadores.CodigoACaracter(request.Caracter),
            Mapeadores.CodigoATipoDoc(request.TipoDocBeneficiario),
            request.NombreLibrador,
            request.NombreBeneficiario,
            cmc7,
            request.CuitLibrador,
            request.CuitBeneficiario,
            request.Monto,
            moneda,
            request.FechaEmision,
            request.FechaDiferimiento,
            request.FechaVencimiento,
            hoy: null,
            concepto: request.Concepto,
            motivo: request.Motivo,
            referencia: request.Referencia,
            emailNotificacion: request.EmailNotificacion);

        repository.Agregar(echeq);

        var respuesta = echeq.AResponse();
        Idempotencia.Registrar(idempotencyKey, Tipo, bodyHash, JsonSerializer.Serialize(respuesta, JsonOpts));

        await unitOfWork.SaveChangesAsync(ct);
        return respuesta;
    }

    protected override IReadOnlyCollection<string> CuitsAfectados(CrearEcheqRequest request)
        => [request.CuitLibrador, request.CuitBeneficiario];
}
