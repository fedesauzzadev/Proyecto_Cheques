using System.Text.Json;
using IrmaRios.Cartera.Application.Common;
using IrmaRios.Cartera.Application.Dtos;
using IrmaRios.Cartera.Application.Puertos;
using IrmaRios.Cartera.Domain;
using IrmaRios.Cartera.Domain.Entidades;
using IrmaRios.Cartera.Domain.Validaciones;
using Microsoft.Extensions.Logging;

namespace IrmaRios.Cartera.Application.CasosUso;

/// <summary>
/// HU-02: depósito de una tanda de instrumentos a cobro. Valida la empresa contra
/// Identidad y cada instrumento contra el clearing (clearing = fuente de verdad).
/// Aceptación parcial: los instrumentos inválidos se reportan con motivo, los
/// válidos ingresan. Idempotente por Idempotency-Key.
/// </summary>
public sealed class DepositarHandler(
    IEmpresas empresas,
    IClearing clearing,
    ICarteraRepository cartera,
    IAlmacenIdempotencia idempotencia,
    IUnitOfWork uow,
    IGestorCacheConsultas cache,
    ILogger<DepositarHandler> logger)
{
    private const int MaxInstrumentosPorTanda = 50;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public async Task<ResultadoIdempotente<DepositoResponse>> Ejecutar(
        DepositoRequest request, string idempotencyKey, CancellationToken ct)
    {
        var bodyHash = Hasher.DelObjeto(request);

        var previo = await idempotencia.ObtenerAsync(idempotencyKey, ct);
        if (previo is not null)
        {
            if (previo.BodyHash != bodyHash)
            {
                throw new ConflictoDominioException("La Idempotency-Key ya fue usada con otro cuerpo de solicitud.");
            }

            return new ResultadoIdempotente<DepositoResponse>(
                JsonSerializer.Deserialize<DepositoResponse>(previo.ResponseJson, JsonOpts)!, EsReplay: true);
        }

        if (!ValidadorCuit.EsValido(request.CuitEmpresa))
        {
            throw new ValidacionException("El CUIT de la empresa depositante es inválido.");
        }

        if (request.Instrumentos is not { Count: > 0 })
        {
            throw new ValidacionException("El depósito debe incluir al menos un instrumento.");
        }

        if (request.Instrumentos.Count > MaxInstrumentosPorTanda)
        {
            throw new ValidacionException($"Máximo {MaxInstrumentosPorTanda} instrumentos por tanda.");
        }

        var duplicados = request.Instrumentos
            .GroupBy(i => (NormalizarTipo(i.Tipo), i.Identificador?.Trim().ToUpperInvariant()))
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicados is not null)
        {
            throw new ValidacionException($"El instrumento {duplicados.Key.Item2} está repetido en la tanda.");
        }

        if (!await empresas.ExisteAsync(request.CuitEmpresa, ct))
        {
            throw new NoEncontradoException(
                $"La empresa {request.CuitEmpresa} no está registrada en el banco.");
        }

        var ahoraUtc = DateTime.UtcNow;
        var hoy = DateOnly.FromDateTime(ahoraUtc);
        var deposito = Deposito.Registrar(request.CuitEmpresa, ahoraUtc);

        var aceptados = new List<InstrumentoAceptadoResponse>();
        var rechazados = new List<InstrumentoRechazadoResponse>();

        foreach (var item in request.Instrumentos)
        {
            var resultado = await ProcesarInstrumento(item, request.CuitEmpresa, hoy, ahoraUtc, deposito, ct);
            if (resultado.Aceptado is not null)
            {
                aceptados.Add(resultado.Aceptado);
            }
            else
            {
                rechazados.Add(resultado.Rechazado!);
            }
        }

        if (aceptados.Count == 0)
        {
            throw new ValidacionException(
                "Ningún instrumento de la tanda pudo ingresar a cartera: " +
                string.Join("; ", rechazados.Select(r => $"{r.Identificador}: {r.Motivo}")));
        }

        var respuesta = new DepositoResponse(deposito.Id, request.CuitEmpresa, aceptados, rechazados);
        cartera.Agregar(deposito);
        idempotencia.Registrar(idempotencyKey, bodyHash, JsonSerializer.Serialize(respuesta, JsonOpts));
        await uow.SaveChangesAsync(ct);

        // Invalida el cache de la empresa para los tipos que recibieron instrumentos.
        foreach (var tipo in aceptados.Select(a => a.Tipo).Distinct())
        {
            await cache.InvalidarAsync(NormalizarTipo(tipo), request.CuitEmpresa, ct);
        }

        logger.LogInformation(
            "Depósito {DepositoId} de {Cuit}: {Aceptados} aceptados, {Rechazados} rechazados.",
            deposito.Id, request.CuitEmpresa, aceptados.Count, rechazados.Count);

        return new ResultadoIdempotente<DepositoResponse>(respuesta, EsReplay: false);
    }

    private async Task<(InstrumentoAceptadoResponse? Aceptado, InstrumentoRechazadoResponse? Rechazado)> ProcesarInstrumento(
        InstrumentoDepositadoRequest item,
        string cuitEmpresa,
        DateOnly hoy,
        DateTime ahoraUtc,
        Deposito deposito,
        CancellationToken ct)
    {
        TipoInstrumento tipo;
        try
        {
            tipo = NormalizarTipo(item.Tipo);
        }
        catch (ValidacionException e)
        {
            return (null, new InstrumentoRechazadoResponse(item.Tipo, item.Identificador ?? "-", e.Message));
        }

        var identificador = item.Identificador?.Trim().ToUpperInvariant() ?? string.Empty;

        var existente = await cartera.ObtenerEnCarteraAsync(tipo, identificador, ct);
        if (existente is not null)
        {
            deposito.AgregarInstrumento(existente.Id);
            return (MapearAceptado(existente), null);
        }

        var clearingData = await clearing.ObtenerAsync(tipo, identificador, ct);
        if (clearingData is null)
        {
            return (null, new InstrumentoRechazadoResponse(
                tipo.ToString(), identificador, "No existe en el clearing."));
        }

        try
        {
            var instrumento = InstrumentoCartera.Ingresar(
                tipo,
                identificador,
                clearingData.CuitLibrador,
                clearingData.CuitBeneficiario,
                clearingData.Monto,
                clearingData.Moneda,
                clearingData.FechaVencimiento,
                clearingData.Estado,
                cuitEmpresa,
                hoy,
                ahoraUtc);

            cartera.Agregar(instrumento);
            deposito.AgregarInstrumento(instrumento.Id);
            return (MapearAceptado(instrumento), null);
        }
        catch (ValidacionException e)
        {
            return (null, new InstrumentoRechazadoResponse(tipo.ToString(), identificador, e.Message));
        }
    }

    internal static TipoInstrumento NormalizarTipo(string? tipo)
    {
        if (Enum.TryParse<TipoInstrumento>(tipo, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new ValidacionException(
            $"El tipo '{tipo}' no es válido. Válidos: {string.Join(", ", Enum.GetNames<TipoInstrumento>())}.");
    }

    private static InstrumentoAceptadoResponse MapearAceptado(InstrumentoCartera i)
        => new(i.Tipo.ToString(), i.Identificador, i.Monto, i.Moneda.ToString(),
            i.FechaVencimiento.ToString("yyyy-MM-dd"), i.EstadoClearing.ToString());
}
