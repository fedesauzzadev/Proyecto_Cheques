using Coelsa.Domain;
using Coelsa.Domain.Validaciones;

namespace Coelsa.Application.Dtos;

/// <summary>
/// Filtros opcionales del listado de echeqs (Fase B6, espejo de la consulta BEE:
/// CBU emisor + estado + rangos de emisión/pago de hasta 360 días + número).
/// El CUIT sigue siendo obligatorio (librador o beneficiario).
/// </summary>
public sealed record FiltrosEcheq(
    string? Cbu,
    string? Estado,
    DateOnly? DesdeEmision,
    DateOnly? HastaEmision,
    DateOnly? DesdeVencimiento,
    DateOnly? HastaVencimiento,
    int? NumeroCheque)
{
    public EstadoInstrumento? EstadoParseado { get; private set; }

    /// <summary>Valida formato de filtros y los rangos (tope 360 días por eje).</summary>
    public FiltrosEcheq Validado()
    {
        if (Cbu is not null && !ValidadorCbu.EsValido(Cbu))
        {
            throw new ValidacionException("El filtro 'cbu' debe ser un CBU válido de 22 dígitos.");
        }

        EstadoInstrumento? estado = null;
        if (Estado is not null)
        {
            estado = Common.Mapeadores.ParseEstado(Estado);
        }

        ValidarRango(DesdeEmision, HastaEmision, "emisión");
        ValidarRango(DesdeVencimiento, HastaVencimiento, "vencimiento");

        if (NumeroCheque is < 1)
        {
            throw new ValidacionException("El filtro 'numeroCheque' debe ser mayor a cero.");
        }

        return this with { EstadoParseado = estado };
    }

    private static void ValidarRango(DateOnly? desde, DateOnly? hasta, string eje)
    {
        if (desde is null || hasta is null)
        {
            return;
        }

        if (hasta < desde)
        {
            throw new ValidacionException($"El rango de {eje} es inválido ('hasta' anterior a 'desde').");
        }

        if (hasta.Value.DayNumber - desde.Value.DayNumber > ValidacionesInstrumento.TenorMaximoDias)
        {
            throw new ValidacionException(
                $"El rango de {eje} no puede superar los {ValidacionesInstrumento.TenorMaximoDias} días.");
        }
    }

    /// <summary>Fragmento estable para la clave de cache (SPEC sección 7).</summary>
    public string ClaveCache()
        => string.Join("|",
            Cbu ?? "-", Estado ?? "-", DesdeEmision?.ToString("yyyyMMdd") ?? "-",
            HastaEmision?.ToString("yyyyMMdd") ?? "-", DesdeVencimiento?.ToString("yyyyMMdd") ?? "-",
            HastaVencimiento?.ToString("yyyyMMdd") ?? "-", NumeroCheque?.ToString() ?? "-");
}
