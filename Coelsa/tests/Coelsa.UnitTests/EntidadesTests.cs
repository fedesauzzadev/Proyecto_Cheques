using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Coelsa.Domain.Validaciones;
using Xunit;

namespace Coelsa.UnitTests;

public class EntidadesTests
{
    private static readonly DateOnly Hoy = new(2026, 9, 5);
    private const string CuitLibrador = "2012345678";
    private const string CuitBeneficiario = "2787654321";
    private static string CuitValido(string base10) => ValidadorCuit.Completar(base10);

    private static string Cmc7Valido(string banco = "060") => $"{banco}000114250000123400001234567";

    private static ChequeFisico CrearCheque(
        string? cmc7 = "060000114250000123400001234567",
        string? cuitLibrador = CuitLibrador,
        string? cuitBeneficiario = CuitBeneficiario,
        decimal? monto = 100_000m,
        DateOnly? fechaEmision = null,
        DateOnly? fechaDiferimiento = null)
        => ChequeFisico.Crear(
            cmc7!,
            CuitValido(cuitLibrador ?? CuitLibrador),
            CuitValido(cuitBeneficiario ?? CuitBeneficiario),
            monto!.Value,
            Moneda.Pesos,
            fechaEmision ?? Hoy,
            fechaDiferimiento,
            Hoy);

    private static Echeq CrearEcheq(
        string? idEcheq = "ABCDEFGHIJK",
        string? cmc7 = null,
        string? cuitLibrador = CuitLibrador,
        string? cuitBeneficiario = CuitBeneficiario,
        decimal? monto = 250_000m)
        => Echeq.Crear(
            idEcheq!,
            cmc7 ?? Cmc7Valido("011"),
            CuitValido(cuitLibrador ?? CuitLibrador),
            CuitValido(cuitBeneficiario ?? CuitBeneficiario),
            monto!.Value,
            Moneda.Dolares,
            Hoy,
            null,
            Hoy);

    [Fact]
    public void ChequeFisico_Crear_QuedaEnEstadoEmitidoYActivo()
    {
        var cheque = CrearCheque();

        Assert.Equal(EstadoInstrumento.Emitido, cheque.Estado);
        Assert.True(cheque.Activo);
        Assert.Equal("060", cheque.DesglosarCmc7().Banco);
    }

    [Theory]
    [InlineData("06000011425000012340000123456A")]
    [InlineData("123")]
    public void ChequeFisico_Crear_ConCmc7Invalido_LanzaExcepcion(string cmc7)
    {
        Assert.Throws<ValidacionException>(() => CrearCheque(cmc7: cmc7));
    }

    [Fact]
    public void ChequeFisico_Crear_ConCuitInvalido_LanzaExcepcion()
    {
        Assert.Throws<ValidacionException>(() => ChequeFisico.Crear(
            "060000114250000123400001234567",
            "99999999999", // dígito verificador inválido
            CuitValido(CuitBeneficiario),
            100_000m,
            Moneda.Pesos,
            Hoy,
            null,
            Hoy));
    }

    [Fact]
    public void ChequeFisico_Crear_ConMontoCero_LanzaExcepcion()
    {
        Assert.Throws<ValidacionException>(() => CrearCheque(monto: 0m));
    }

    [Fact]
    public void ChequeFisico_Crear_ConDiferimientoAnteriorAEmision_LanzaExcepcion()
    {
        Assert.Throws<ValidacionException>(() =>
            CrearCheque(fechaDiferimiento: Hoy.AddDays(-1)));
    }

    [Fact]
    public void ChequeFisico_Crear_ConFechaEmisionFuturaLejana_LanzaExcepcion()
    {
        Assert.Throws<ValidacionException>(() => CrearCheque(fechaEmision: Hoy.AddDays(5)));
    }

    [Fact]
    public void ChequeFisico_CambiarEstado_ARechazadoSinMotivo_LanzaExcepcion()
    {
        var cheque = CrearCheque();
        cheque.CambiarEstado(EstadoInstrumento.Depositado, null);

        Assert.Throws<ValidacionException>(() =>
            cheque.CambiarEstado(EstadoInstrumento.Rechazado, null));
    }

    [Fact]
    public void ChequeFisico_CambiarEstado_AnuladoConMotivo_LanzaExcepcion()
    {
        var cheque = CrearCheque();

        Assert.Throws<ValidacionException>(() =>
            cheque.CambiarEstado(EstadoInstrumento.Anulado, MotivoRechazo.FaltaDeFondos));
    }

    [Fact]
    public void ChequeFisico_CicloCompleto_DepositadoCompensadoPagado()
    {
        var cheque = CrearCheque();

        cheque.CambiarEstado(EstadoInstrumento.Depositado, null);
        cheque.CambiarEstado(EstadoInstrumento.Compensado, null);
        cheque.CambiarEstado(EstadoInstrumento.Pagado, null);

        Assert.Equal(EstadoInstrumento.Pagado, cheque.Estado);
    }

    [Fact]
    public void ChequeFisico_CambiarEstado_TransicionInvalida_Lanza422()
    {
        var cheque = CrearCheque();
        cheque.CambiarEstado(EstadoInstrumento.Anulado, null);

        Assert.Throws<TransicionInvalidaException>(() =>
            cheque.CambiarEstado(EstadoInstrumento.Compensado, null));
    }

    [Fact]
    public void ChequeFisico_Eliminar_AplicaBajaLogica()
    {
        var cheque = CrearCheque();

        cheque.Eliminar();

        Assert.False(cheque.Activo);
        Assert.NotNull(cheque.FechaBaja);
    }

    [Fact]
    public void ChequeFisico_EliminarDosVeces_LanzaNoEncontrado()
    {
        var cheque = CrearCheque();
        cheque.Eliminar();

        Assert.Throws<NoEncontradoException>(cheque.Eliminar);
    }

    [Fact]
    public void Echeq_Crear_QuedaEnEstadoEmitidoConCeroEndosos()
    {
        var echeq = CrearEcheq();

        Assert.Equal(EstadoInstrumento.Emitido, echeq.Estado);
        Assert.Equal(0, echeq.CantidadEndosos);
        Assert.Equal("ABCDEFGHIJK", echeq.IdEcheq);
        Assert.Equal("011", echeq.DesglosarCmc7().Banco);
    }

    [Theory]
    [InlineData("ABCDEFGHIJ")]    // 10 letras
    [InlineData("ABCDEFGHIJKL")]  // 12 letras
    [InlineData("ABC12345678")]   // con dígitos
    [InlineData("abcdefghijk")]   // minúsculas
    public void Echeq_Crear_ConIdEcheqInvalido_LanzaExcepcion(string idEcheq)
    {
        Assert.Throws<ValidacionException>(() => CrearEcheq(idEcheq: idEcheq));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("06000011425000012340000123456A")]
    public void Echeq_Crear_ConCmc7Invalido_LanzaExcepcion(string cmc7)
    {
        Assert.Throws<ValidacionException>(() => CrearEcheq(cmc7: cmc7));
    }

    [Fact]
    public void Echeq_Eliminar_AplicaBajaLogica()
    {
        var echeq = CrearEcheq();

        echeq.Eliminar();

        Assert.False(echeq.Activo);
    }
}
