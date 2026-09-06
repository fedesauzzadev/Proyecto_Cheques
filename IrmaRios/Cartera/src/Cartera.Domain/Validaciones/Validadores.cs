using System.Text.RegularExpressions;

namespace IrmaRios.Cartera.Domain.Validaciones;

/// <summary>Identificadores de negocio: CMC7 (cheque físico, 30 dígitos) e IDECHEQ (echeq, 11 letras).</summary>
public static partial class ValidadorIdentificadores
{
    [GeneratedRegex(@"^\d{30}$")]
    private static partial Regex PatronCmc7();

    [GeneratedRegex(@"^[A-Z]{11}$")]
    private static partial Regex PatronIdEcheq();

    public static bool EsCmc7Valido(string? cmc7) => cmc7 is not null && PatronCmc7().IsMatch(cmc7);

    public static bool EsIdEcheqValido(string? idEcheq) => idEcheq is not null && PatronIdEcheq().IsMatch(idEcheq);

    public static void Validar(TipoInstrumento tipo, string identificador)
    {
        var valido = tipo == TipoInstrumento.ChequeFisico ? EsCmc7Valido(identificador) : EsIdEcheqValido(identificador);
        if (!valido)
        {
            throw new ValidacionException(tipo == TipoInstrumento.ChequeFisico
                ? "El identificador del cheque físico debe ser un CMC7 de 30 dígitos."
                : "El identificador del echeq debe ser un IDECHEQ de 11 letras mayúsculas.");
        }
    }
}

/// <summary>Validación de CUIT/CUIL (mismo algoritmo módulo 11 que el ecosistema).</summary>
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

    public static string Completar(string baseDiezDigitos)
        => baseDiezDigitos + CalcularDigitoVerificador(baseDiezDigitos);
}
