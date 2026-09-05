using System.Security.Cryptography;

namespace Coelsa.Application;

/// <summary>
/// Genera IDECHEQ: prefijo "EQ" + 16 caracteres alfanuméricos = 18 (SPEC 5.2).
/// </summary>
public interface IGeneradorIdEcheq
{
    string Generar();
}

public class GeneradorIdEcheq : IGeneradorIdEcheq
{
    private const string Alfabeto = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int LongitudAleatoria = 16;

    public string Generar()
    {
        Span<char> caracteres = stackalloc char[LongitudAleatoria];
        Span<byte> bytes = stackalloc byte[LongitudAleatoria];
        RandomNumberGenerator.Fill(bytes);

        for (var i = 0; i < LongitudAleatoria; i++)
        {
            caracteres[i] = Alfabeto[bytes[i] % Alfabeto.Length];
        }

        return "EQ" + new string(caracteres);
    }
}
