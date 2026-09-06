using System.Threading;
using IrmaRios.Cartera.Application.CasosUso;
using IrmaRios.Cartera.Application.Dtos;
using IrmaRios.Cartera.Application.Puertos;
using IrmaRios.Cartera.Domain;
using IrmaRios.Cartera.Domain.Validaciones;
using Microsoft.Extensions.Logging.Abstractions;

namespace IrmaRios.Cartera.UnitTests;

public class DepositarHandlerTests
{
    private static readonly string Cuit = ValidadorCuit.Completar("2033445566");

    private readonly CarteraRepositoryFake _cartera = new();
    private readonly ClearingFake _clearing = new();
    private readonly EmpresasFake _empresas = new();
    private readonly GestorCacheFake _cache = new();
    private readonly AlmacenIdempotenciaFake _idempotencia = new();
    private readonly UnitOfWorkFake _uow = new();

    public DepositarHandlerTests()
    {
        _empresas.CuitsExistentes.Add(Cuit);
    }

    private DepositarHandler Handler() => new(
        _empresas, _clearing, _cartera, _idempotencia, _uow, _cache,
        NullLogger<DepositarHandler>.Instance);

    private static InstrumentoClearing EnClearing(
        string identificador,
        EstadoClearing estado = EstadoClearing.Emitido,
        string? beneficiario = null)
        => new(
            identificador,
            ValidadorCuit.Completar("2012345678"),
            beneficiario ?? Cuit,
            150_000m,
            Moneda.Pesos,
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
            estado);

    private const string Cmc7 = "060000114250000001234567890123";

    [Fact]
    public async Task Deposito_validado_ingresa_a_cartera()
    {
        _clearing.Datos[Cmc7] = EnClearing(Cmc7);

        var resultado = await Handler().Ejecutar(
            new DepositoRequest(Cuit, [new InstrumentoDepositadoRequest("ChequeFisico", Cmc7)]),
            "key-1", CancellationToken.None);

        Assert.False(resultado.EsReplay);
        Assert.Single(resultado.Respuesta.Aceptados);
        Assert.Empty(resultado.Respuesta.Rechazados);
        Assert.Single(_cartera.Instrumentos);
    }

    [Fact]
    public async Task Deposito_con_empresa_inexistente_da_404()
    {
        _clearing.Datos[Cmc7] = EnClearing(Cmc7);

        await Assert.ThrowsAsync<NoEncontradoException>(() => Handler().Ejecutar(
            new DepositoRequest(ValidadorCuit.Completar("2787654321"),
                [new InstrumentoDepositadoRequest("ChequeFisico", Cmc7)]),
            "key-1", CancellationToken.None));
    }

    [Fact]
    public async Task Instrumento_inexistente_en_clearing_se_reporta_rechazado()
    {
        // Toda la tanda rechazada => 400 con motivos.
        var ex = await Assert.ThrowsAsync<ValidacionException>(() => Handler().Ejecutar(
            new DepositoRequest(Cuit, [new InstrumentoDepositadoRequest("ChequeFisico", Cmc7)]),
            "key-1", CancellationToken.None));

        Assert.Contains("Ningún instrumento", ex.Message);
        Assert.Contains("No existe en el clearing", ex.Message);
    }

    [Fact]
    public async Task Aceptacion_parcial_rechazado_con_motivo_y_aceptado_con_valido()
    {
        var buenCmc7 = "060000114250000009995678901234";
        _clearing.Datos[Cmc7] = EnClearing(Cmc7, EstadoClearing.Rechazado);
        _clearing.Datos[buenCmc7] = EnClearing(buenCmc7);

        var resultado = await Handler().Ejecutar(
            new DepositoRequest(Cuit,
            [
                new InstrumentoDepositadoRequest("ChequeFisico", Cmc7),
                new InstrumentoDepositadoRequest("ChequeFisico", buenCmc7)
            ]),
            "key-1", CancellationToken.None);

        Assert.Single(resultado.Respuesta.Aceptados);
        var rechazado = Assert.Single(resultado.Respuesta.Rechazados);
        Assert.Contains("no es negociable", rechazado.Motivo);
    }

    [Fact]
    public async Task Reenvio_con_misma_key_y_body_es_replay()
    {
        _clearing.Datos[Cmc7] = EnClearing(Cmc7);
        var request = new DepositoRequest(Cuit, [new InstrumentoDepositadoRequest("ChequeFisico", Cmc7)]);

        await Handler().Ejecutar(request, "key-1", CancellationToken.None);
        var resultado = await Handler().Ejecutar(request, "key-1", CancellationToken.None);

        Assert.True(resultado.EsReplay);
        Assert.Single(_cartera.Instrumentos);
    }

    [Fact]
    public async Task Depositar_invalida_cache_de_la_empresa()
    {
        _clearing.Datos[Cmc7] = EnClearing(Cmc7);

        await Handler().Ejecutar(
            new DepositoRequest(Cuit, [new InstrumentoDepositadoRequest("ChequeFisico", Cmc7)]),
            "key-1", CancellationToken.None);

        Assert.Contains(_cache.Invalidaciones, c => c.Contains(Cuit));
    }

    [Fact]
    public async Task Instrumento_ya_en_cartera_se_reacepta_sin_duplicar()
    {
        _clearing.Datos[Cmc7] = EnClearing(Cmc7);
        await Handler().Ejecutar(
            new DepositoRequest(Cuit, [new InstrumentoDepositadoRequest("ChequeFisico", Cmc7)]),
            "key-1", CancellationToken.None);

        var resultado = await Handler().Ejecutar(
            new DepositoRequest(Cuit, [new InstrumentoDepositadoRequest("ChequeFisico", Cmc7)]),
            "key-2", CancellationToken.None);

        Assert.Single(resultado.Respuesta.Aceptados);
        Assert.Single(_cartera.Instrumentos);
        Assert.Equal(2, _cartera.Depositos.Count);
    }
}
