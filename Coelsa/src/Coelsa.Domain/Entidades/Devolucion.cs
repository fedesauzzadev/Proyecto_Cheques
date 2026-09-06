using Coelsa.Domain.Validaciones;

namespace Coelsa.Domain.Entidades;

/// <summary>
/// Pedido de devolución de un echeq (SPEC Fase A): cualquier integrante de la
/// cadena (distinto del tenedor actual) puede solicitarla; el tenedor actual la
/// acepta o rechaza; el solicitante puede anularla mientras está solicitada.
/// Al aceptarse, la tenencia vuelve al solicitante y se revierten los endosos
/// vigentes posteriores a su posición en la cadena.
/// </summary>
public class Devolucion
{
    public const int LongitudMaximaMotivo = 280;

    public Guid Id { get; private set; }
    public Guid EcheqId { get; private set; }
    public int Numero { get; private set; }
    public string CuitSolicitante { get; private set; } = null!;
    public string? Motivo { get; private set; }
    public EstadoDevolucion Estado { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaModificacion { get; private set; }

    private Devolucion()
    {
    }

    public static Devolucion Solicitar(Guid echeqId, int numero, string cuitSolicitante, string? motivo)
    {
        if (numero < 1)
        {
            throw new ValidacionException("El número de la devolución debe ser mayor a cero.");
        }

        if (!ValidadorCuit.EsValido(cuitSolicitante))
        {
            throw new ValidacionException($"El CUIT/CUIL del solicitante '{cuitSolicitante}' no es válido (se espera 11 dígitos con verificador módulo 11).");
        }

        if (motivo is not null && motivo.Length > LongitudMaximaMotivo)
        {
            throw new ValidacionException($"El motivo no puede superar los {LongitudMaximaMotivo} caracteres.");
        }

        return new Devolucion
        {
            Id = Guid.NewGuid(),
            EcheqId = echeqId,
            Numero = numero,
            CuitSolicitante = cuitSolicitante,
            Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim(),
            Estado = EstadoDevolucion.Solicitada,
            FechaCreacion = DateTime.UtcNow
        };
    }

    /// <summary>El tenedor actual acepta: la tenencia volverá al solicitante.</summary>
    public void Aceptar()
    {
        if (Estado != EstadoDevolucion.Solicitada)
        {
            throw new TransicionInvalidaException($"Solo una devolución solicitada puede aceptarse (estado actual: {Estado}).");
        }

        Estado = EstadoDevolucion.Aceptada;
        FechaModificacion = DateTime.UtcNow;
    }

    /// <summary>El tenedor actual rechaza el pedido.</summary>
    public void Rechazar()
    {
        if (Estado != EstadoDevolucion.Solicitada)
        {
            throw new TransicionInvalidaException($"Solo una devolución solicitada puede rechazarse (estado actual: {Estado}).");
        }

        Estado = EstadoDevolucion.Rechazada;
        FechaModificacion = DateTime.UtcNow;
    }

    /// <summary>El solicitante anula su pedido mientras sigue solicitado.</summary>
    public void Anular()
    {
        if (Estado != EstadoDevolucion.Solicitada)
        {
            throw new TransicionInvalidaException($"Solo una devolución solicitada puede anularse (estado actual: {Estado}).");
        }

        Estado = EstadoDevolucion.Anulada;
        FechaModificacion = DateTime.UtcNow;
    }
}
