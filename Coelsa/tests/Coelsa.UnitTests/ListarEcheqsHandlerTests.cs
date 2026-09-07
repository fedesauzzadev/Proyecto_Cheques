using Coelsa.Application.CasosUso;
using Coelsa.Application.Dtos;
using Coelsa.Domain;
using Coelsa.Domain.Validaciones;
using Xunit;

namespace Coelsa.UnitTests;

public class ListarEcheqsHandlerTests
{
    private static string Cuit(string base10) => ValidadorCuit.Completar(base10);

    private static FiltrosEcheq SinFiltros()
        => new(null, null, null, null, null, null, null);

    private static ListarEcheqsHandler CrearHandler(RepositorioEcheqsFake repositorio)
        => new(repositorio, new GestorCacheFake());

    private static void Sembrar(RepositorioEcheqsFake repositorio)
    {
        // Dos echeqs del mismo librador, distinto CBU/estado/fechas/número.
        var primero = FabricaEcheqs.Crear("AAAAAAAAAAA", numeroCheque: 1);
        primero.Aceptar();
        var segundo = FabricaEcheqs.Crear("BBBBBBBBBBB", numeroCheque: 2);
        repositorio.Agregar(primero);
        repositorio.Agregar(segundo);
    }

    [Fact]
    public async Task Ejecutar_SinFiltros_DevuelveAmbos()
    {
        var repositorio = new RepositorioEcheqsFake();
        Sembrar(repositorio);

        var resultado = await CrearHandler(repositorio)
            .Ejecutar(Cuit("2012345678"), SinFiltros(), 1, 10, default);

        Assert.Equal(2, resultado.TotalCount);
    }

    [Fact]
    public async Task Ejecutar_PorEstado_FiltraPendienteYEmitido()
    {
        var repositorio = new RepositorioEcheqsFake();
        Sembrar(repositorio);
        var handler = CrearHandler(repositorio);

        var pendientes = await handler.Ejecutar(
            Cuit("2012345678"), SinFiltros() with { Estado = "Pendiente" }, 1, 10, default);
        var emitidos = await handler.Ejecutar(
            Cuit("2012345678"), SinFiltros() with { Estado = "Emitido" }, 1, 10, default);

        Assert.Single(pendientes.Items);
        Assert.Single(emitidos.Items);
    }

    [Fact]
    public async Task Ejecutar_PorCbu_Filtra()
    {
        var repositorio = new RepositorioEcheqsFake();
        Sembrar(repositorio);

        var mismo = await CrearHandler(repositorio).Ejecutar(
            Cuit("2012345678"), SinFiltros() with { Cbu = FabricaEcheqs.Cbu }, 1, 10, default);
        var otro = await CrearHandler(repositorio).Ejecutar(
            Cuit("2012345678"),
            SinFiltros() with { Cbu = ValidadorCbu.Crear("011", "9999", "0000000000001") },
            1, 10, default);

        Assert.Equal(2, mismo.TotalCount);
        Assert.Equal(0, otro.TotalCount);
    }

    [Fact]
    public async Task Ejecutar_PorNumeroCheque_Filtra()
    {
        var repositorio = new RepositorioEcheqsFake();
        Sembrar(repositorio);

        var resultado = await CrearHandler(repositorio).Ejecutar(
            Cuit("2012345678"), SinFiltros() with { NumeroCheque = 2 }, 1, 10, default);

        Assert.Single(resultado.Items);
        Assert.Equal("BBBBBBBBBBB", resultado.Items[0].Identificador);
    }

    [Fact]
    public async Task Ejecutar_PorRangoEmision_Filtra()
    {
        var repositorio = new RepositorioEcheqsFake();
        Sembrar(repositorio);
        // Ambos emitidos el 2026-09-05: el rango que lo excluye da vacío.
        var resultado = await CrearHandler(repositorio).Ejecutar(
            Cuit("2012345678"),
            SinFiltros() with
            {
                DesdeEmision = new DateOnly(2026, 9, 6),
                HastaEmision = new DateOnly(2026, 9, 10)
            },
            1, 10, default);

        Assert.Equal(0, resultado.TotalCount);
    }

    [Theory]
    [InlineData(" Boga ")]
    [InlineData("123")]
    public async Task Ejecutar_ConCbuInvalido_LanzaValidacion(string cbu)
    {
        await Assert.ThrowsAsync<ValidacionException>(
            () => CrearHandler(new RepositorioEcheqsFake()).Ejecutar(
                Cuit("2012345678"), SinFiltros() with { Cbu = cbu }, 1, 10, default));
    }

    [Fact]
    public async Task Ejecutar_ConEstadoInvalido_LanzaValidacion()
    {
        await Assert.ThrowsAsync<ValidacionException>(
            () => CrearHandler(new RepositorioEcheqsFake()).Ejecutar(
                Cuit("2012345678"), SinFiltros() with { Estado = "Volando" }, 1, 10, default));
    }

    [Fact]
    public async Task Ejecutar_ConRangoMayorA360Dias_LanzaValidacion()
    {
        await Assert.ThrowsAsync<ValidacionException>(
            () => CrearHandler(new RepositorioEcheqsFake()).Ejecutar(
                Cuit("2012345678"),
                SinFiltros() with
                {
                    DesdeEmision = new DateOnly(2025, 1, 1),
                    HastaEmision = new DateOnly(2026, 9, 5)
                },
                1, 10, default));
    }

    [Fact]
    public async Task Ejecutar_ConRangoInvertido_LanzaValidacion()
    {
        await Assert.ThrowsAsync<ValidacionException>(
            () => CrearHandler(new RepositorioEcheqsFake()).Ejecutar(
                Cuit("2012345678"),
                SinFiltros() with
                {
                    DesdeEmision = new DateOnly(2026, 9, 10),
                    HastaEmision = new DateOnly(2026, 9, 1)
                },
                1, 10, default));
    }
}
