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
        DateOnly? hoy = null)
    {
        ValidacionesInstrumento.ValidarComunes(cuitLibrador, cuitBeneficiario, monto, fechaEmision, fechaDiferimiento, hoy);

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
            Estado = EstadoInstrumento.Emitido,
            CantidadEndosos = 0,
            FechaCreacion = DateTime.UtcNow,
            Activo = true
        };
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
