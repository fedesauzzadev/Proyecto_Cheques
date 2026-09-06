using System.Text.RegularExpressions;
using Coelsa.Domain.Validaciones;
using Coelsa.Domain.ValueObjects;

namespace Coelsa.Domain.Entidades;

/// <summary>
/// Echeq identificado por su IDECHEQ alfabético de 11 letras asignado al crearlo (SPEC 5.2).
/// Todo echeq lleva su CMC7 completo de 30 dígitos, como los cheques físicos.
/// </summary>
public class Echeq : IInstrumento
{
    public const int LongitudIdEcheq = 11;

    public Guid Id { get; private set; }
    public string IdEcheq { get; private set; } = null!;
    public string Cmc7 { get; private set; } = null!;
    public string CuitLibrador { get; private set; } = null!;
    public string CuitBeneficiario { get; private set; } = null!;
    public decimal Monto { get; private set; }
    public Moneda Moneda { get; private set; }
    public DateOnly FechaEmision { get; private set; }
    public DateOnly? FechaDiferimiento { get; private set; }
    public DateOnly FechaVencimiento { get; private set; }
    public EstadoInstrumento Estado { get; private set; }
    public MotivoRechazo? MotivoRechazo { get; private set; }
    public int CantidadEndosos { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaModificacion { get; private set; }
    public DateTime? FechaBaja { get; private set; }
    public bool Activo { get; private set; } = true;

    private Echeq()
    {
    }

    public static Echeq Crear(
        string idEcheq,
        string cmc7,
        string cuitLibrador,
        string cuitBeneficiario,
        decimal monto,
        Moneda moneda,
        DateOnly fechaEmision,
        DateOnly? fechaDiferimiento,
        DateOnly fechaVencimiento,
        DateOnly? hoy = null)
    {
        ValidacionesInstrumento.ValidarComunes(cuitLibrador, cuitBeneficiario, monto, fechaEmision, fechaDiferimiento, fechaVencimiento, hoy);

        if (idEcheq is null || !Regex.IsMatch(idEcheq, $"^[A-Z]{{{LongitudIdEcheq}}}$"))
        {
            throw new ValidacionException($"El IDECHEQ debe ser alfabético de {LongitudIdEcheq} letras mayúsculas.");
        }

        var cmc7Vo = ValueObjects.Cmc7.Crear(cmc7);

        return new Echeq
        {
            Id = Guid.NewGuid(),
            IdEcheq = idEcheq,
            Cmc7 = cmc7Vo.Valor,
            CuitLibrador = cuitLibrador!,
            CuitBeneficiario = cuitBeneficiario!,
            Monto = monto,
            Moneda = moneda,
            FechaEmision = fechaEmision,
            FechaDiferimiento = fechaDiferimiento,
            FechaVencimiento = fechaVencimiento,
            Estado = EstadoInstrumento.Pendiente,
            CantidadEndosos = 0,
            FechaCreacion = DateTime.UtcNow,
            Activo = true
        };
    }

    /// <summary>Aceptación del beneficiario: el echeq pendiente entra en circulación.</summary>
    public void Aceptar()
    {
        if (!Activo)
        {
            throw new TransicionInvalidaException("No se puede aceptar un echeq dado de baja.");
        }

        CambiarEstado(EstadoInstrumento.Emitido, null);
    }

    /// <summary>Repudio del beneficiario: rechaza el echeq pendiente (terminal).</summary>
    public void Repudiar()
    {
        if (!Activo)
        {
            throw new TransicionInvalidaException("No se puede repudiar un echeq dado de baja.");
        }

        CambiarEstado(EstadoInstrumento.Repudiado, null);
    }

    /// <summary>Pone el echeq en custodia del banco (solo desde Emitido).</summary>
    public void PonerEnCustodia() => CambiarEstado(EstadoInstrumento.EnCustodia, null);

    /// <summary>Rescate: saca el echeq de custodia y lo devuelve a Emitido.</summary>
    public void Rescatar() => CambiarEstado(EstadoInstrumento.Emitido, null);

    /// <summary>
    /// Débito automático al vencer (worker): deposita un echeq en custodia cuya
    /// fecha de vencimiento ya pasó.
    /// </summary>
    public void DepositarPorVencimiento(DateOnly hoy)
    {
        if (FechaVencimiento > hoy)
        {
            throw new TransicionInvalidaException(
                $"El echeq con IDECHEQ {IdEcheq} aún no venció (vence el {FechaVencimiento:yyyy-MM-dd}).");
        }

        CambiarEstado(EstadoInstrumento.Depositado, null);
    }

    /// <summary>Cambia la tenencia al admitir un endoso o aceptar una devolución.</summary>
    public void CambiarTenencia(string nuevoCuitBeneficiario)
    {
        if (!Activo)
        {
            throw new TransicionInvalidaException("No se puede cambiar la tenencia de un echeq dado de baja.");
        }

        if (!Validaciones.ValidadorCuit.EsValido(nuevoCuitBeneficiario))
        {
            throw new ValidacionException($"El CUIT/CUIL del nuevo beneficiario '{nuevoCuitBeneficiario}' no es válido (se espera 11 dígitos con verificador módulo 11).");
        }

        CuitBeneficiario = nuevoCuitBeneficiario;
        FechaModificacion = DateTime.UtcNow;
    }

    /// <summary>Refresca el contador informativo con los endosos vigentes.</summary>
    public void FijarCantidadEndosos(int cantidad)
    {
        CantidadEndosos = cantidad;
        FechaModificacion = DateTime.UtcNow;
    }

    public void CambiarEstado(EstadoInstrumento nuevoEstado, MotivoRechazo? motivoRechazo)
    {
        if (!Activo)
        {
            throw new TransicionInvalidaException("No se puede cambiar el estado de un echeq dado de baja.");
        }

        ValidacionesInstrumento.ValidarEstadoCambio(Estado, nuevoEstado, motivoRechazo);

        Estado = nuevoEstado;
        MotivoRechazo = motivoRechazo;
        FechaModificacion = DateTime.UtcNow;
    }

    public void Eliminar()
    {
        if (!Activo)
        {
            throw new NoEncontradoException($"El echeq con IDECHEQ {IdEcheq} ya se encuentra dado de baja.");
        }

        Activo = false;
        FechaBaja = DateTime.UtcNow;
    }

    public DesgloseCmc7 DesglosarCmc7() => ValueObjects.Cmc7.Crear(Cmc7).Desglosar();
}
