namespace Coelsa.Application.Dtos;

public class DesgloseCmc7Dto
{
    public string Banco { get; set; } = null!;
    public string Sucursal { get; set; } = null!;
    public string CodigoPostal { get; set; } = null!;
    public string NumeroCheque { get; set; } = null!;
    public string NumeroCuenta { get; set; } = null!;
}

public class ChequeResponse
{
    /// <summary>El CMC7 completo: identificador de negocio del cheque físico.</summary>
    public string Identificador { get; set; } = null!;
    public string Tipo { get; set; } = "ChequeFisico";
    public DesgloseCmc7Dto DesgloseCmc7 { get; set; } = null!;
    public string CuitLibrador { get; set; } = null!;
    public string CuitBeneficiario { get; set; } = null!;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = null!;
    public DateOnly FechaEmision { get; set; }
    public DateOnly? FechaDiferimiento { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public string Estado { get; set; } = null!;
    public int? MotivoRechazo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class EcheqResponse
{
    /// <summary>El IDECHEQ (11 letras mayúsculas): identificador de negocio del echeq.</summary>
    public string Identificador { get; set; } = null!;
    public string Tipo { get; set; } = "Echeq";
    public string CbuEmisor { get; set; } = null!;
    public int NumeroChequera { get; set; }
    public int NumeroCheque { get; set; }
    public string Caracter { get; set; } = null!;
    /// <summary>Siempre "Cruzado": el echeq solo se deposita en cuenta.</summary>
    public string Modo { get; set; } = "Cruzado";
    public string TipoDocBeneficiario { get; set; } = null!;
    public string NombreLibrador { get; set; } = null!;
    public string NombreBeneficiario { get; set; } = null!;
    public string? Concepto { get; set; }
    public string? Motivo { get; set; }
    public string? Referencia { get; set; }
    public string? EmailNotificacion { get; set; }
    public string Cmc7 { get; set; } = null!;
    public DesgloseCmc7Dto DesgloseCmc7 { get; set; } = null!;
    public string CuitLibrador { get; set; } = null!;
    public string CuitBeneficiario { get; set; } = null!;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = null!;
    public DateOnly FechaEmision { get; set; }
    public DateOnly? FechaDiferimiento { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public string Estado { get; set; } = null!;
    public int? MotivoRechazo { get; set; }
    public string? MotivoRepudio { get; set; }
    public int CantidadEndosos { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class EndosoResponse
{
    public int Orden { get; set; }
    public string CuitEndosante { get; set; } = null!;
    public string CuitEndosatario { get; set; } = null!;
    public string Estado { get; set; } = null!;
    public DateTime FechaCreacion { get; set; }
}

public class DevolucionResponse
{
    public int Numero { get; set; }
    public string CuitSolicitante { get; set; } = null!;
    public string? Motivo { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime FechaCreacion { get; set; }
}

public class CesionResponse
{
    public int Numero { get; set; }
    public string CuitCedente { get; set; } = null!;
    public string CuitCesionario { get; set; } = null!;
    public string DomicilioCesionario { get; set; } = null!;
    public string Estado { get; set; } = null!;
    public DateTime FechaCreacion { get; set; }
}

/// <summary>
/// Certificado para ejercer acciones civiles ante un echeq rechazado (Fase D3):
/// CUD determinista + datos del rechazo. Habilita el reclamo judicial y es la
/// base documental de la contragarantía en el descuento.
/// </summary>
public class CertificadoResponse
{
    public string Cud { get; set; } = null!;
    public string CodigoVisualizacion { get; set; } = null!;
    public string IdEcheq { get; set; } = null!;
    public string Cmc7 { get; set; } = null!;
    public string Estado { get; set; } = null!;
    public int? MotivoRechazo { get; set; }
    public string CuitLibrador { get; set; } = null!;
    public string NombreLibrador { get; set; } = null!;
    public string CuitBeneficiario { get; set; } = null!;
    public string NombreBeneficiario { get; set; } = null!;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = null!;
    public DateOnly FechaEmision { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public DateTime? FechaRechazo { get; set; }
}
