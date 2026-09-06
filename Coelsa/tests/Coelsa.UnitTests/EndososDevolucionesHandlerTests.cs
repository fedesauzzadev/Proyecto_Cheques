using Coelsa.Application.CasosUso;
using Coelsa.Application.Dtos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Coelsa.Domain.Validaciones;
using Xunit;

namespace Coelsa.UnitTests;

public class EndososDevolucionesHandlerTests
{
    private static readonly DateOnly Hoy = new(2026, 9, 5);
    private static string Cuit(string base10) => ValidadorCuit.Completar(base10);

    private readonly RepositorioEcheqsFake _echeqs = new();
    private readonly RepositorioEndososFake _endosos = new();
    private readonly RepositorioDevolucionesFake _devoluciones = new();
    private readonly UnitOfWorkFake _unitOfWork = new();
    private readonly GestorCacheFake _cache = new();
    private readonly AlmacenIdempotenciaFake _idempotencia = new();

    private Echeq NuevoEcheqAceptado(
        string idEcheq = "ABCDEFGHIJK",
        string cmc7 = "011000114250000123400001234567",
        string? cuitBeneficiario = null,
        DateOnly? emision = null,
        DateOnly? vencimiento = null)
    {
        var fechaEmision = emision ?? Hoy;
        var echeq = Echeq.Crear(
            idEcheq,
            cmc7,
            Cuit("2012345678"),
            Cuit(cuitBeneficiario ?? "2787654321"),
            250_000m,
            Moneda.Dolares,
            fechaEmision,
            null,
            vencimiento ?? Hoy.AddDays(30),
            Hoy);
        echeq.Aceptar();
        _echeqs.Agregar(echeq);
        return echeq;
    }

    [Fact]
    public async Task Aceptar_Pendiente_PasaAEmitido()
    {
        var echeq = Echeq.Crear(
            "ABCDEFGHIJK", "011000114250000123400001234567", Cuit("2012345678"), Cuit("2787654321"),
            250_000m, Moneda.Dolares, Hoy, null, Hoy.AddDays(30), Hoy);
        _echeqs.Agregar(echeq);
        var handler = new AceptarEcheqHandler(_echeqs, _unitOfWork, _cache);

        var respuesta = await handler.Ejecutar("ABCDEFGHIJK", new AceptarEcheqRequest { Aceptada = true }, default);

        Assert.Equal("Emitido", respuesta.Estado);
    }

    [Fact]
    public async Task Aceptar_Rechazado_PorInexistente_Lanza404()
    {
        var handler = new AceptarEcheqHandler(_echeqs, _unitOfWork, _cache);

        await Assert.ThrowsAsync<NoEncontradoException>(
            () => handler.Ejecutar("ZZZZZZZZZZZ", new AceptarEcheqRequest { Aceptada = true }, default));
    }

    [Fact]
    public async Task Repudiar_Pendiente_PasaARepudiado()
    {
        var echeq = Echeq.Crear(
            "ABCDEFGHIJK", "011000114250000123400001234567", Cuit("2012345678"), Cuit("2787654321"),
            250_000m, Moneda.Dolares, Hoy, null, Hoy.AddDays(30), Hoy);
        _echeqs.Agregar(echeq);
        var handler = new AceptarEcheqHandler(_echeqs, _unitOfWork, _cache);

        var respuesta = await handler.Ejecutar("ABCDEFGHIJK", new AceptarEcheqRequest { Aceptada = false }, default);

        Assert.Equal("Repudiado", respuesta.Estado);
    }

    [Fact]
    public async Task ProponerEndoso_Emitido_CreaPropuestoOrden1()
    {
        NuevoEcheqAceptado();
        var handler = new ProponerEndosoHandler(_echeqs, _endosos, _unitOfWork, _cache);

        var respuesta = await handler.Ejecutar(
            "ABCDEFGHIJK", new ProponerEndosoRequest { CuitEndosatario = Cuit("3051122233") }, default);

        Assert.Equal(1, respuesta.Orden);
        Assert.Equal("Propuesto", respuesta.Estado);
        Assert.Single(_endosos.Datos);
    }

