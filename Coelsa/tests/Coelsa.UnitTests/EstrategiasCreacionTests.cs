using Coelsa.Application.Dtos;
using Coelsa.Application.Estrategias;
using Coelsa.Domain;
using Coelsa.Domain.Validaciones;
using Xunit;

namespace Coelsa.UnitTests;

public class EstrategiasCreacionTests
{
    private readonly AlmacenIdempotenciaFake _idempotencia = new();
    private readonly GestorCacheFake _cache = new();
    private readonly RepositorioChequesFake _repositorio = new();
    private readonly UnitOfWorkFake _unitOfWork = new();

    private ChequeFisicoCreationStrategy CrearStrategy()
        => new(_repositorio, _unitOfWork, _idempotencia, _cache);

    private static CrearChequeFisicoRequest RequestValido(string cmc7 = "060000114250000123400001234567") => new()
    {
        Cmc7 = cmc7,
        CuitLibrador = ValidadorCuit.Completar("2012345678"),
        CuitBeneficiario = ValidadorCuit.Completar("2787654321"),
        Monto = 1_500_000.50m,
        Moneda = "P",
        FechaEmision = new DateOnly(2026, 9, 5),
        FechaDiferimiento = new DateOnly(2026, 10, 5),
        FechaVencimiento = new DateOnly(2026, 11, 5)
    };

    [Fact]
    public async Task Crear_PrimeraVez_PersisteEInvalidaCacheDeAmbosCuits()
    {
        var resultado = await CrearStrategy().CrearAsync(
            RequestValido(), "11111111-1111-1111-1111-111111111111", default);

        Assert.False(resultado.EsReplay);
        Assert.Single(_repositorio.Datos);
        Assert.Equal(2, _cache.Invalidaciones.Count); // librador + beneficiario
        Assert.Equal(1, _unitOfWork.Guardadas);
    }

    [Fact]
    public async Task Crear_MismaKeyYMismoBody_EsReplaySinDuplicar()
    {
        var estrategia = CrearStrategy();
        var key = "22222222-2222-2222-2222-222222222222";
        var request = RequestValido("011000114250000123400001234567");

        var primera = await estrategia.CrearAsync(request, key, default);
        var segunda = await estrategia.CrearAsync(request, key, default);

        Assert.False(primera.EsReplay);
        Assert.True(segunda.EsReplay);
        Assert.Equal(primera.Respuesta.Identificador, segunda.Respuesta.Identificador);
        Assert.Single(_repositorio.Datos);
    }

    [Fact]
    public async Task Crear_MismaKeyConBodyDistinto_LanzaConflicto409()
    {
        var estrategia = CrearStrategy();
        var key = "33333333-3333-3333-3333-333333333333";

        await estrategia.CrearAsync(RequestValido(), key, default);

        var otroBody = RequestValido();
        otroBody.Monto = 999m;

        var conflicto = await Assert.ThrowsAsync<ConflictoDominioException>(
            () => estrategia.CrearAsync(otroBody, key, default));

        Assert.Contains("Idempotency-Key", conflicto.Message);
    }

    [Fact]
    public async Task Crear_Cmc7DuplicadoConKeyDistinta_LanzaConflicto409()
    {
        var estrategia = CrearStrategy();
        const string cmc7 = "034000114250000123400001234567";

        await estrategia.CrearAsync(RequestValido(cmc7), "44444444-4444-4444-4444-444444444444", default);

        var conflicto = await Assert.ThrowsAsync<ConflictoDominioException>(
            () => estrategia.CrearAsync(RequestValido(cmc7), "55555555-5555-5555-5555-555555555555", default));

        Assert.Contains("CMC7", conflicto.Message);
    }

    [Fact]
    public async Task Crear_ConDatosInvalidos_LanzaValidacion400()
    {
        var estrategia = CrearStrategy();
        var request = RequestValido();
        request.CuitLibrador = "2012345678"; // sin dígito verificador

        await Assert.ThrowsAsync<ValidacionException>(
            () => estrategia.CrearAsync(request, "66666666-6666-6666-6666-666666666666", default));
    }
}

public class EcheqCreationStrategyTests
{
    private readonly AlmacenIdempotenciaFake _idempotencia = new();
    private readonly GestorCacheFake _cache = new();
    private readonly RepositorioEcheqsFake _repositorio = new();
    private readonly RepositorioCuentasFake _cuentas = new();
    private readonly RepositorioChequerasFake _chequeras = new();
    private readonly UnitOfWorkFake _unitOfWork = new();

    private readonly string _cbu = FabricaEcheqs.Cbu;
    private readonly string _cuitLibrador;
    private readonly string _cuitBeneficiario;

    public EcheqCreationStrategyTests()
    {
        _cuitLibrador = ValidadorCuit.Completar("2012345678");
        _cuitBeneficiario = ValidadorCuit.Completar("2787654321");
        var cuenta = Domain.Entidades.Cuenta.Crear(_cbu, _cuitLibrador, "Librador Demo S.A.", Domain.Moneda.Dolares);
        _cuentas.Agregar(cuenta);
        _chequeras.Agregar(Domain.Entidades.Chequera.Solicitar(cuenta.Id, 1));
    }

    private EcheqCreationStrategy CrearStrategy()
        => new(_repositorio, _cuentas, _chequeras, _unitOfWork, _idempotencia, _cache, new GeneradorIdEcheqFijo());

