namespace Coelsa.Domain.Validaciones;

/// <summary>
/// Validaciones de dominio comunes a cheques físicos y echeqs (SPEC 5.1 / 5.2).
/// </summary>
public static class ValidacionesInstrumento
{
    public static void ValidarComunes(
        string? cuitLibrador,
        string? cuitBeneficiario,
        decimal monto,
        DateOnly fechaEmision,
        DateOnly? fechaDiferimiento,
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
}
