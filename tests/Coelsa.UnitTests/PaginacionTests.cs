using Coelsa.Application.Common;
using Coelsa.Domain;
using Xunit;

namespace Coelsa.UnitTests;

public class PaginacionTests
{
    [Fact]
    public void Normalizar_AplicaValoresPorDefecto()
    {
        var (page, pageSize) = Paginacion.Normalizar(null, null);

        Assert.Equal(1, page);
        Assert.Equal(10, pageSize);
    }

    [Fact]
    public void Normalizar_RespetaValoresExplicitos()
    {
        var (page, pageSize) = Paginacion.Normalizar(3, 50);

        Assert.Equal(3, page);
        Assert.Equal(50, pageSize);
    }

    [Fact]
    public void Normalizar_AceptaLimiteMaximo()
    {
        var (_, pageSize) = Paginacion.Normalizar(1, 100);

        Assert.Equal(100, pageSize);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -5)]
    [InlineData(1, 101)]
    public void Normalizar_ConValoresFueraDeRango_LanzaExcepcion(int page, int pageSize)
    {
        Assert.Throws<ValidacionException>(() => Paginacion.Normalizar(page, pageSize));
    }
}
