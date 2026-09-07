using Coelsa.Domain.Validaciones;

namespace Coelsa.Domain.Entidades;

/// <summary>
/// Cesión electrónica de un echeq "no a la orden" (Fase D1): el tenedor actual
/// cede sus derechos a un tercero bancarizado indicando su domicilio. El
/// cesionario la acepta o rechaza; el cedente puede anularla mientras está
/// solicitada. Al aceptarse, la tenencia pasa al cesionario.
/// Máximo 10 cesiones por echeq (regla BCRA).
/// </summary>
public class Cesion
{
    public const int MaximoCesiones = 10;
    public const int LongitudMaximaDomicilio = 200;

    public Guid Id { get; private set; }
    public Guid EcheqId { get; private set; }
    public int Numero { get; private set; }
    public string CuitCedente { get; private set; } = null!;
    public string CuitCesionario { get; private set; } = null!;
    public string DomicilioCesionario { get; private set; } = null!;
    public EstadoCesion Estado { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaModificacion { get; private set; }

    private Cesion()
    {
    }

    public static Cesion Solicitar(
        Guid echeqId, int numero, string cuitCedente, string cuitCesionario, string domicilioCesionario)
    {
        if (numero < 1)
        {
            throw new ValidacionException("El número de la cesión debe ser mayor a cero.");
        }

        if (!ValidadorCuit.EsValido(cuitCedente))
        {
            throw new ValidacionException($"El CUIT/CUIL del cedente '{cuitCedente}' no es válido (se espera 11 dígitos con verificador módulo 11).");
        }

        if (!ValidadorCuit.EsValido(cuitCesionario))
        {
            throw new ValidacionException($"El CUIT/CUIL del cesionario '{cuitCesionario}' no es válido (se espera 11 dígitos con verificador módulo 11).");
        }

        if (cuitCedente == cuitCesionario)
        {
            throw new ValidacionException("El cedente y el cesionario no pueden ser el mismo CUIT/CUIL.");
        }

        if (string.IsNullOrWhiteSpace(domicilioCesionario) || domicilioCesionario.Trim().Length > LongitudMaximaDomicilio)
        {
            throw new ValidacionException($"El domicilio del cesionario es obligatorio y debe tener hasta {LongitudMaximaDomicilio} caracteres.");
        }

        return new Cesion
        {
            Id = Guid.NewGuid(),
            EcheqId = echeqId,
            Numero = numero,
            CuitCedente = cuitCedente,
            CuitCesionario = cuitCesionario,
            DomicilioCesionario = domicilioCesionario.Trim(),
            Estado = EstadoCesion.Solicitada,
            FechaCreacion = DateTime.UtcNow
        };
    }

    /// <summary>El cesionario acepta: la tenencia pasará a él.</summary>
    public void Aceptar()
    {
        if (Estado != EstadoCesion.Solicitada)
        {
            throw new TransicionInvalidaException($"Solo una cesión solicitada puede aceptarse (estado actual: {Estado}).");
        }

        Estado = EstadoCesion.Aceptada;
        FechaModificacion = DateTime.UtcNow;
    }

    /// <summary>El cesionario rechaza la cesión.</summary>
    public void Rechazar()
    {
        if (Estado != EstadoCesion.Solicitada)
        {
            throw new TransicionInvalidaException($"Solo una cesión solicitada puede rechazarse (estado actual: {Estado}).");
        }

        Estado = EstadoCesion.Rechazada;
        FechaModificacion = DateTime.UtcNow;
    }

    /// <summary>El cedente anula su cesión mientras sigue solicitada.</summary>
    public void Anular()
    {
        if (Estado != EstadoCesion.Solicitada)
        {
            throw new TransicionInvalidaException($"Solo una cesión solicitada puede anularse (estado actual: {Estado}).");
        }

        Estado = EstadoCesion.Anulada;
        FechaModificacion = DateTime.UtcNow;
    }
}