    private CrearEcheqRequest RequestValido() => new()
    {
        CbuEmisor = _cbu,
        Caracter = "AlaOrden",
        TipoDocBeneficiario = "CUIT",
        NombreLibrador = "Librador Demo S.A.",
        NombreBeneficiario = "Beneficiario Demo S.A.",
        CuitLibrador = _cuitLibrador,
        CuitBeneficiario = _cuitBeneficiario,
        Monto = 250_000m,
        Moneda = "D",
        FechaEmision = new DateOnly(2026, 9, 5),
        FechaVencimiento = new DateOnly(2026, 10, 5)
    };

    [Fact]
    public async Task Crear_GeneraIdEcheqDe11LetrasYCmc7DerivadoDelCbu()
    {
        var resultado = await CrearStrategy().CrearAsync(
            RequestValido(), "77777777-7777-7777-7777-777777777777", default);

        Assert.False(resultado.EsReplay);
        Assert.Matches("^[A-Z]{11}$", resultado.Respuesta.Identificador);
        Assert.Equal(_cbu, resultado.Respuesta.CbuEmisor);
        Assert.Equal(1, resultado.Respuesta.NumeroChequera);
        Assert.Equal(1, resultado.Respuesta.NumeroCheque);
        Assert.Equal("CUIT", resultado.Respuesta.TipoDocBeneficiario);
        Assert.Equal("Librador Demo S.A.", resultado.Respuesta.NombreLibrador);
        Assert.Equal("Beneficiario Demo S.A.", resultado.Respuesta.NombreBeneficiario);
        Assert.Equal(
            Domain.ValueObjects.Cmc7.Derivar(_cbu, 1).Valor,
            resultado.Respuesta.Cmc7);
        Assert.Single(_repositorio.Datos);
    }

    [Fact]
    public async Task Crear_ConsumeNumerosSecuencialesSinRepetirCmc7()
    {
        var estrategia = CrearStrategy();

        var primero = await estrategia.CrearAsync(RequestValido(), "88888888-8888-8888-8888-888888888888", default);
        var segundo = await estrategia.CrearAsync(RequestValido(), "99999999-9999-9999-9999-999999999999", default);

        Assert.Equal(1, primero.Respuesta.NumeroCheque);
        Assert.Equal(2, segundo.Respuesta.NumeroCheque);
        Assert.NotEqual(primero.Respuesta.Cmc7, segundo.Respuesta.Cmc7);
        Assert.NotEqual(primero.Respuesta.Identificador, segundo.Respuesta.Identificador);
    }

    [Fact]
    public async Task Crear_LibradorDistintoDelTitular_LanzaValidacion400()
    {
        var request = RequestValido();
        request.CuitLibrador = ValidadorCuit.Completar("3051122233");

        await Assert.ThrowsAsync<ValidacionException>(
            () => CrearStrategy().CrearAsync(request, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", default));
    }

    [Fact]
    public async Task Crear_MonedaDistintaDeLaCuenta_LanzaValidacion400()
    {
        var request = RequestValido();
        request.Moneda = "P"; // la cuenta demo es en dólares

        await Assert.ThrowsAsync<ValidacionException>(
            () => CrearStrategy().CrearAsync(request, "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", default));
    }

    [Fact]
    public async Task Crear_CuentaInexistente_LanzaValidacion400()
    {
        var request = RequestValido();
        request.CbuEmisor = ValidadorCbu.Crear("011", "9999", "0000000000001");

        await Assert.ThrowsAsync<ValidacionException>(
            () => CrearStrategy().CrearAsync(request, "cccccccc-cccc-cccc-cccc-cccccccccccc", default));
    }

    [Fact]
    public async Task Crear_GuardaCaracterYModoCruzado()
    {
        var request = RequestValido();
        request.Caracter = "NoAlaOrden";

        var resultado = await CrearStrategy().CrearAsync(
            request, "dddddddd-dddd-dddd-dddd-dddddddddddd", default);

        Assert.Equal("NoAlaOrden", resultado.Respuesta.Caracter);
        Assert.Equal("Cruzado", resultado.Respuesta.Modo);
    }

    [Fact]
    public async Task Crear_GuardaGestionOpcional()
    {
        var request = RequestValido();
        request.Concepto = "Pago a proveedores";
        request.Referencia = "FAC-123";
        request.EmailNotificacion = "cobros@demo.local";

        var resultado = await CrearStrategy().CrearAsync(
            request, "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee", default);

        Assert.Equal("Pago a proveedores", resultado.Respuesta.Concepto);
        Assert.Equal("FAC-123", resultado.Respuesta.Referencia);
        Assert.Equal("cobros@demo.local", resultado.Respuesta.EmailNotificacion);
        Assert.Null(resultado.Respuesta.Motivo);
    }

    [Fact]
    public async Task Crear_SinChequeraConLugar_LanzaConflicto409()
    {
        // Se agota la única chequera (50 números) con keys distintas.
        var estrategia = CrearStrategy();
        for (var i = 0; i < Domain.Entidades.Chequera.CantidadNumeros; i++)
        {
            await estrategia.CrearAsync(RequestValido(), Guid.NewGuid().ToString(), default);
        }

        await Assert.ThrowsAsync<ConflictoDominioException>(
            () => estrategia.CrearAsync(RequestValido(), Guid.NewGuid().ToString(), default));
    }
}
