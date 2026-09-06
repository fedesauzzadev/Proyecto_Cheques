using System.Text.RegularExpressions;
using IrmaRios.Identidad.Domain;

namespace IrmaRios.Identidad.Domain.Entidades;

/// <summary>Persona física que puede estar autorizada en una o más empresas (HU-01).</summary>
public sealed class Persona
{
    private static readonly string[] TiposDocumento = ["DNI", "CUIT", "CUIL", "PAS"];

    public Guid Id { get; private set; }

    public string DocTipo { get; private set; } = string.Empty;

    public string DocNumero { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public string Apellido { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public bool Borrado { get; private set; }

    private Persona() { }

    public static Persona Crear(
        string docTipo, string docNumero, string nombre, string apellido, string email)
    {
        docTipo = docTipo?.Trim().ToUpperInvariant() ?? string.Empty;
        if (Array.IndexOf(TiposDocumento, docTipo) < 0)
        {
            throw new ValidacionException($"El tipo de documento debe ser uno de: {string.Join(", ", TiposDocumento)}.");
        }

        if (docNumero is null || !Regex.IsMatch(docNumero, @"^\d{6,12}$"))
        {
            throw new ValidacionException("El número de documento debe tener entre 6 y 12 dígitos.");
        }

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido))
        {
            throw new ValidacionException("El nombre y el apellido de la persona son obligatorios.");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new ValidacionException("El email de la persona es obligatorio y debe ser válido.");
        }

        return new Persona
        {
            Id = Guid.NewGuid(),
            DocTipo = docTipo,
            DocNumero = docNumero,
            Nombre = nombre.Trim(),
            Apellido = apellido.Trim(),
            Email = email.Trim().ToLowerInvariant()
        };
    }

    /// <summary>Identidad de negocio de la persona: misma persona = mismo tipo y número de documento.</summary>
    public bool EsMisma(string docTipo, string docNumero)
        => DocTipo == docTipo.Trim().ToUpperInvariant() && DocNumero == docNumero;

    public void Eliminar() => Borrado = true;
}
