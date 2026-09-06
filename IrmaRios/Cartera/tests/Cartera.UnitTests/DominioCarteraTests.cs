using IrmaRios.Cartera.Domain;
using IrmaRios.Cartera.Domain.Entidades;
using IrmaRios.Cartera.Domain.Validaciones;

namespace IrmaRios.Cartera.UnitTests;

public class DominioCarteraTests
{
    private static readonly string CuitEmpresa = ValidadorCuit.Completar("2033445566");

    private static InstrumentoCartera Ingresar(
        EstadoClearing estado = EstadoClearing.Emitido,
        string? beneficiario = null,
        DateOnly? vencimiento = null,
        decimal monto = 150000m)
        => InstrumentoCartera.Ingresar(
            TipoInstrumento.ChequeFisico,
            "060000114250000001234567890123",
            ValidadorCuit.Completar("2012345678"),
            beneficiario ?? CuitEmpresa,
            monto,
            Moneda.Pesos,
            vencimiento ?? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
            estado,
            CuitEmpresa,
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateTime.UtcNow);

    [Fact]
    public void Ingresar_con_beneficiario_distinto_a_la_empresa_rechaza()
    {
        var ex = Assert.Throws<ValidacionException>(() => Ingresar(beneficiario: ValidadorCuit.Completar("2787654321")));
        Assert.Contains("a favor del CUIT", ex.Message);
    }

    [Theory]
    [InlineData(EstadoClearing.Rechazado)]
    [InlineData(EstadoClearing.Anulado)]
    [InlineData(EstadoClearing.Pagado)]
    [InlineData(EstadoClearing.Compensado)]
    public void Ingresar_con_estado_no_negociable_rechaza(EstadoClearing estado)
    {
        var ex = Assert.Throws<ValidacionException>(() => Ingresar(estado: estado));
        Assert.Contains("no es negociable", ex.Message);
    }

    [Theory]
    [InlineData(EstadoClearing.Emitido)]
    [InlineData(EstadoClearing.Pendiente)]
    public void Ingresar_con_estado_negociable_acepta(EstadoClearing estado)
    {
        var instrumento = Ingresar(estado: estado);
        Assert.Equal(EstadoCartera.EnCartera, instrumento.Estado);
    }

    [Fact]
    public void Ingresar_instrumento_vencido_rechaza()
    {
        var ex = Assert.Throws<ValidacionException>(
            () => Ingresar(vencimiento: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1)));
        Assert.Contains("venció", ex.Message);
    }

    [Fact]
    public void Ingresar_cmc7_mal_formado_rechaza()
    {
        Assert.Throws<ValidacionException>(
            () => IngresarConIdentificador(TipoInstrumento.ChequeFisico, "0600001"));
    }

    [Fact]
    public void Ingresar_idEcheq_mal_formado_rechaza()
    {
        Assert.Throws<ValidacionException>(
            () => IngresarConIdentificador(TipoInstrumento.Echeq, "ABC123"));
    }

    [Fact]
    public void Deposito_no_acepta_instrumento_repetido()
    {
        var deposito = Deposito.Registrar(CuitEmpresa, DateTime.UtcNow);
        var id = Guid.NewGuid();
        deposito.AgregarInstrumento(id);

        Assert.Throws<ValidacionException>(() => deposito.AgregarInstrumento(id));
    }

    private static void IngresarConIdentificador(TipoInstrumento tipo, string identificador)
        => InstrumentoCartera.Ingresar(
            tipo, identificador,
            ValidadorCuit.Completar("2012345678"), CuitEmpresa, 100m, Moneda.Pesos,
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10), EstadoClearing.Emitido,
            CuitEmpresa, DateOnly.FromDateTime(DateTime.UtcNow), DateTime.UtcNow);
}
