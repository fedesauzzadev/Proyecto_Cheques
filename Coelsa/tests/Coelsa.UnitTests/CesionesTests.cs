using Coelsa.Application.CasosUso;
using Coelsa.Application.Dtos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Coelsa.Domain.Validaciones;
using Xunit;

namespace Coelsa.UnitTests;

public class CesionesTests
{
    private static string Cuit(string base10) => ValidadorCuit.Completar(base10);

    private readonly RepositorioEcheqsFake _echeqs = new();
    private readonly RepositorioCesionesFake _cesiones = new();
    private readonly UnitOfWorkFake _unitOfWork = new();
    private readonly GestorCacheFake _cache = new();

    private Echeq NuevoEcheqNoAlaOrden(string idEcheq = "ABCDEFGHIJK", int numeroCheque = 1)
    {
        var echeq = FabricaEcheqs.Crear(idEcheq, numeroCheque: numeroCheque, caracter: Caracter.NoAlaOrden);
        echeq.Aceptar();
        _echeqs.Agregar(echeq);
        return echeq;
    }

    private static SolicitarCesionRequest Pedido(string? cesionario = null, string domicilio = "Calle 123")
        => new() { CuitCesionario = cesionario ?? Cuit("3051122233"), DomicilioCesionario = domicilio };

    [Fact]
    public async Task Solicitar_NoAlaOrdenEmitido_CreaSolicitadaOrden1()
    {
        var handler = new SolicitarCesionHandler(_echeqs, _cesiones, _unitOfWork, _cache);
        NuevoEcheqNoAlaOrden();

        var respuesta = await handler.Ejecutar("ABCDEFGHIJK", Pedido(), default);

        Assert.Equal(1, respuesta.Numero);
        Assert.Equal("Solicitada", respuesta.Estado);
        Assert.Equal("Calle 123", respuesta.DomicilioCesionario);
        Assert.Single(_cesiones.Datos);
    }

    [Fact]
    public async Task Solicitar_AlaOrden_Lanza422()
    {
        var echeq = FabricaEcheqs.Crear();
        echeq.Aceptar();
        _echeqs.Agregar(echeq);
        var handler = new SolicitarCesionHandler(_echeqs, _cesiones, _unitOfWork, _cache);

        await Assert.ThrowsAsync<TransicionInvalidaException>(
            () => handler.Ejecutar("ABCDEFGHIJK", Pedido(), default));
    }

    [Fact]
    public async Task Solicitar_NoEmitido_Lanza422()
    {
        _echeqs.Agregar(FabricaEcheqs.Crear(caracter: Caracter.NoAlaOrden));
        var handler = new SolicitarCesionHandler(_echeqs, _cesiones, _unitOfWork, _cache);

        await Assert.ThrowsAsync<TransicionInvalidaException>(
            () => handler.Ejecutar("ABCDEFGHIJK", Pedido(), default));
    }

    [Fact]
    public async Task Solicitar_ConSolicitadaPendiente_LanzaConflicto409()
    {
        var handler = new SolicitarCesionHandler(_echeqs, _cesiones, _unitOfWork, _cache);
        NuevoEcheqNoAlaOrden();

        await handler.Ejecutar("ABCDEFGHIJK", Pedido(), default);

        await Assert.ThrowsAsync<ConflictoDominioException>(
            () => handler.Ejecutar("ABCDEFGHIJK", Pedido(Cuit("2033445566")), default));
    }

    [Fact]
    public async Task Resolver_Acepta_CambiaTenenciaAlCesionario()
    {
        var solicitar = new SolicitarCesionHandler(_echeqs, _cesiones, _unitOfWork, _cache);
        var resolver = new ResolverCesionHandler(_echeqs, _cesiones, _unitOfWork, _cache);
        var echeq = NuevoEcheqNoAlaOrden();
        await solicitar.Ejecutar("ABCDEFGHIJK", Pedido(), default);

        var respuesta = await resolver.Ejecutar(
            "ABCDEFGHIJK", 1,
            new ResolverCesionRequest { Aceptada = true, CuitResolutor = Cuit("3051122233") }, default);

        Assert.Equal("Aceptada", respuesta.Estado);
        Assert.Equal(Cuit("3051122233"), echeq.CuitBeneficiario);
    }

