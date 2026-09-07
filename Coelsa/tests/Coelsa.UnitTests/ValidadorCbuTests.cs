using Coelsa.Domain.Validaciones;
using Xunit;

namespace Coelsa.UnitTests;

public class ValidadorCbuTests
{
    // CBU construido a mano: entidad 011 + sucursal 0001 (dv 3) + cuenta 0000000000001 (dv 7).
    public const string CbuValido = "0110001300000000000017";

    [Fact]
    public void EsValido_AceptaCbuConVerificadoresCorrectos()
    {
        Assert.True(ValidadorCbu.EsValido(CbuValido));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("0110001300000000000018")] // dígito verificador del bloque 2 alterado
    [InlineData("0110001400000000000017")] // dígito verificador del bloque 1 alterado
    [InlineData("011000130000000000001A")] // no numérico
    public void EsValido_RechazaCbuMalFormado(string? cbu)
    {
        Assert.False(ValidadorCbu.EsValido(cbu));
    }

    [Fact]
    public void Crear_GeneraCbuValidoConBloquesDados()
    {
        var cbu = ValidadorCbu.Crear("011", "0001", "0000000000001");

        Assert.Equal(CbuValido, cbu);
        Assert.True(ValidadorCbu.EsValido(cbu));
    }

    [Fact]
    public void Desglose_ExtraeBancoSucursalYCuenta()
    {
        Assert.Equal("011", ValidadorCbu.Banco(CbuValido));
        Assert.Equal("0001", ValidadorCbu.Sucursal(CbuValido));
        Assert.Equal("0000000000001", ValidadorCbu.NumeroCuenta(CbuValido));
        Assert.Equal("00000000001", ValidadorCbu.CuentaCmc7(CbuValido));
    }
}
