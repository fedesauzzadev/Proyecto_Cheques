using Coelsa.Domain;
using Coelsa.Domain.ValueObjects;
using Xunit;

namespace Coelsa.UnitTests;

public class Cmc7Tests
{
    private const string Cmc7Valido = "060000114250000123400001234567";

    [Fact]
    public void Crear_AceptaCmc7De30Digitos()
    {
        var cmc7 = Cmc7.Crear(Cmc7Valido);

        Assert.Equal(Cmc7Valido, cmc7.Valor);
    }

    [Fact]
    public void Desglosar_DevuelveLosTramosSegunElSpec()
    {
        // banco(3) + sucursal(4) + código postal(4) + número de cheque(8) + cuenta(11)
        var desglose = Cmc7.Crear(Cmc7Valido).Desglosar();

        Assert.Equal("060", desglose.Banco);
        Assert.Equal("0001", desglose.Sucursal);
        Assert.Equal("1425", desglose.CodigoPostal);
        Assert.Equal("00001234", desglose.NumeroCheque);
        Assert.Equal("00001234567", desglose.NumeroCuenta);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("06000011425000012340000123456")]    // 29 dígitos
    [InlineData("0600001142500001234000012345678")]  // 31 dígitos
    [InlineData("06000011425000012340000123456A")]   // letra
    public void EsValido_RechazaFormatosInvalidos(string? valor)
    {
        Assert.False(Cmc7.EsValido(valor));
    }

    [Fact]
    public void Crear_ConValorInvalido_LanzaValidacionException()
    {
        Assert.Throws<ValidacionException>(() => Cmc7.Crear("123"));
    }
}
