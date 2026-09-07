namespace Coelsa.Domain.Validaciones;

/// <summary>
/// Validaciones de dominio comunes a cheques físicos y echeqs (SPEC 5.1 / 5.2).
/// </summary>
public static class ValidacionesInstrumento
{
    /// <summary>Plazo máximo entre emisión y vencimiento: 360 días (BCRA).</summary>
    public const int TenorMaximoDias = 360;

    /// <summary>Plazo de presentación al cobro desde el vencimiento: 30 días.</summary>
    public const int PlazoPresentacionDias = 30;

    public const int LongitudConcepto = 60;
    public const int LongitudMotivo = 280;
    public const int LongitudReferencia = 60;
    public const int LongitudEmail = 160;
    public static void ValidarComunes(
        string? cuitLibrador,
        string? cuitBeneficiario,
        decimal monto,
        DateOnly fechaEmision,
        DateOnly? fechaDiferimiento,
        DateOnly fechaVencimiento,
        DateOnly? hoy = null)
    {
        hoy ??= DateOnly.FromDateTime(DateTime.UtcNow);

        if (!ValidadorCuit.EsValido(cuitLibrador))
        {
            throw new ValidacionException($"El CUIT/CUIL del librador '{cuitLibrador}' no es válido (se espera 11 dígitos con verificador módulo 11).");
        }

        if (!ValidadorCuit.EsValido(cuitBeneficiario))
        {
            throw new ValidacionException($"El CUIT/CUIL del beneficiario '{cuitBeneficiario}' no es válido (se espera 11 dígitos con verificador módulo 11).");
        }

        if (monto <= 0)
        {
            throw new ValidacionException("El monto debe ser mayor a cero.");
        }

        if (fechaEmision > hoy.Value.AddDays(1))
        {
            throw new ValidacionException($"La fecha de emisión {fechaEmision:yyyy-MM-dd} no puede ser mayor a {hoy.Value.AddDays(1):yyyy-MM-dd}.");
        }

        if (fechaDiferimiento.HasValue && fechaDiferimiento.Value < fechaEmision)
        {
            throw new ValidacionException($"La fecha de diferimiento {fechaDiferimiento.Value:yyyy-MM-dd} no puede ser anterior a la fecha de emisión {fechaEmision:yyyy-MM-dd}.");
        }

        if (fechaVencimiento <= fechaEmision)
        {
            throw new ValidacionException($"La fecha de vencimiento {fechaVencimiento:yyyy-MM-dd} debe ser posterior a la fecha de emisión {fechaEmision:yyyy-MM-dd}.");
        }

        if (fechaVencimiento.DayNumber - fechaEmision.DayNumber > TenorMaximoDias)
        {
            throw new ValidacionException(
                $"El plazo entre la emisión y el vencimiento no puede superar los {TenorMaximoDias} días.");
        }

        if (fechaDiferimiento.HasValue && fechaVencimiento <= fechaDiferimiento.Value)
        {
            throw new ValidacionException($"La fecha de vencimiento {fechaVencimiento:yyyy-MM-dd} debe ser posterior a la fecha de diferimiento {fechaDiferimiento.Value:yyyy-MM-dd}.");
        }
    }

    public static void ValidarEstadoCambio(EstadoInstrumento actual, EstadoInstrumento nuevo, MotivoRechazo? motivo)
    {
        if (!TransicionesEstado.EsValida(actual, nuevo))
        {
            throw new TransicionInvalidaException(
                $"No se puede pasar del estado {actual} al estado {nuevo}. " +
                $"Destinos válidos desde {actual}: {(TransicionesEstado.DestinosDesde(actual).Count == 0 ? "ninguno (estado terminal)" : string.Join(", ", TransicionesEstado.DestinosDesde(actual)))}.");
        }

        if (nuevo == EstadoInstrumento.Rechazado && motivo is null)
        {
            throw new ValidacionException("El estado Rechazado exige un motivo de rechazo.");
        }

        if (nuevo != EstadoInstrumento.Rechazado && motivo is not null)
        {
            throw new ValidacionException("Solo el estado Rechazado admite un motivo de rechazo.");
        }
    }

    /// <summary>
    /// Ventana de presentación al cobro (Fase B5): el depósito solo procede
    /// dentro de los 30 días posteriores al vencimiento; después el echeq caduca.
    /// </summary>
    public static void ValidarVentanaPresentacion(DateOnly fechaVencimiento, DateOnly hoy)
    {
        if (hoy.DayNumber - fechaVencimiento.DayNumber > PlazoPresentacionDias)
        {
            throw new TransicionInvalidaException(
                $"El plazo de presentación al cobro ({PlazoPresentacionDias} días desde el vencimiento) ya pasó.");
        }
    }

    /// <summary>Normaliza un campo opcional de gestión (concepto, motivo, referencia, email).</summary>
    public static string? NormalizarGestion(string? valor, int maximo, string campo, bool esEmail = false)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var normalizado = valor.Trim();
        if (normalizado.Length > maximo)
        {
            throw new ValidacionException($"El campo '{campo}' debe tener hasta {maximo} caracteres.");
        }

        if (esEmail && !System.Text.RegularExpressions.Regex.IsMatch(normalizado, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new ValidacionException($"El campo '{campo}' debe ser un email válido.");
        }

        return normalizado;
    }
}
