using Coelsa.Application.CasosUso;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Coelsa.Domain.Validaciones;
using Xunit;

namespace Coelsa.UnitTests;

public class ListarInstrumentosHandlerTests
{
    private const string CuitLibrador = "2012345678";

    private static string Cuit(string base10) => ValidadorCuit.Completar(base10);

    private static ListarChequesHandler CrearHandler(RepositorioChequesFake repositorio)
        => new(repositorio, new GestorCacheFake());

    private static ChequeFisico NuevoCheque(int indice)
        => ChequeFisico.Crear(
            $"06000011425000{indice:D10}".PadRight(30, '0')[..30],
            Cuit(CuitLibrador),
            Cuit("2787654321"),
            1000m * indice,
            Moneda.Pesos,
            new DateOnly(2026, 9, 1),
            null,
            new DateOnly(2026, 9, 5));

    [Fact]
    public async Task Ejecutar_CuitInvalido_LanzaValidacion()
    {
        await Assert.ThrowsAsync<ValidacionException>(
            () => CrearHandler(new RepositorioChequesFake()).Ejecutar("20123", 1, 10, default));
    }

    [Fact]
    public async Task Ejecutar_PaginacionCorrecta_Con15Cheques()
    {
        var repositorio = new RepositorioChequesFake();
        for (var i = 1; i <= 15; i++)
        {
            repositorio.Agregar(NuevoCheque(i));
        }

        var pagina1 = await CrearHandler(repositorio).Ejecutar(Cuit(CuitLibrador), 1, 10, default);
        var pagina2 = await CrearHandler(repositorio).Ejecutar(Cuit(CuitLibrador), 2, 10, default);

        Assert.Equal(15, pagina1.TotalCount);
        Assert.Equal(2, pagina1.TotalPages);
        Assert.Equal(10, pagina1.Items.Count);
        Assert.Equal(5, pagina2.Items.Count); // 15 totales, 10 en la primera página
    }

    [Fact]
    public async Task Ejecutar_FiltraTambienPorCuitBeneficiario()
    {
        var repositorio = new RepositorioChequesFake();
        repositorio.Agregar(NuevoCheque(1));

        var resultado = await CrearHandler(repositorio).Ejecutar(Cuit("2787654321"), 1, 10, default);

        Assert.Single(resultado.Items);
    }

    [Fact]
    public async Task Ejecutar_NoDevuelveDadosDeBaja()
    {
        var repositorio = new RepositorioChequesFake();
        var cheque = NuevoCheque(1);
        repositorio.Agregar(cheque);
        cheque.Eliminar();

        var resultado = await CrearHandler(repositorio).Ejecutar(Cuit(CuitLibrador), 1, 10, default);

        Assert.Empty(resultado.Items);
        Assert.Equal(0, resultado.TotalCount);
    }
}
