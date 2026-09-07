using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Coelsa.Domain.Validaciones;
using Coelsa.Domain.ValueObjects;
using Xunit;

namespace Coelsa.UnitTests;

public class EntidadesTests
{
    private static readonly DateOnly Hoy = new(2026, 9, 5);
    private const string CuitLibrador = "2012345678";
    private const string CuitBeneficiario = "2787654321";
    private static string CuitValido(string base10) => ValidadorCuit.Completar(base10);

    private static ChequeFisico CrearCheque(
        string? cmc7 = "060000114250000123400001234567",
        string? cuitLibrador = CuitLibrador,
        string? cuitBeneficiario = CuitBeneficiario,
        decimal? monto = 100_000m,
        DateOnly? fechaEmision = null,
        DateOnly? fechaDiferimiento = null,
        DateOnly? fechaVencimiento = null)
        => ChequeFisico.Crear(
            cmc7!,
            CuitValido(cuitLibrador ?? CuitLibrador),
            CuitValido(cuitBeneficiario ?? CuitBeneficiario),
            monto!.Value,
            Moneda.Pesos,
            fechaEmision ?? Hoy,
            fechaDiferimiento,
            fechaVencimiento ?? Hoy.AddDays(30),
            Hoy);

    private static Echeq CrearEcheq(
        string? idEcheq = "ABCDEFGHIJK",
        string? cmc7 = null,
        int numeroCheque = 1,
        Caracter caracter = Caracter.AlaOrden,
        string? cuitLibrador = CuitLibrador,
        string? cuitBeneficiario = CuitBeneficiario,
        decimal? monto = 250_000m,
        DateOnly? fechaVencimiento = null)
    {
        var cbu = FabricaEcheqs.Cbu;
        return Echeq.Crear(
            idEcheq!,
            cbu,
            1,
            numeroCheque,
            caracter,
            TipoDocumento.Cuit,
            "Librador Demo S.A.",
            "Beneficiario Demo S.A.",
            cmc7 ?? Cmc7.Derivar(cbu, numeroCheque).Valor,
            CuitValido(cuitLibrador ?? CuitLibrador),
            CuitValido(cuitBeneficiario ?? CuitBeneficiario),
            monto!.Value,
            Moneda.Dolares,
            Hoy,
            null,
            fechaVencimiento ?? Hoy.AddDays(30),
            Hoy);
    }

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
            Hoy.AddDays(30),
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
    public void ChequeFisico_Crear_ConVencimientoNoPosteriorAEmision_LanzaExcepcion()
    {
        Assert.Throws<ValidacionException>(() => CrearCheque(fechaVencimiento: Hoy));
    }

