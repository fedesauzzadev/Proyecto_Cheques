using System.Security.Cryptography;

namespace Coelsa.Application;

/// <summary>
/// Genera IDECHEQ: 11 letras mayúsculas aleatorias (SPEC 5.2).
/// </summary>
public interface IGeneradorIdEcheq
{
    string Generar();
}

public class GeneradorIdEcheq : IGeneradorIdEcheq
{
    private const string Alfabeto = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int Longitud = 11;

    public string Generar()
    {
        Span<char> caracteres = stackalloc char[Longitud];
        Span<byte> bytes = stackalloc byte[Longitud];
        RandomNumberGenerator.Fill(bytes);

        for (var i = 0; i < Longitud; i++)
        {
            caracteres[i] = Alfabeto[bytes[i] % Alfabeto.Length];
        }

        return new string(caracteres);
    }
}