    [Fact]
    public async Task ProponerEndoso_Pendiente_Lanza422()
    {
        var echeq = Echeq.Crear(
            "ABCDEFGHIJK", "011000114250000123400001234567", Cuit("2012345678"), Cuit("2787654321"),
            250_000m, Moneda.Dolares, Hoy, null, Hoy.AddDays(30), Hoy);
        _echeqs.Agregar(echeq);
        var handler = new ProponerEndosoHandler(_echeqs, _endosos, _unitOfWork, _cache);

        await Assert.ThrowsAsync<TransicionInvalidaException>(
            () => handler.Ejecutar(
                "ABCDEFGHIJK", new ProponerEndosoRequest { CuitEndosatario = Cuit("3051122233") }, default));
    }

    [Fact]
    public async Task ResolverEndoso_Admite_CambiaTenenciaYContador()
    {
        var echeq = NuevoEcheqAceptado();
        var proponer = new ProponerEndosoHandler(_echeqs, _endosos, _unitOfWork, _cache);
        await proponer.Ejecutar(
            "ABCDEFGHIJK", new ProponerEndosoRequest { CuitEndosatario = Cuit("3051122233") }, default);
        var resolver = new ResolverEndosoHandler(_echeqs, _endosos, _unitOfWork, _cache);

        var respuesta = await resolver.Ejecutar(
            "ABCDEFGHIJK", 1,
            new ResolverEndosoRequest { Admitido = true, Cuit = Cuit("3051122233") }, default);

        Assert.Equal("Vigente", respuesta.Estado);
        Assert.Equal(Cuit("3051122233"), echeq.CuitBeneficiario);
        Assert.Equal(1, echeq.CantidadEndosos);
    }

    [Fact]
    public async Task ResolverEndoso_ConCuitDistinto_Lanza400()
    {
        NuevoEcheqAceptado();
        var proponer = new ProponerEndosoHandler(_echeqs, _endosos, _unitOfWork, _cache);
        await proponer.Ejecutar(
            "ABCDEFGHIJK", new ProponerEndosoRequest { CuitEndosatario = Cuit("3051122233") }, default);
        var resolver = new ResolverEndosoHandler(_echeqs, _endosos, _unitOfWork, _cache);

        await Assert.ThrowsAsync<ValidacionException>(
            () => resolver.Ejecutar(
                "ABCDEFGHIJK", 1,
                new ResolverEndosoRequest { Admitido = true, Cuit = Cuit("2033445566") }, default));
    }

    [Fact]
    public async Task AnularEndoso_Propuesto_LoAnula()
    {
        NuevoEcheqAceptado();
        var proponer = new ProponerEndosoHandler(_echeqs, _endosos, _unitOfWork, _cache);
        await proponer.Ejecutar(
            "ABCDEFGHIJK", new ProponerEndosoRequest { CuitEndosatario = Cuit("3051122233") }, default);
        var anular = new AnularEndosoHandler(_echeqs, _endosos, _unitOfWork, _cache);

        await anular.Ejecutar("ABCDEFGHIJK", 1, default);

        Assert.Equal(EstadoEndoso.Anulado, _endosos.Datos[0].Estado);
    }

    [Fact]
    public async Task SolicitarDevolucion_TenedorActual_Lanza400()
    {
        NuevoEcheqAceptado();
        var handler = new SolicitarDevolucionHandler(_echeqs, _endosos, _devoluciones, _unitOfWork, _cache);

        await Assert.ThrowsAsync<ValidacionException>(
            () => handler.Ejecutar(
                "ABCDEFGHIJK",
                new SolicitarDevolucionRequest { CuitSolicitante = Cuit("2787654321") }, default));
    }

