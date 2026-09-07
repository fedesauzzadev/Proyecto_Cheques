using Coelsa.Application.CasosUso;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Coelsa.Domain.Validaciones;
using Xunit;

namespace Coelsa.UnitTests;

public class CertificadoTests
{
    private readonly RepositorioEcheqsFake _echeqs = new();

    private Echeq NuevoRechazado()
    {
        var echeq = FabricaEcheqs.Crear();
        echeq.Aceptar();
        echeq.CambiarEstado(EstadoInstrumento.Depositado, null);
        echeq.CambiarEstado(EstadoInstrumento.Rechazado, MotivoRechazo.FaltaDeFondos);
        _echeqs.Agregar(echeq);
        return echeq;
    }

    [Fact]
    public async Task Obtener_Rechazado_DevuelveCudDe64HexYCodigo()
    {
        var echeq = NuevoRechazado();
        var handler = new ObtenerCertificadoHandler(_echeqs);

        var primera = await handler.Ejecutar("ABCDEFGHIJK", default);
        var segunda = await handler.Ejecutar("ABCDEFGHIJK", default);

        Assert.Matches("^[0-9A-F]{64}$", primera.Cud);
        Assert.Equal(primera.Cud[..12], primera.CodigoVisualizacion);
        Assert.Equal(primera.Cud, segunda.Cud); // determinista
        Assert.Equal("ABCDEFGHIJK", primera.IdEcheq);
        Assert.Equal(11, primera.MotivoRechazo);
        Assert.Equal(echeq.Cmc7, primera.Cmc7);
        Assert.NotNull(primera.FechaRechazo);
    }

    [Fact]
    public async Task Obtener_NoRechazado_Lanza422()
    {
        var echeq = FabricaEcheqs.Crear();
        echeq.Aceptar();
        _echeqs.Agregar(echeq);
        var handler = new ObtenerCertificadoHandler(_echeqs);

        await Assert.ThrowsAsync<TransicionInvalidaException>(
            () => handler.Ejecutar("ABCDEFGHIJK", default));
    }

    [Fact]
    public async Task Obtener_Inexistente_Lanza404()
    {
        var handler = new ObtenerCertificadoHandler(_echeqs);

        await Assert.ThrowsAsync<NoEncontradoException>(
            () => handler.Ejecutar("ZZZZZZZZZZZ", default));
    }

    [Fact]
    public async Task Obtener_IdEcheqInvalido_LanzaValidacion()
    {
        var handler = new ObtenerCertificadoHandler(_echeqs);

        await Assert.ThrowsAsync<ValidacionException>(
            () => handler.Ejecutar("123", default));
    }

    [Fact]
    public void CalcularCud_CambiaConLosDatos()
    {
        var base_ = FabricaEcheqs.Crear();
        var otroMonto = FabricaEcheqs.Crear(numeroCheque: 2);

        Assert.NotEqual(base_.CalcularCud(), otroMonto.CalcularCud());
    }
}
