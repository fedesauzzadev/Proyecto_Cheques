namespace Coelsa.Domain.Entidades;

/// <summary>
/// E-chequera: talonario electrónico de 50 números contra una cuenta (Fase B).
/// Se solicita por cuenta y el simulador la habilita de inmediato (en la
/// operatoria real son 24h). Cada emisión reserva el próximo número; al
/// agotarse se solicita una chequera nueva (una cuenta puede tener varias).
/// </summary>
public class Chequera
{
    public const int CantidadNumeros = 50;

    public Guid Id { get; private set; }
    public Guid CuentaId { get; private set; }
    public int Numero { get; private set; }
    public int ProximoNumero { get; private set; }
    public EstadoChequera Estado { get; private set; }
    public DateTime FechaSolicitud { get; private set; }
    public DateTime FechaHabilitacion { get; private set; }
    public bool Activa { get; private set; } = true;

    private Chequera()
    {
    }

    public static Chequera Solicitar(Guid cuentaId, int numero)
    {
        if (numero < 1)
        {
            throw new ValidacionException("El número de chequera debe ser mayor a cero.");
        }

        var ahora = DateTime.UtcNow;
        return new Chequera
        {
            Id = Guid.NewGuid(),
            CuentaId = cuentaId,
            Numero = numero,
            ProximoNumero = 1,
            Estado = EstadoChequera.Vigente,
            FechaSolicitud = ahora,
            // El simulador acelera las 24h reales de habilitación.
            FechaHabilitacion = ahora,
            Activa = true
        };
    }

    public bool TieneLugar => Activa && Estado == EstadoChequera.Vigente && ProximoNumero <= CantidadNumeros;

    /// <summary>Reserva el próximo número de cheque (1-based) y agota la chequera al llegar al 50.</summary>
    public int ReservarNumero()
    {
        if (!TieneLugar)
        {
            throw new ConflictoDominioException(
                "La chequera no tiene números disponibles. Solicite una chequera nueva.");
        }

        var numero = ProximoNumero;
        ProximoNumero++;

        if (ProximoNumero > CantidadNumeros)
        {
            Estado = EstadoChequera.Agotada;
        }

        return numero;
    }
}
