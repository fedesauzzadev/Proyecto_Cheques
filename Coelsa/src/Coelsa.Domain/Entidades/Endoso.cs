using Coelsa.Domain.Validaciones;

namespace Coelsa.Domain.Entidades;

/// <summary>
/// Endoso nominativo de un echeq (SPEC Fase A): transmite la tenencia a un nuevo
/// beneficiario (CUIT). Ciclo propio: Propuesto → Vigente (admitido por el
/// endosatario) / Repudiado; el endosante puede Anularlo mientras está propuesto;
/// una devolución aceptada Revierte los endosos posteriores al solicitante.
/// Máximo 100 endosos por echeq (regla BCRA).
/// </summary>
public class Endoso
{
    public const int MaximoEndosos = 100;

    public Guid Id { get; private set; }
    public Guid EcheqId { get; private set; }
    public int Orden { get; private set; }
    public string CuitEndosante { get; private set; } = null!;
    public string CuitEndosatario { get; private set; } = null!;
    public EstadoEndoso Estado { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaModificacion { get; private set; }

    private Endoso()
    {
    }

    public static Endoso Proponer(Guid echeqId, int orden, string cuitEndosante, string cuitEndosatario)
    {
        if (orden < 1)
        {
            throw new ValidacionException("El orden del endoso debe ser mayor a cero.");
        }

        if (!ValidadorCuit.EsValido(cuitEndosante))
        {
            throw new ValidacionException($"El CUIT/CUIL del endosante '{cuitEndosante}' no es válido (se espera 11 dígitos con verificador módulo 11).");
        }

        if (!ValidadorCuit.EsValido(cuitEndosatario))
        {
            throw new ValidacionException($"El CUIT/CUIL del endosatario '{cuitEndosatario}' no es válido (se espera 11 dígitos con verificador módulo 11).");
        }

        if (cuitEndosante == cuitEndosatario)
        {
            throw new ValidacionException("El endosante y el endosatario no pueden ser el mismo CUIT/CUIL.");
        }

        return new Endoso
        {
            Id = Guid.NewGuid(),
            EcheqId = echeqId,
            Orden = orden,
            CuitEndosante = cuitEndosante,
            CuitEndosatario = cuitEndosatario,
            Estado = EstadoEndoso.Propuesto,
            FechaCreacion = DateTime.UtcNow
        };
    }

    /// <summary>El endosatario admite el endoso: entra en vigencia (transmite la tenencia).</summary>
    public void Admitir()
    {
        if (Estado != EstadoEndoso.Propuesto)
        {
            throw new TransicionInvalidaException($"Solo un endoso propuesto puede admitirse (estado actual: {Estado}).");
        }

        Estado = EstadoEndoso.Vigente;
        FechaModificacion = DateTime.UtcNow;
    }

    /// <summary>El endosatario repudia el endoso propuesto.</summary>
    public void Repudiar()
    {
        if (Estado != EstadoEndoso.Propuesto)
        {
            throw new TransicionInvalidaException($"Solo un endoso propuesto puede repudiarse (estado actual: {Estado}).");
        }

        Estado = EstadoEndoso.Repudiado;
        FechaModificacion = DateTime.UtcNow;
    }

    /// <summary>El endosante anula el endoso mientras sigue propuesto.</summary>
    public void Anular()
    {
        if (Estado != EstadoEndoso.Propuesto)
        {
            throw new TransicionInvalidaException($"Solo un endoso propuesto puede anularse (estado actual: {Estado}).");
        }

        Estado = EstadoEndoso.Anulado;
        FechaModificacion = DateTime.UtcNow;
    }

    /// <summary>Una devolución aceptada revierte el endoso (queda en la historia).</summary>
    public void RevertirPorDevolucion()
    {
        if (Estado != EstadoEndoso.Vigente)
        {
            throw new TransicionInvalidaException($"Solo un endoso vigente puede revertirse por devolución (estado actual: {Estado}).");
        }

        Estado = EstadoEndoso.Revertido;
        FechaModificacion = DateTime.UtcNow;
    }
}
