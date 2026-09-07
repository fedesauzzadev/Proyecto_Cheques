using System.Text.RegularExpressions;

namespace Coelsa.Domain.Validaciones;

public static partial class ValidadorCbu
{
    public const int Longitud = 22;

    [GeneratedRegex(@"^\d{22}$")]
    private static partial Regex PatronCbu();

    // Pesos oficiales BCRA: bloque 1 (entidad 3 + sucursal 4), bloque 2 (cuenta 13).
    private static readonly int[] PesosBloque1 = [7, 1, 3, 9, 7, 1, 3];
    private static readonly int[] PesosBloque2 = [3, 9, 7, 1, 3, 9, 7, 1, 3, 9, 7, 1, 3];

    public static bool EsValido(string? cbu)
    {
        if (cbu is null || cbu.Length != Longitud || !PatronCbu().IsMatch(cbu))
        {
            return false;
        }

        return DigitoVerificador(cbu[..7], PesosBloque1) == cbu[7] - '0'
            && DigitoVerificador(cbu.Substring(8, 13), PesosBloque2) == cbu[21] - '0';
    }

    /// <summary>Dígito verificador de un bloque: (10 - (suma % 10)) % 10.</summary>
    public static int DigitoVerificador(string bloqueSinDigito, int[] pesos)
    {
        var suma = 0;
        for (var i = 0; i < bloqueSinDigito.Length; i++)
        {
            suma += (bloqueSinDigito[i] - '0') * pesos[i];
        }

        return (10 - suma % 10) % 10;
    }

    /// <summary>Arma un CBU válido desde entidad (3) + sucursal (4) + cuenta (13).</summary>
    public static string Crear(string entidad, string sucursal, string cuenta)
    {
        var bloque1 = entidad + sucursal;
        var dv1 = DigitoVerificador(bloque1, PesosBloque1);
        var dv2 = DigitoVerificador(cuenta, PesosBloque2);
        return $"{bloque1}{dv1}{cuenta}{dv2}";
    }

    public static string Banco(string cbu) => cbu[..3];

    public static string Sucursal(string cbu) => cbu.Substring(3, 4);

    /// <summary>Bloque de cuenta de 13 dígitos (posiciones 9-21 del CBU).</summary>
    public static string NumeroCuenta(string cbu) => cbu.Substring(8, 13);

    /// <summary>Últimos 11 dígitos de la cuenta: tramo "cuenta" del CMC7.</summary>
    public static string CuentaCmc7(string cbu) => cbu.Substring(10, 11);
}