    [Fact]
    public void ChequeFisico_Crear_ConVencimientoNoPosteriorADiferimiento_LanzaExcepcion()
    {
        Assert.Throws<ValidacionException>(() => CrearCheque(
            fechaDiferimiento: Hoy.AddDays(10),
            fechaVencimiento: Hoy.AddDays(10)));
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
    public void Echeq_Crear_QuedaPendienteConCeroEndosos()
    {
        var echeq = CrearEcheq();

        Assert.Equal(EstadoInstrumento.Pendiente, echeq.Estado);
        Assert.Equal(0, echeq.CantidadEndosos);
        Assert.Equal("ABCDEFGHIJK", echeq.IdEcheq);
        Assert.Equal("011", echeq.DesglosarCmc7().Banco);
    }

    [Fact]
    public void Echeq_Aceptar_PasaAEmitido()
    {
        var echeq = CrearEcheq();

        echeq.Aceptar();

        Assert.Equal(EstadoInstrumento.Emitido, echeq.Estado);
    }

    [Fact]
    public void Echeq_Repudiar_PasaARepudiadoTerminal()
    {
        var echeq = CrearEcheq();

        echeq.Repudiar("No reconozco la deuda");

        Assert.Equal(EstadoInstrumento.Repudiado, echeq.Estado);
        Assert.Equal("No reconozco la deuda", echeq.MotivoRepudio);
        Assert.Empty(TransicionesEstado.DestinosDesde(EstadoInstrumento.Repudiado));
    }

    [Fact]
    public void Echeq_Repudiar_SinMotivo_LanzaValidacion()
    {
        var echeq = CrearEcheq();

        Assert.Throws<ValidacionException>(() => echeq.Repudiar(null));
        Assert.Throws<ValidacionException>(() => echeq.Repudiar("  "));
    }

    [Fact]
    public void Echeq_Custodia_Rescate_VuelvenAEmitido()
    {
        var echeq = CrearEcheq();
        echeq.Aceptar();

        echeq.PonerEnCustodia();
        Assert.Equal(EstadoInstrumento.EnCustodia, echeq.Estado);

        echeq.Rescatar();
        Assert.Equal(EstadoInstrumento.Emitido, echeq.Estado);
    }

    [Fact]
    public void Echeq_DepositarPorVencimiento_SoloSiVencio()
    {
        var echeq = CrearEcheq();
        echeq.Aceptar();
        echeq.PonerEnCustodia();

        Assert.Throws<TransicionInvalidaException>(() => echeq.DepositarPorVencimiento(Hoy));

        echeq.DepositarPorVencimiento(Hoy.AddDays(31));
        Assert.Equal(EstadoInstrumento.Depositado, echeq.Estado);
    }

    [Fact]
    public void Echeq_CambiarTenencia_ValidaCuit()
    {
        var echeq = CrearEcheq();
        echeq.Aceptar();

        echeq.CambiarTenencia(CuitValido("3051122233"));

        Assert.Equal(CuitValido("3051122233"), echeq.CuitBeneficiario);
        Assert.Throws<ValidacionException>(() => echeq.CambiarTenencia("123"));
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
    public void Echeq_Crear_ConVencimientoNoPosteriorAEmision_LanzaExcepcion()
    {
        Assert.Throws<ValidacionException>(() => CrearEcheq(fechaVencimiento: Hoy));
    }

    [Fact]
    public void Echeq_Crear_ConTenorMayorA360Dias_LanzaExcepcion()
    {
        Assert.Throws<ValidacionException>(() => CrearEcheq(fechaVencimiento: Hoy.AddDays(361)));
        Assert.Throws<ValidacionException>(() => CrearCheque(fechaVencimiento: Hoy.AddDays(361)));
    }

    [Fact]
    public void Echeq_Crear_ConGestionNormalizaYValida()
    {
        var echeq = Echeq.Crear(
            "ABCDEFGHIJK",
            FabricaEcheqs.Cbu,
            1,
            1,
            Caracter.AlaOrden,
            TipoDocumento.Cuit,
            "Librador Demo S.A.",
            "Beneficiario Demo S.A.",
            Cmc7.Derivar(FabricaEcheqs.Cbu, 1).Valor,
            CuitValido(CuitLibrador),
            CuitValido(CuitBeneficiario),
            250_000m,
            Moneda.Pesos,
            Hoy,
            null,
            Hoy.AddDays(30),
            Hoy,
            concepto: "  Pago a proveedores  ",
            motivo: null,
            referencia: "",
            emailNotificacion: "cobros@demo.local");

        Assert.Equal("Pago a proveedores", echeq.Concepto);
        Assert.Null(echeq.Motivo);
        Assert.Null(echeq.Referencia);
        Assert.Equal("cobros@demo.local", echeq.EmailNotificacion);
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("a@")]
    [InlineData("a@b")]
    public void Echeq_Crear_ConEmailInvalido_LanzaExcepcion(string email)
    {
        Assert.Throws<ValidacionException>(() => CrearEcheqConGestion(emailNotificacion: email));
    }

    [Fact]
    public void Echeq_Crear_ConConceptoMuyLargo_LanzaExcepcion()
    {
        Assert.Throws<ValidacionException>(() => CrearEcheqConGestion(concepto: new string('x', 61)));
    }

    private static Echeq CrearEcheqConGestion(string? concepto = null, string? emailNotificacion = null)
    {
        var cbu = FabricaEcheqs.Cbu;
        return Echeq.Crear(
            "ABCDEFGHIJK",
            cbu,
            1,
            1,
            Caracter.AlaOrden,
            TipoDocumento.Cuit,
            "Librador Demo S.A.",
            "Beneficiario Demo S.A.",
            Cmc7.Derivar(cbu, 1).Valor,
            CuitValido(CuitLibrador),
            CuitValido(CuitBeneficiario),
            250_000m,
            Moneda.Pesos,
            Hoy,
            null,
            Hoy.AddDays(30),
            Hoy,
            concepto: concepto,
            emailNotificacion: emailNotificacion);
    }

    [Fact]
    public void Echeq_Eliminar_AplicaBajaLogica()
    {
        var echeq = CrearEcheq();

        echeq.Eliminar();

        Assert.False(echeq.Activo);
    }
}
