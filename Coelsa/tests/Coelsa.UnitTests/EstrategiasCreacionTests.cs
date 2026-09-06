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
    private readonly UnitOfWorkFake _unitOfWork = new();

    private EcheqCreationStrategy CrearStrategy()
        => new(_repositorio, _unitOfWork, _idempotencia, _cache, new GeneradorIdEcheqFijo());

    private static string Cmc7Valido(int i) =>
        $"011{i:D4}1425{(100000 + i):D8}000098765{i:D2}";

    private static CrearEcheqRequest RequestValido(string? cmc7 = null) => new()
    {
        Cmc7 = cmc7 ?? Cmc7Valido(1),
        CuitLibrador = ValidadorCuit.Completar("2012345678"),
        CuitBeneficiario = ValidadorCuit.Completar("2787654321"),
        Monto = 250_000m,
        Moneda = "D",
        FechaEmision = new DateOnly(2026, 9, 5),
        FechaVencimiento = new DateOnly(2026, 10, 5)
    };

    [Fact]
    public async Task Crear_GeneraIdEcheqDe11Letras()
    {
        var resultado = await CrearStrategy().CrearAsync(
            RequestValido(), "77777777-7777-7777-7777-777777777777", default);

        Assert.False(resultado.EsReplay);
        Assert.Matches("^[A-Z]{11}$", resultado.Respuesta.Identificador);
        Assert.Single(_repositorio.Datos);
    }

    [Fact]
    public async Task Crear_Cmc7Duplicado_LanzaConflicto409()
    {
        const int numero = 7;

        await CrearStrategy().CrearAsync(RequestValido(Cmc7Valido(numero)), "88888888-8888-8888-8888-888888888888", default);

        await Assert.ThrowsAsync<ConflictoDominioException>(
            () => CrearStrategy().CrearAsync(RequestValido(Cmc7Valido(numero)), "99999999-9999-9999-9999-999999999999", default));
    }
}
