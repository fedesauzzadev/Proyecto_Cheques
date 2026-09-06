using System.Text.Json.Serialization;

namespace IrmaRios.Cartera.Application.Dtos;

public sealed record InstrumentoDepositadoRequest(string Tipo, string Identificador);

/// <summary>Tanda de instrumentos a ingresar en cartera (HU-02).</summary>
public sealed record DepositoRequest(string CuitEmpresa, List<InstrumentoDepositadoRequest> Instrumentos);

public sealed record InstrumentoAceptadoResponse(
    string Tipo,
    string Identificador,
    decimal Monto,
    string Moneda,
    string FechaVencimiento,
    string EstadoClearing);

public sealed record InstrumentoRechazadoResponse(string Tipo, string Identificador, string Motivo);

public sealed record DepositoResponse(
    Guid DepositoId,
    string CuitEmpresa,
    List<InstrumentoAceptadoResponse> Aceptados,
    List<InstrumentoRechazadoResponse> Rechazados);

public sealed record InstrumentoCarteraResponse(
    string Tipo,
    string Identificador,
    string CuitLibrador,
    string CuitBeneficiario,
    string TitularCuit,
    decimal Monto,
    string Moneda,
    string FechaVencimiento,
    string EstadoClearing,
    string EstadoCartera);

public sealed record PagedResponse<T>
{
    public List<T> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    [JsonIgnore]
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
