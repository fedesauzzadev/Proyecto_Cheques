using Coelsa.Domain.Validaciones;
using Xunit;

namespace Coelsa.UnitTests;

public class ValidadorCuitTests
{
    [Theory]
    [InlineData("2012345678")] // 20-12345678-X
    [InlineData("2787654321")]
    [InlineData("3051122233")]
    [InlineData("2033445566")]
    public void Completar_GeneraCuitsValidos(string baseDiez)
    {
        var cuit = ValidadorCuit.Completar(baseDiez);

        Assert.Equal(11, cuit.Length);
        Assert.True(ValidadorCuit.EsValido(cuit));
    }

    [Fact]
    public void EsValido_RechazaCuitConDigitoVerificadorIncorrecto()
    {
        var cuit = ValidadorCuit.Completar("2012345678");
        var cuitFalsificado = cuit[..10] + (cuit[10] == '9' ? '0' : '9');

        Assert.False(ValidadorCuit.EsValido(cuitFalsificado));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("201234567")]        // 9 dígitos
    [InlineData("201234567890")]     // 12 dígitos
    [InlineData("2012345678A")]      // letra
    [InlineData("20 12345678 9")]    // espacios
    public void EsValido_RechazaFormatosInvalidos(string? cuit)
    {
        Assert.False(ValidadorCuit.EsValido(cuit));
    }
}
