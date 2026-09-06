using IrmaRios.Identidad.Domain;
using IrmaRios.Identidad.Domain.Entidades;
using IrmaRios.Identidad.Domain.Validaciones;

namespace IrmaRios.Identidad.UnitTests;

public class DominioIdentidadTests
{
    [Fact]
    public void Crear_empresa_con_cuit_invalido_rechaza()
    {
        var ex = Assert.Throws<ValidacionException>(
            () => Empresa.Crear("20123456789", "IrmaRios SA", DateTime.UtcNow));
        Assert.Contains("CUIT", ex.Message);
    }

    [Fact]
    public void Crear_empresa_con_razon_social_vacia_rechaza()
    {
        var cuit = ValidadorCuit.Completar("2012345678");
        Assert.Throws<ValidacionException>(() => Empresa.Crear(cuit, "   ", DateTime.UtcNow));
    }

    [Fact]
    public void Crear_empresa_valida_queda_activa()
    {
        var cuit = ValidadorCuit.Completar("2012345678");
        var empresa = Empresa.Crear(cuit, "IrmaRios SA", DateTime.UtcNow);

        Assert.Equal(cuit, empresa.Cuit);
        Assert.False(empresa.Borrado);
    }

    [Theory]
    [InlineData("", "12345678")]
    [InlineData("DNI", "12")]
    [InlineData("PASAPORTE", "12345678")]
    public void Crear_persona_con_documento_invalido_rechaza(string docTipo, string docNumero)
    {
        Assert.Throws<ValidacionException>(
            () => Persona.Crear(docTipo, docNumero, "Ana", "Pérez", "ana@irma.rios"));
    }

    [Fact]
    public void Crear_persona_con_email_invalido_rechaza()
    {
        Assert.Throws<ValidacionException>(
            () => Persona.Crear("DNI", "12345678", "Ana", "Pérez", "no-es-email"));
    }

    [Fact]
    public void Vincular_y_cerrar_vinculo()
    {
        var persona = Persona.Crear("DNI", "12345678", "Ana", "Pérez", "ana@irma.rios");
        var empresa = Empresa.Crear(ValidadorCuit.Completar("2012345678"), "IrmaRios SA", DateTime.UtcNow);
        var ahora = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var vinculo = Vinculo.Vincular(persona.Id, empresa.Id, RolVinculo.Apoderado, ahora);

        Assert.True(vinculo.EsVigente(ahora.AddDays(1)));
        vinculo.Cerrar(ahora.AddDays(10));
        Assert.False(vinculo.EsVigente(ahora.AddDays(11)));
    }

    [Fact]
    public void Cerrar_vinculo_con_anterior_al_alta_rechaza()
    {
        var persona = Persona.Crear("DNI", "12345678", "Ana", "Pérez", "ana@irma.rios");
        var ahora = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var vinculo = Vinculo.Vincular(persona.Id, Guid.NewGuid(), RolVinculo.Consultor, ahora);

        Assert.Throws<ValidacionException>(() => vinculo.Cerrar(ahora.AddDays(-1)));
    }

    [Fact]
    public void Cuit_con_digito_verificador_correcto_es_valido()
    {
        Assert.True(ValidadorCuit.EsValido(ValidadorCuit.Completar("2787654321")));
        Assert.False(ValidadorCuit.EsValido("27876543218"));
    }
}
