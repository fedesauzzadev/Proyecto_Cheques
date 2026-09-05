using System.Text.RegularExpressions;
using Coelsa.Domain.Validaciones;

namespace Coelsa.Domain.Entidades;

/// <summary>
/// Echeq identificado por su IDECHEQ alfanumérico asignado al crearlo (SPEC 5.2).
/// </summary>
public class Echeq : IInstrumento
{
    public const int LongitudIdEcheq = 18;
    public const int LongitudCud = 64;

    public Guid Id { get; private set; }
    public string IdEcheq { get; private set; } = null!;
    public string Cud { get; private set; } = null!;
    public string CodigoBanco { get; private set; } = null!;
    public string NumeroCuenta { get; private set; } = null!;
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
        string cud,
        string codigoBanco,
        string numeroCuenta,
        string cuitLibrador,
        string cuitBeneficiario,
        decimal monto,
        Moneda moneda,
        DateOnly fechaEmision,
        DateOnly? fechaDiferimiento,
        DateOnly? hoy = null)
    {
        ValidacionesInstrumento.ValidarComunes(cuitLibrador, cuitBeneficiario, monto, fechaEmision, fechaDiferimiento, hoy);

        if (idEcheq is null || !Regex.IsMatch(idEcheq, $"^[A-Z0-9]{{{LongitudIdEcheq}}}$"))
        {
            throw new ValidacionException($"El IDECHEQ debe ser alfanumérico de {LongitudIdEcheq} caracteres (mayúsculas y dígitos).");
        }

        if (cud is null || !Regex.IsMatch(cud, @"^[0-9a-fA-F]{64}$"))
        {
            throw new ValidacionException("El CUD debe ser un hash SHA-256 expresado en 64 caracteres hexadecimales.");
        }

        if (codigoBanco is null || !Regex.IsMatch(codigoBanco, @"^\d{3}$"))
        {
            throw new ValidacionException("El código de banco debe ser numérico de 3 dígitos (código BCRA).");
        }

        if (numeroCuenta is null || !Regex.IsMatch(numeroCuenta, @"^\d{12}$"))
        {
            throw new ValidacionException("El número de cuenta debe ser numérico de 12 dígitos.");
        }

        return new Echeq
        {
            Id = Guid.NewGuid(),
            IdEcheq = idEcheq,
            Cud = cud,
            CodigoBanco = codigoBanco,
            NumeroCuenta = numeroCuenta,
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
}
