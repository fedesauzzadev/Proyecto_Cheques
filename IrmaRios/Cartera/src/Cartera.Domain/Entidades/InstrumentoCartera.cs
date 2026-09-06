using IrmaRios.Cartera.Domain;
using IrmaRios.Cartera.Domain.Validaciones;

namespace IrmaRios.Cartera.Domain.Entidades;

/// <summary>
/// Espejo durable en el banco de un instrumento que vive en el clearing (HU-02/03).
/// El clearing es la fuente de verdad; esta copia alimenta la cartera del cliente
/// y es la base del cache de consultas sin castigar a COELSA.
/// </summary>
public sealed class InstrumentoCartera
{
    public Guid Id { get; private set; }

    public TipoInstrumento Tipo { get; private set; }

    /// <summary>CMC7 (cheque físico) o IDECHEQ (echeq); único en la cartera.</summary>
    public string Identificador { get; private set; } = string.Empty;

    public string CuitLibrador { get; private set; } = string.Empty;

    public string CuitBeneficiario { get; private set; } = string.Empty;

    /// <summary>CUIT de la empresa del banco que lo tiene en cartera.</summary>
    public string TitularCuit { get; private set; } = string.Empty;

    public decimal Monto { get; private set; }

    public Moneda Moneda { get; private set; }

    public DateOnly FechaVencimiento { get; private set; }

    public EstadoClearing EstadoClearing { get; private set; }

    public EstadoCartera Estado { get; private set; }

    public DateTime UltimaSincronizacionUtc { get; private set; }

    public DateTime CreadoUtc { get; private set; }

    private InstrumentoCartera() { }

    /// <summary>
    /// Reglas de ingreso a cartera (HU-02): identificador bien formado, instrumento
    /// a favor de la empresa depositante, estado negociable en el clearing y no vencido.
    /// </summary>
    public static InstrumentoCartera Ingresar(
        TipoInstrumento tipo,
        string identificador,
        string cuitLibrador,
        string cuitBeneficiario,
        decimal monto,
        Moneda moneda,
        DateOnly fechaVencimiento,
        EstadoClearing estadoClearing,
        string cuitEmpresaDepositante,
        DateOnly hoy,
        DateTime ahoraUtc)
    {
        ValidadorIdentificadores.Validar(tipo, identificador);

        if (cuitBeneficiario != cuitEmpresaDepositante)
        {
            throw new ValidacionException(
                $"El instrumento {identificador} está a favor del CUIT {cuitBeneficiario}, no de la empresa depositante ({cuitEmpresaDepositante}).");
        }

        if (!EsNegociable(estadoClearing))
        {
            throw new ValidacionException(
                $"El instrumento {identificador} está en estado {estadoClearing} en el clearing y no es negociable.");
        }

        if (fechaVencimiento < hoy)
        {
            throw new ValidacionException(
                $"El instrumento {identificador} venció el {fechaVencimiento:yyyy-MM-dd} y no puede ingresar a cartera.");
        }

        if (monto <= 0)
        {
            throw new ValidacionException($"El instrumento {identificador} tiene un monto inválido.");
        }

        return new InstrumentoCartera
        {
            Id = Guid.NewGuid(),
            Tipo = tipo,
            Identificador = identificador,
            CuitLibrador = cuitLibrador,
            CuitBeneficiario = cuitBeneficiario,
            TitularCuit = cuitEmpresaDepositante,
            Monto = monto,
            Moneda = moneda,
            FechaVencimiento = fechaVencimiento,
            EstadoClearing = estadoClearing,
            Estado = EstadoCartera.EnCartera,
            UltimaSincronizacionUtc = ahoraUtc,
            CreadoUtc = ahoraUtc
        };
    }

    /// <summary>Solo estados tempranos del clearing permiten el ingreso a cartera.</summary>
    public static bool EsNegociable(EstadoClearing estado)
        => estado is EstadoClearing.Emitido or EstadoClearing.Pendiente;

    public void Sincronizar(EstadoClearing estado, DateTime ahoraUtc)
    {
        EstadoClearing = estado;
        UltimaSincronizacionUtc = ahoraUtc;
    }
}
