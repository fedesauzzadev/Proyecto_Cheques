using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Coelsa.Domain.Validaciones;
using Coelsa.Domain.ValueObjects;
using Xunit;

namespace Coelsa.UnitTests;

public class CuentasTests
{
    private static readonly DateOnly Hoy = new(2026, 9, 5);
    private static string CuitValido(string base10) => ValidadorCuit.Completar(base10);

    private static string CbuValido(int i = 1)
        => ValidadorCbu.Crear("011", $"{i:D4}", $"{i:D13}");

    private static Echeq CrearEcheqConCuenta(string cbu, int numeroChequera, int numeroCheque)
        => Echeq.Crear(
            "ABCDEFGHIJK",
            cbu,
            numeroChequera,
            numeroCheque,
            Caracter.AlaOrden,
            TipoDocumento.Cuit,
            "Librador Demo S.A.",
            "Beneficiario Demo S.A.",
            Cmc7.Derivar(cbu, numeroCheque).Valor,
            CuitValido("2012345678"),
            CuitValido("2787654321"),
            250_000m,
            Moneda.Pesos,
            Hoy,
            null,
            Hoy.AddDays(30),
            Hoy);

    [Fact]
    public void Cuenta_Crear_AceptaCbuYCuitValidos()
    {
        var cuenta = Cuenta.Crear(CbuValido(), CuitValido("2012345678"), "Alfa S.R.L.", Moneda.Pesos);

        Assert.Equal("011", cuenta.Banco);
        Assert.Equal("0001", cuenta.Sucursal);
        Assert.Equal("Alfa S.R.L.", cuenta.NombreTitular);
        Assert.True(cuenta.Activa);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("0110001300000000000018")]
    public void Cuenta_Crear_ConCbuInvalido_LanzaExcepcion(string cbu)
    {
        Assert.Throws<ValidacionException>(() => Cuenta.Crear(cbu, CuitValido("2012345678"), "Alfa S.R.L.", Moneda.Pesos));
    }

    [Fact]
    public void Cuenta_Crear_SinNombre_LanzaExcepcion()
    {
        Assert.Throws<ValidacionException>(() => Cuenta.Crear(CbuValido(), CuitValido("2012345678"), "  ", Moneda.Pesos));
    }

    [Theory]
    [InlineData("CUIT", true)]
    [InlineData("CUIL", true)]
    [InlineData("CDI", true)]
    [InlineData("DNI", false)]
    [InlineData(null, false)]
    public void Documento_CodigoATipo_SoloAceptaCuitCuilCdi(string? codigo, bool esperado)
    {
        Assert.Equal(esperado, ValidadorDocumento.CodigoATipo(codigo) is not null);
    }

    [Fact]
    public void Documento_EsValido_AceptaCdiDe11Digitos()
    {
        Assert.True(ValidadorDocumento.EsValido(TipoDocumento.Cdi, CuitValido("2012345678")));
        Assert.False(ValidadorDocumento.EsValido("CUIT", "123"));
    }

    [Fact]
    public void Chequera_ReservarNumero_EntregaSecuenciaDel1Al50YSeAgota()
    {
        var chequera = Chequera.Solicitar(Guid.NewGuid(), 1);

        for (var i = 1; i <= Chequera.CantidadNumeros; i++)
        {
            Assert.Equal(i, chequera.ReservarNumero());
        }

        Assert.Equal(EstadoChequera.Agotada, chequera.Estado);
        Assert.Throws<ConflictoDominioException>(() => chequera.ReservarNumero());
    }

    [Fact]
    public void Cmc7_Derivar_ComponeBancoSucursalCpNumeroYCuenta()
    {
        var cbu = ValidadorCbu.Crear("011", "0001", "0000000000001");

        var cmc7 = Cmc7.Derivar(cbu, 42);

        Assert.Equal("011" + "0001" + "2077" + "00000042" + "00000000001", cmc7.Valor);
        Assert.True(Cmc7.EsValido(cmc7.Valor));
    }

    [Fact]
    public void Echeq_Crear_GuardaCbuChequeraYNumero()
    {
        var cbu = CbuValido();

        var echeq = CrearEcheqConCuenta(cbu, 2, 7);

        Assert.Equal(cbu, echeq.CbuEmisor);
        Assert.Equal(2, echeq.NumeroChequera);
        Assert.Equal(7, echeq.NumeroCheque);
        Assert.Equal("00000007", echeq.DesglosarCmc7().NumeroCheque);
    }

    [Fact]
    public void Echeq_Crear_ConCmc7NoDerivado_LanzaExcepcion()
    {
        var cbu = CbuValido();

        Assert.Throws<ValidacionException>(() => Echeq.Crear(
            "ABCDEFGHIJK",
            cbu,
            1,
            7,
            Caracter.AlaOrden,
            TipoDocumento.Cuit,
            "Librador Demo S.A.",
            "Beneficiario Demo S.A.",
            "060000114250000123400001234567", // CMC7 de otra cuenta
            CuitValido("2012345678"),
            CuitValido("2787654321"),
            250_000m,
            Moneda.Pesos,
            Hoy,
            null,
            Hoy.AddDays(30),
            Hoy));
    }
}
