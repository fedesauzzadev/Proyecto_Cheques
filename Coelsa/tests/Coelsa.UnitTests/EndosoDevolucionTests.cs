using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Coelsa.Domain.Validaciones;
using Xunit;

namespace Coelsa.UnitTests;

public class EndosoDevolucionTests
{
    private static readonly Guid EcheqId = Guid.NewGuid();
    private static string Cuit(string base10) => ValidadorCuit.Completar(base10);

    private static Endoso Proponer(int orden = 1)
        => Endoso.Proponer(EcheqId, orden, Cuit("2012345678"), Cuit("2787654321"));

    [Fact]
    public void Endoso_Proponer_QuedaPropuesto()
    {
        var endoso = Proponer();

        Assert.Equal(1, endoso.Orden);
        Assert.Equal(EstadoEndoso.Propuesto, endoso.Estado);
    }

    [Fact]
    public void Endoso_Admitir_PasaAVigente()
    {
        var endoso = Proponer();

        endoso.Admitir();

        Assert.Equal(EstadoEndoso.Vigente, endoso.Estado);
    }

    [Fact]
    public void Endoso_Repudiar_PasaARepudiado()
    {
        var endoso = Proponer();

        endoso.Repudiar();

        Assert.Equal(EstadoEndoso.Repudiado, endoso.Estado);
    }

    [Fact]
    public void Endoso_Anular_SoloPropuesto()
    {
        var endoso = Proponer();
        endoso.Anular();

        Assert.Equal(EstadoEndoso.Anulado, endoso.Estado);
        Assert.Throws<TransicionInvalidaException>(() => endoso.Admitir());
    }

    [Fact]
    public void Endoso_RevertirPorDevolucion_SoloVigente()
    {
        var endoso = Proponer();
        endoso.Admitir();

        endoso.RevertirPorDevolucion();

        Assert.Equal(EstadoEndoso.Revertido, endoso.Estado);
        Assert.Throws<TransicionInvalidaException>(() => Proponer().RevertirPorDevolucion());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Endoso_Proponer_ConOrdenInvalido_LanzaExcepcion(int orden)
    {
        Assert.Throws<ValidacionException>(
            () => Endoso.Proponer(EcheqId, orden, Cuit("2012345678"), Cuit("2787654321")));
    }

    [Fact]
    public void Endoso_Proponer_ConMismoCuit_LanzaExcepcion()
    {
        var cuit = Cuit("2012345678");

        Assert.Throws<ValidacionException>(() => Endoso.Proponer(EcheqId, 1, cuit, cuit));
    }

    [Fact]
    public void Devolucion_Solicitar_QuedaSolicitada()
    {
        var devolucion = Devolucion.Solicitar(EcheqId, 1, Cuit("2012345678"), "Motivo demo");

        Assert.Equal(EstadoDevolucion.Solicitada, devolucion.Estado);
        Assert.Equal("Motivo demo", devolucion.Motivo);
    }

    [Fact]
    public void Devolucion_Aceptar_Rechazar_Anular_SoloSolicitada()
    {
        var aceptada = Devolucion.Solicitar(EcheqId, 1, Cuit("2012345678"), null);
        aceptada.Aceptar();
        Assert.Equal(EstadoDevolucion.Aceptada, aceptada.Estado);

        var rechazada = Devolucion.Solicitar(EcheqId, 2, Cuit("2012345678"), null);
        rechazada.Rechazar();
        Assert.Equal(EstadoDevolucion.Rechazada, rechazada.Estado);

        var anulada = Devolucion.Solicitar(EcheqId, 3, Cuit("2012345678"), null);
        anulada.Anular();
        Assert.Equal(EstadoDevolucion.Anulada, anulada.Estado);

        Assert.Throws<TransicionInvalidaException>(() => aceptada.Rechazar());
        Assert.Throws<TransicionInvalidaException>(() => rechazada.Aceptar());
        Assert.Throws<TransicionInvalidaException>(() => anulada.Aceptar());
    }

    [Fact]
    public void Devolucion_Solicitar_ConMotivoLargo_LanzaExcepcion()
    {
        Assert.Throws<ValidacionException>(
            () => Devolucion.Solicitar(EcheqId, 1, Cuit("2012345678"), new string('x', 281)));
    }
}
