namespace Coelsa.Domain.ValueObjects;

public sealed record DesgloseCmc7(
    string Banco,
    string Sucursal,
    string CodigoPostal,
    string NumeroCheque,
    string NumeroCuenta);

/// <summary>
/// CMC7: código magnetizable de 30 dígitos de la banda inferior del cheque físico.
/// banco(3) + sucursal(4) + código postal(4) + número de cheque(8) + cuenta(11).
/// Identificador único del cheque físico.
/// </summary>
public sealed class Cmc7
{
    public const int Longitud = 30;

    public string Valor { get; }

    private Cmc7(string valor)
    {
        Valor = valor;
    }

    public static bool EsValido(string? valor)
    {
        return valor is not null
            && valor.Length == Longitud
            && valor.All(char.IsDigit);
    }

    public static Cmc7 Crear(string? valor)
    {
        if (!EsValido(valor))
        {
            throw new ValidacionException(
                $"El CMC7 debe ser un código magnetizable de {Longitud} dígitos " +
                "(banco + sucursal + código postal + número de cheque + cuenta).");
        }

        return new Cmc7(valor!);
    }

    public DesgloseCmc7 Desglosar() => new(
        Valor[..3],
        Valor.Substring(3, 4),
        Valor.Substring(7, 4),
        Valor.Substring(11, 8),
        Valor.Substring(19, 11));

    /// <summary>
    /// Deriva el CMC7 del echeq desde la cuenta de débito (Fase B): banco(3) +
    /// sucursal(4) del CBU + código postal 2077 (rango echeqs) + número de
    /// cheque de la chequera (8) + últimos 11 dígitos de la cuenta.
    /// </summary>
    public static Cmc7 Derivar(string cbu, int numeroCheque)
    {
        if (!Validaciones.ValidadorCbu.EsValido(cbu))
        {
            throw new ValidacionException(
                "El CBU debe tener 22 dígitos con verificadores válidos (entidad + sucursal + cuenta).");
        }

        if (numeroCheque < 1 || numeroCheque > 99_999_999)
        {
            throw new ValidacionException("El número de cheque debe estar entre 1 y 99999999.");
        }

        var valor = string.Concat(
            Validaciones.ValidadorCbu.Banco(cbu),
            Validaciones.ValidadorCbu.Sucursal(cbu),
            "2077",
            numeroCheque.ToString("D8"),
            Validaciones.ValidadorCbu.CuentaCmc7(cbu));

        return new Cmc7(valor);
    }
}
