using System.Text.RegularExpressions;

namespace IrmaRios.Identidad.Domain.Validaciones;

/// <summary>
/// Validación de CUIT/CUIL compartida con el simulador COELSA (mismo algoritmo módulo 11).
/// </summary>
public static partial class ValidadorCuit
{
    public const int Longitud = 11;

    [GeneratedRegex(@"^\d{11}$")]
    private static partial Regex PatronCuit();

    private static readonly int[] Pesos = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];

    public static bool EsValido(string? cuit)
    {
        if (cuit is null || cuit.Length != Longitud || !PatronCuit().IsMatch(cuit))
        {
            return false;
        }

        return CalcularDigitoVerificador(cuit[..10]) == cuit[10] - '0';
    }

    /// <summary>Dígito verificador módulo 11: resto 0 =&gt; 0, resto 1 =&gt; 9, resto n =&gt; 11 - n.</summary>
    public static int CalcularDigitoVerificador(string primerosDiezDigitos)
    {
        var suma = 0;
        for (var i = 0; i < 10; i++)
        {
            suma += (primerosDiezDigitos[i] - '0') * Pesos[i];
        }

        var resto = suma % 11;
        return resto == 0 ? 0 : resto == 1 ? 9 : 11 - resto;
    }

    /// <summary>Completa una base de 10 dígitos con su dígito verificador (útil para seeds y tests).</summary>
    public static string Completar(string baseDiezDigitos)
    {
        return baseDiezDigitos + CalcularDigitoVerificador(baseDiezDigitos);
    }
}