    [Fact]
    public async Task SolicitarDevolucion_FueraDeCadena_Lanza400()
    {
        NuevoEcheqAceptado();
        var handler = new SolicitarDevolucionHandler(_echeqs, _endosos, _devoluciones, _unitOfWork, _cache);

        await Assert.ThrowsAsync<ValidacionException>(
            () => handler.Ejecutar(
                "ABCDEFGHIJK",
                new SolicitarDevolucionRequest { CuitSolicitante = Cuit("2033445566") }, default));
    }

    [Fact]
    public async Task ResolverDevolucion_Acepta_RevierteCadenaYTenencia()
    {
        var echeq = NuevoEcheqAceptado();
        var proponer = new ProponerEndosoHandler(_echeqs, _endosos, _unitOfWork, _cache);
        await proponer.Ejecutar(
            "ABCDEFGHIJK", new ProponerEndosoRequest { CuitEndosatario = Cuit("3051122233") }, default);
        var resolverEndoso = new ResolverEndosoHandler(_echeqs, _endosos, _unitOfWork, _cache);
        await resolverEndoso.Ejecutar(
            "ABCDEFGHIJK", 1,
            new ResolverEndosoRequest { Admitido = true, Cuit = Cuit("3051122233") }, default);
        var solicitar = new SolicitarDevolucionHandler(_echeqs, _endosos, _devoluciones, _unitOfWork, _cache);
        await solicitar.Ejecutar(
            "ABCDEFGHIJK",
            new SolicitarDevolucionRequest { CuitSolicitante = Cuit("2012345678") }, default);
        var resolver = new ResolverDevolucionHandler(_echeqs, _endosos, _devoluciones, _unitOfWork, _cache);

        var respuesta = await resolver.Ejecutar(
            "ABCDEFGHIJK", 1,
            new ResolverDevolucionRequest { Aceptada = true, CuitResolutor = Cuit("3051122233") }, default);

        Assert.Equal("Aceptada", respuesta.Estado);
        Assert.Equal(Cuit("2012345678"), echeq.CuitBeneficiario);
        Assert.Equal(EstadoEndoso.Revertido, _endosos.Datos[0].Estado);
        Assert.Equal(0, echeq.CantidadEndosos);
    }

    [Fact]
    public async Task ResolverDevolucion_ResolutorDistinto_Lanza400()
    {
        NuevoEcheqAceptado();
        var solicitar = new SolicitarDevolucionHandler(_echeqs, _endosos, _devoluciones, _unitOfWork, _cache);
        await solicitar.Ejecutar(
            "ABCDEFGHIJK",
            new SolicitarDevolucionRequest { CuitSolicitante = Cuit("2012345678") }, default);
        var resolver = new ResolverDevolucionHandler(_echeqs, _endosos, _devoluciones, _unitOfWork, _cache);

        await Assert.ThrowsAsync<ValidacionException>(
            () => resolver.Ejecutar(
                "ABCDEFGHIJK", 1,
                new ResolverDevolucionRequest { Aceptada = true, CuitResolutor = Cuit("2012345678") }, default));
    }

    [Fact]
    public async Task DepositarCustodiasVencidas_SoloVencidas()
    {
        var vencida = NuevoEcheqAceptado(
            "AAAAAAAAAAA", "060000114250000123400001234567", null, Hoy.AddDays(-60), Hoy.AddDays(-1));
        vencida.PonerEnCustodia();
        var noVencida = NuevoEcheqAceptado(
            "BBBBBBBBBBB", "060000114250000123400001234568");
        noVencida.PonerEnCustodia();
        var handler = new DepositarCustodiasVencidasHandler(_echeqs, _unitOfWork, _cache);

        var cantidad = await handler.Ejecutar(Hoy, default);

        Assert.Equal(1, cantidad);
        Assert.Equal(EstadoInstrumento.Depositado, vencida.Estado);
        Assert.Equal(EstadoInstrumento.EnCustodia, noVencida.Estado);
    }
}
