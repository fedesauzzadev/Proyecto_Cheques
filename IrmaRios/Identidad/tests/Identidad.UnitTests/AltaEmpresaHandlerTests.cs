using System.Threading;
using IrmaRios.Identidad.Application.CasosUso;
using IrmaRios.Identidad.Application.Dtos;
using IrmaRios.Identidad.Domain;
using IrmaRios.Identidad.Domain.Validaciones;
using Microsoft.Extensions.Logging.Abstractions;

namespace IrmaRios.Identidad.UnitTests;

public class AltaEmpresaHandlerTests
{
    private readonly EmpresaRepositoryFake _empresas = new();
    private readonly PersonaRepositoryFake _personas = new();
    private readonly VinculoRepositoryFake _vinculos = new();
    private readonly AlmacenIdempotenciaFake _idempotencia = new();
    private readonly UnitOfWorkFake _uow = new();

    private AltaEmpresaHandler Handler() => new(
        _empresas, _personas, _vinculos, _idempotencia, _uow,
        NullLogger<AltaEmpresaHandler>.Instance);

    private static AltaEmpresaRequest Request(string cuit) => new(
        cuit, "IrmaRios SA",
        [new PersonaRequest("DNI", "12345678", "Ana", "Pérez", "ana@irma.rios", "Apoderado")]);

    [Fact]
    public async Task Alta_crea_empresa_persona_y_vinculo()
    {
        var resultado = await Handler().Ejecutar(Request(ValidadorCuit.Completar("2012345678")), "key-1", CancellationToken.None);

        Assert.False(resultado.EsReplay);
        Assert.Single(_empresas.Datos);
        Assert.Single(_personas.Datos);
        Assert.Single(_vinculos.Datos);
        Assert.Equal(RolVinculo.Apoderado, _vinculos.Datos[0].Rol);
        Assert.Single(resultado.Respuesta.Personas);
    }

    [Fact]
    public async Task Reenvio_con_misma_key_y_mismo_body_es_replay_sin_duplicar()
    {
        var cuit = ValidadorCuit.Completar("2012345678");
        await Handler().Ejecutar(Request(cuit), "key-1", CancellationToken.None);
        var resultado = await Handler().Ejecutar(Request(cuit), "key-1", CancellationToken.None);

        Assert.True(resultado.EsReplay);
        Assert.Single(_empresas.Datos);
        // El replay no vuelve a persistir: una sola escritura real.
        Assert.Equal(1, _uow.Guardadas);
    }

    [Fact]
    public async Task Misma_key_con_otro_body_es_conflicto()
    {
        var cuit = ValidadorCuit.Completar("2012345678");
        await Handler().Ejecutar(Request(cuit), "key-1", CancellationToken.None);

        var otro = new AltaEmpresaRequest(
            ValidadorCuit.Completar("3051122233"), "Otra SA", null);

        await Assert.ThrowsAsync<ConflictoDominioException>(
            () => Handler().Ejecutar(otro, "key-1", CancellationToken.None));
    }

    [Fact]
    public async Task Cuit_duplicado_es_conflicto()
    {
        var cuit = ValidadorCuit.Completar("2012345678");
        await Handler().Ejecutar(Request(cuit), "key-1", CancellationToken.None);

        await Assert.ThrowsAsync<ConflictoDominioException>(
            () => Handler().Ejecutar(Request(cuit), "key-2", CancellationToken.None));
    }

    [Fact]
    public async Task Persona_existente_en_otra_empresa_se_reutiliza()
    {
        var persona = Domain.Entidades.Persona.Crear("DNI", "12345678", "Ana", "Pérez", "ana@irma.rios");
        _personas.Datos.Add(persona);

        var resultado = await Handler().Ejecutar(
            Request(ValidadorCuit.Completar("2012345678")), "key-1", CancellationToken.None);

        Assert.Single(_personas.Datos);
        Assert.Single(_vinculos.Datos);
    }

    [Fact]
    public async Task Rol_invalido_rechaza()
    {
        var request = new AltaEmpresaRequest(
            ValidadorCuit.Completar("2012345678"), "IrmaRios SA",
            [new PersonaRequest("DNI", "12345678", "Ana", "Pérez", "ana@irma.rios", "Gerente")]);

        await Assert.ThrowsAsync<ValidacionException>(
            () => Handler().Ejecutar(request, "key-1", CancellationToken.None));
    }
}
