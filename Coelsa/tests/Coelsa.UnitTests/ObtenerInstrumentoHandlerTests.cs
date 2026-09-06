using Coelsa.Application.CasosUso;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Coelsa.Domain.Validaciones;
using Xunit;

namespace Coelsa.UnitTests;

public class ObtenerInstrumentoHandlerTests
{
    private static readonly DateOnly Hoy = new(2026, 9, 5);
    private static string Cuit(string base10) => ValidadorCuit.Completar(base10);

    [Fact]
    public async Task ObtenerEcheq_ConIdEcheqDe11Letras_Devuelve()
    {
        var repositorio = new RepositorioEcheqsFake();
        var echeq = Echeq.Crear(
            "ABCDEFGHIJK", "011000114250000123400001234567", Cuit("2012345678"), Cuit("2787654321"),
            250_000m, Moneda.Dolares, Hoy, null, Hoy.AddDays(30), Hoy);
        repositorio.Agregar(echeq);
        var handler = new ObtenerEcheqHandler(repositorio);

        var respuesta = await handler.Ejecutar("ABCDEFGHIJK", default);

        Assert.Equal("ABCDEFGHIJK", respuesta.Identificador);
    }

    [Fact]
    public async Task ObtenerEcheq_ConFormatoViejoDe18_Lanza400()
    {
        var handler = new ObtenerEcheqHandler(new RepositorioEcheqsFake());

        await Assert.ThrowsAsync<ValidacionException>(
            () => handler.Ejecutar("EQSEED000000000001", default));
    }

    [Fact]
    public async Task ObtenerCheque_ConCmc7Invalido_Lanza400()
    {
        var handler = new ObtenerChequeHandler(new RepositorioChequesFake());

        await Assert.ThrowsAsync<ValidacionException>(() => handler.Ejecutar("123", default));
    }
}
