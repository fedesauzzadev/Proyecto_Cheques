using Coelsa.Domain.Validaciones;
using Coelsa.Domain.ValueObjects;

namespace Coelsa.Domain.Entidades;

/// <summary>
/// Cheque físico identificado por su CMC7 (SPEC 5.1).
/// El Guid interno es solo PK de base de datos; nunca se expone en la API.
/// </summary>
public class ChequeFisico : IInstrumento
{
    public Guid Id { get; private set; }
    public string Cmc7 { get; private set; } = null!;
    public string CuitLibrador { get; private set; } = null!;
    public string CuitBeneficiario { get; private set; } = null!;
    public decimal Monto { get; private set; }
    public Moneda Moneda { get; private set; }
    public DateOnly FechaEmision { get; private set; }
    public DateOnly? FechaDiferimiento { get; private set; }
    public EstadoInstrumento Estado { get; private set; }
    public MotivoRechazo? MotivoRechazo { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaModificacion { get; private set; }
    public DateTime? FechaBaja { get; private set; }
    public bool Activo { get; private set; } = true;

    private ChequeFisico()
    {
    }

    public static ChequeFisico Crear(
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
        var cmc7Vo = ValueObjects.Cmc7.Crear(cmc7);

        return new ChequeFisico
        {
            Id = Guid.NewGuid(),
            Cmc7 = cmc7Vo.Valor,
            CuitLibrador = cuitLibrador!,
            CuitBeneficiario = cuitBeneficiario!,
            Monto = monto,
            Moneda = moneda,
            FechaEmision = fechaEmision,
            FechaDiferimiento = fechaDiferimiento,
            Estado = EstadoInstrumento.Emitido,
            FechaCreacion = DateTime.UtcNow,
            Activo = true
        };
    }

    public void CambiarEstado(EstadoInstrumento nuevoEstado, MotivoRechazo? motivoRechazo)
    {
        if (!Activo)
        {
            throw new TransicionInvalidaException("No se puede cambiar el estado de un cheque dado de baja.");
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
            throw new NoEncontradoException($"El cheque con CMC7 {Cmc7} ya se encuentra dado de baja.");
        }

        Activo = false;
        FechaBaja = DateTime.UtcNow;
    }

    public DesgloseCmc7 DesglosarCmc7() => ValueObjects.Cmc7.Crear(Cmc7).Desglosar();
}
