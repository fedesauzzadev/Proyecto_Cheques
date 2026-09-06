using System.Text.Json;
using IrmaRios.Cartera.Application.Puertos;
using IrmaRios.Cartera.Domain;
using Microsoft.Extensions.Logging;

namespace IrmaRios.Cartera.Infrastructure.Integracion;

/// <summary>
/// Adaptador HTTP del antipuerto IClearing contra el simulador COELSA
/// (fuente de verdad de instrumentos). Tolerante al shape real de la API
/// (moneda "P"/"D" o numérica, estado por nombre). 404 =&gt; null (no existe);
/// otro error =&gt; IntegracionException (502 al llamador).
/// </summary>
public class ClearingCoelsaApi(HttpClient http, ILogger<ClearingCoelsaApi> logger) : IClearing
{
    public async Task<InstrumentoClearing?> ObtenerAsync(TipoInstrumento tipo, string identificador, CancellationToken ct)
    {
        var ruta = tipo == TipoInstrumento.ChequeFisico
            ? $"/api/v1/cheques/{identificador}"
            : $"/api/v1/echeqs/{identificador}";

        HttpResponseMessage respuesta;
        try
        {
            respuesta = await http.GetAsync(ruta, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Clearing inaccesible al consultar {Ruta}.", ruta);
            throw new IntegracionException("El clearing (COELSA) no está disponible. Reintente en unos instantes.");
        }

        if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            logger.LogError("Clearing respondió {Estado} al consultar {Ruta}.", (int)respuesta.StatusCode, ruta);
            throw new IntegracionException($"El clearing respondió {(int)respuesta.StatusCode} al consultar el instrumento.");
        }

        using var documento = await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var raiz = documento.RootElement;

        return new InstrumentoClearing(
            Identificador: raiz.TryGetProperty("identificador", out var id) ? id.GetString()! : identificador,
            CuitLibrador: raiz.GetProperty("cuitLibrador").GetString()!,
            CuitBeneficiario: raiz.GetProperty("cuitBeneficiario").GetString()!,
            Monto: raiz.GetProperty("monto").GetDecimal(),
            Moneda: MapearMoneda(raiz.GetProperty("moneda")),
            FechaVencimiento: DateOnly.Parse(raiz.GetProperty("fechaVencimiento").GetString()!),
            Estado: MapearEstado(raiz.GetProperty("estado").GetString()!));
    }

    private static Moneda MapearMoneda(JsonElement elemento)
    {
        return elemento.ValueKind switch
        {
            JsonValueKind.String when elemento.GetString()!.StartsWith('P') => Moneda.Pesos,
            JsonValueKind.String when elemento.GetString()!.StartsWith('D') => Moneda.Dolares,
            JsonValueKind.String when elemento.GetString()!.StartsWith('1') => Moneda.Pesos,
            JsonValueKind.String when elemento.GetString()!.StartsWith('2') => Moneda.Dolares,
            JsonValueKind.Number when elemento.GetInt32() == 1 => Moneda.Pesos,
            _ => Moneda.Dolares
        };
    }

    private static EstadoClearing MapearEstado(string estado)
        => Enum.TryParse<EstadoClearing>(estado, ignoreCase: true, out var parsed)
            ? parsed
            : EstadoClearing.Emitido;
}
