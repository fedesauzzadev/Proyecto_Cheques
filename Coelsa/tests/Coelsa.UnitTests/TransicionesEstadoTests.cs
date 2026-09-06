using Coelsa.Domain;
using Coelsa.Domain.Validaciones;
using Xunit;

namespace Coelsa.UnitTests;

public class TransicionesEstadoTests
{
    [Theory]
    [InlineData(EstadoInstrumento.Emitido, EstadoInstrumento.Depositado)]
    [InlineData(EstadoInstrumento.Emitido, EstadoInstrumento.Anulado)]
    [InlineData(EstadoInstrumento.Depositado, EstadoInstrumento.Compensado)]
    [InlineData(EstadoInstrumento.Depositado, EstadoInstrumento.Rechazado)]
    [InlineData(EstadoInstrumento.Compensado, EstadoInstrumento.Pagado)]
    [InlineData(EstadoInstrumento.Pendiente, EstadoInstrumento.Emitido)]
    [InlineData(EstadoInstrumento.Pendiente, EstadoInstrumento.Repudiado)]
    [InlineData(EstadoInstrumento.Pendiente, EstadoInstrumento.Anulado)]
    [InlineData(EstadoInstrumento.Emitido, EstadoInstrumento.EnCustodia)]
    [InlineData(EstadoInstrumento.EnCustodia, EstadoInstrumento.Emitido)]
    [InlineData(EstadoInstrumento.EnCustodia, EstadoInstrumento.Depositado)]
    public void EsValida_AceptaTransicionesDelDiagrama(EstadoInstrumento desde, EstadoInstrumento hacia)
    {
        Assert.True(TransicionesEstado.EsValida(desde, hacia));
    }

    [Theory]
    [InlineData(EstadoInstrumento.Emitido, EstadoInstrumento.Compensado)]   // salto inválido
    [InlineData(EstadoInstrumento.Emitido, EstadoInstrumento.Pagado)]
    [InlineData(EstadoInstrumento.Anulado, EstadoInstrumento.Depositado)]   // terminal
    [InlineData(EstadoInstrumento.Rechazado, EstadoInstrumento.Compensado)] // terminal
    [InlineData(EstadoInstrumento.Pagado, EstadoInstrumento.Emitido)]       // terminal
    [InlineData(EstadoInstrumento.Compensado, EstadoInstrumento.Rechazado)] // solo desde Depositado
    [InlineData(EstadoInstrumento.Pendiente, EstadoInstrumento.Depositado)] // sin aceptar
    [InlineData(EstadoInstrumento.Pendiente, EstadoInstrumento.Pagado)]
    [InlineData(EstadoInstrumento.EnCustodia, EstadoInstrumento.Compensado)]
    [InlineData(EstadoInstrumento.EnCustodia, EstadoInstrumento.Rechazado)]
    [InlineData(EstadoInstrumento.EnCustodia, EstadoInstrumento.Anulado)]
    [InlineData(EstadoInstrumento.Repudiado, EstadoInstrumento.Emitido)]    // terminal
    [InlineData(EstadoInstrumento.Depositado, EstadoInstrumento.EnCustodia)] // solo desde Emitido
    public void EsValida_RechazaTransicionesInvalidas(EstadoInstrumento desde, EstadoInstrumento hacia)
    {
        Assert.False(TransicionesEstado.EsValida(desde, hacia));
    }

    [Fact]
    public void DestinosDesde_EstadosTerminalesNoTienenDestinos()
    {
        Assert.Empty(TransicionesEstado.DestinosDesde(EstadoInstrumento.Anulado));
        Assert.Empty(TransicionesEstado.DestinosDesde(EstadoInstrumento.Rechazado));
        Assert.Empty(TransicionesEstado.DestinosDesde(EstadoInstrumento.Pagado));
        Assert.Empty(TransicionesEstado.DestinosDesde(EstadoInstrumento.Repudiado));
    }
}
