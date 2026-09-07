using System.Text.RegularExpressions;
using Coelsa.Domain.Validaciones;
using Coelsa.Domain.ValueObjects;

namespace Coelsa.Domain.Entidades;

/// <summary>
/// Echeq identificado por su IDECHEQ alfabético de 11 letras asignado al crearlo (SPEC 5.2).
/// Lleva su CMC7 de 30 dígitos derivado de la cuenta de débito + número de chequera (Fase B),
/// como en la operatoria real donde el sistema informa ambos al emitir.
/// </summary>
public class Echeq : IInstrumento
{
    public const int LongitudIdEcheq = 11;

    public Guid Id { get; private set; }
    public string IdEcheq { get; private set; } = null!;
    public string CbuEmisor { get; private set; } = null!;
    public int NumeroChequera { get; private set; }
    public int NumeroCheque { get; private set; }
    public Caracter Caracter { get; private set; }
    public TipoDocumento TipoDocBeneficiario { get; private set; }
    public string NombreLibrador { get; private set; } = null!;
    public string NombreBeneficiario { get; private set; } = null!;
    public string? Concepto { get; private set; }
    public string? Motivo { get; private set; }
    public string? Referencia { get; private set; }
    public string? EmailNotificacion { get; private set; }
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
    public string? MotivoRepudio { get; private set; }
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
        string cbuEmisor,
        int numeroChequera,
        int numeroCheque,
        Caracter caracter,
        TipoDocumento tipoDocBeneficiario,
        string nombreLibrador,
        string nombreBeneficiario,
        string cmc7,
        string cuitLibrador,
        string cuitBeneficiario,
        decimal monto,
        Moneda moneda,
        DateOnly fechaEmision,
        DateOnly? fechaDiferimiento,
        DateOnly fechaVencimiento,
        DateOnly? hoy = null,
        string? concepto = null,
        string? motivo = null,
        string? referencia = null,
        string? emailNotificacion = null)
    {
        ValidacionesInstrumento.ValidarComunes(cuitLibrador, cuitBeneficiario, monto, fechaEmision, fechaDiferimiento, fechaVencimiento, hoy);

        if (idEcheq is null || !Regex.IsMatch(idEcheq, $"^[A-Z]{{{LongitudIdEcheq}}}$"))
        {
            throw new ValidacionException($"El IDECHEQ debe ser alfabético de {LongitudIdEcheq} letras mayúsculas.");
        }

        if (!Validaciones.ValidadorCbu.EsValido(cbuEmisor))
        {
            throw new ValidacionException(
                "El CBU emisor debe tener 22 dígitos con verificadores válidos (entidad + sucursal + cuenta).");
        }

        if (!Validaciones.ValidadorDocumento.EsValido(tipoDocBeneficiario, cuitBeneficiario))
        {
            throw new ValidacionException(
                $"El documento del beneficiario '{cuitBeneficiario}' no es válido para el tipo {Validaciones.ValidadorDocumento.TipoACodigo(tipoDocBeneficiario)}.");
        }

        Validaciones.ValidadorDocumento.ValidarNombre(nombreLibrador, "nombre del librador");
        Validaciones.ValidadorDocumento.ValidarNombre(nombreBeneficiario, "nombre del beneficiario");

        if (numeroChequera < 1)
        {
            throw new ValidacionException("El número de chequera debe ser mayor a cero.");
        }

        var cmc7Vo = ValueObjects.Cmc7.Crear(cmc7);

        var esperado = ValueObjects.Cmc7.Derivar(cbuEmisor!, numeroCheque).Valor;
        if (!string.Equals(cmc7Vo.Valor, esperado, StringComparison.Ordinal))
        {
            throw new ValidacionException(
                "El CMC7 no coincide con el derivado del CBU emisor y el número de cheque.");
        }

        return new Echeq
        {
            Id = Guid.NewGuid(),
            IdEcheq = idEcheq,
            CbuEmisor = cbuEmisor!,
            NumeroChequera = numeroChequera,
            NumeroCheque = numeroCheque,
            Caracter = caracter,
            TipoDocBeneficiario = tipoDocBeneficiario,
            NombreLibrador = nombreLibrador!.Trim(),
            NombreBeneficiario = nombreBeneficiario!.Trim(),
            Concepto = ValidacionesInstrumento.NormalizarGestion(concepto, ValidacionesInstrumento.LongitudConcepto, "concepto"),
            Motivo = ValidacionesInstrumento.NormalizarGestion(motivo, ValidacionesInstrumento.LongitudMotivo, "motivo"),
            Referencia = ValidacionesInstrumento.NormalizarGestion(referencia, ValidacionesInstrumento.LongitudReferencia, "referencia"),
            EmailNotificacion = ValidacionesInstrumento.NormalizarGestion(emailNotificacion, ValidacionesInstrumento.LongitudEmail, "email", esEmail: true),
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

    /// <summary>Repudio del beneficiario: rechaza el echeq pendiente (terminal). Exige motivo.</summary>
    public void Repudiar(string? motivo)
    {
        if (!Activo)
        {
            throw new TransicionInvalidaException("No se puede repudiar un echeq dado de baja.");
        }

        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 280)
        {
            throw new ValidacionException("El repudio exige un motivo de hasta 280 caracteres.");
        }

        MotivoRepudio = motivo.Trim();
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

    /// <summary>
    /// CUD (Clave Única Digital, Fase D3): hash SHA-256 de los datos del echeq,
    /// base del certificado para ejercer acciones civiles ante un rechazo.
    /// Determinista: el mismo echeq siempre da el mismo CUD.
    /// </summary>
    public string CalcularCud()
    {
        var canonico = string.Join('|',
            IdEcheq,
            Cmc7,
            CbuEmisor,
            CuitLibrador,
            CuitBeneficiario,
            Monto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            ((int)Moneda).ToString(System.Globalization.CultureInfo.InvariantCulture),
            FechaEmision.ToString("yyyy-MM-dd"),
            FechaVencimiento.ToString("yyyy-MM-dd"));

        return Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonico)));
    }

    /// <summary>Código de visualización del certificado (primeros 12 del CUD).</summary>
    public string CodigoVisualizacion() => CalcularCud()[..12];
}