    [Fact]
    public async Task Resolver_ConCuitDistintoDelCesionario_LanzaValidacion()
    {
        var solicitar = new SolicitarCesionHandler(_echeqs, _cesiones, _unitOfWork, _cache);
        var resolver = new ResolverCesionHandler(_echeqs, _cesiones, _unitOfWork, _cache);
        NuevoEcheqNoAlaOrden();
        await solicitar.Ejecutar("ABCDEFGHIJK", Pedido(), default);

        await Assert.ThrowsAsync<ValidacionException>(
            () => resolver.Ejecutar(
                "ABCDEFGHIJK", 1,
                new ResolverCesionRequest { Aceptada = true, CuitResolutor = Cuit("2012345678") }, default));
    }

    [Fact]
    public async Task Resolver_Rechaza_DejaTenencia()
    {
        var solicitar = new SolicitarCesionHandler(_echeqs, _cesiones, _unitOfWork, _cache);
        var resolver = new ResolverCesionHandler(_echeqs, _cesiones, _unitOfWork, _cache);
        var echeq = NuevoEcheqNoAlaOrden();
        var tenedor = echeq.CuitBeneficiario;
        await solicitar.Ejecutar("ABCDEFGHIJK", Pedido(), default);

        var respuesta = await resolver.Ejecutar(
            "ABCDEFGHIJK", 1,
            new ResolverCesionRequest { Aceptada = false, CuitResolutor = Cuit("3051122233") }, default);

        Assert.Equal("Rechazada", respuesta.Estado);
        Assert.Equal(tenedor, echeq.CuitBeneficiario);
    }

    [Fact]
    public async Task Anular_Solicitada_PasaAAnulada()
    {
        var solicitar = new SolicitarCesionHandler(_echeqs, _cesiones, _unitOfWork, _cache);
        var anular = new AnularCesionHandler(_echeqs, _cesiones, _unitOfWork, _cache);
        NuevoEcheqNoAlaOrden();
        await solicitar.Ejecutar("ABCDEFGHIJK", Pedido(), default);

        await anular.Ejecutar("ABCDEFGHIJK", 1, default);

        Assert.Equal(EstadoCesion.Anulada, _cesiones.Datos[0].Estado);
    }

    [Fact]
    public async Task Listar_DevuelveCadenaOrdenada()
    {
        var solicitar = new SolicitarCesionHandler(_echeqs, _cesiones, _unitOfWork, _cache);
        var listar = new ListarCesionesHandler(_echeqs, _cesiones);
        NuevoEcheqNoAlaOrden();
        await solicitar.Ejecutar("ABCDEFGHIJK", Pedido(), default);
        await new AnularCesionHandler(_echeqs, _cesiones, _unitOfWork, _cache).Ejecutar("ABCDEFGHIJK", 1, default);
        await solicitar.Ejecutar("ABCDEFGHIJK", Pedido(Cuit("2033445566")), default);

        var cadena = await listar.Ejecutar("ABCDEFGHIJK", default);

        Assert.Equal([1, 2], cadena.Select(c => c.Numero));
    }

    [Fact]
    public void Solicitar_ConDomicilioVacio_LanzaValidacion()
    {
        Assert.Throws<ValidacionException>(
            () => Cesion.Solicitar(Guid.NewGuid(), 1, Cuit("2012345678"), Cuit("3051122233"), "  "));
    }

    [Fact]
    public void Solicitar_ConMismosCuits_LanzaValidacion()
    {
        Assert.Throws<ValidacionException>(
            () => Cesion.Solicitar(Guid.NewGuid(), 1, Cuit("2012345678"), Cuit("2012345678"), "Calle 123"));
    }
}
