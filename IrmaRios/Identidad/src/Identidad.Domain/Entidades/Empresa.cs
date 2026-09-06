using IrmaRios.Identidad.Domain.Validaciones;

namespace IrmaRios.Identidad.Domain.Entidades;

/// <summary>Agregado raíz: empresa cliente del banco (persona jurídica, HU-01).</summary>
public sealed class Empresa
{
    public Guid Id { get; private set; }

    /// <summary>CUIT de 11 dígitos con dígito verificador válido; único entre empresas activas.</summary>
    public string Cuit { get; private set; } = string.Empty;

    public string RazonSocial { get; private set; } = string.Empty;

    /// <summary>Baja lógica: la empresa deja de operar pero la historia se conserva (ADR-007).</summary>
    public bool Borrado { get; private set; }

    public DateTime CreadoUtc { get; private set; }

    private Empresa() { }

    public static Empresa Crear(string cuit, string razonSocial, DateTime ahoraUtc)
    {
        if (!ValidadorCuit.EsValido(cuit))
        {
            throw new ValidacionException("El CUIT debe ser válido de 11 dígitos con dígito verificador correcto.");
        }

        if (string.IsNullOrWhiteSpace(razonSocial) || razonSocial.Trim().Length > 200)
        {
            throw new ValidacionException("La razón social es obligatoria (máximo 200 caracteres).");
        }

        return new Empresa
        {
            Id = Guid.NewGuid(),
            Cuit = cuit,
            RazonSocial = razonSocial.Trim(),
            CreadoUtc = ahoraUtc
        };
    }

    public void Eliminar() => Borrado = true;
}
