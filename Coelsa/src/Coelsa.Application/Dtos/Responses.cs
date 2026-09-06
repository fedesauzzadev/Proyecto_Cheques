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
    public string Estado { get; set; } = null!;
    public int? MotivoRechazo { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class EcheqResponse
{
    /// <summary>El IDECHEQ (11 letras mayúsculas): identificador de negocio del echeq.</summary>
    public string Identificador { get; set; } = null!;
    public string Tipo { get; set; } = "Echeq";
    public string Cmc7 { get; set; } = null!;
    public DesgloseCmc7Dto DesgloseCmc7 { get; set; } = null!;
    public string CuitLibrador { get; set; } = null!;
    public string CuitBeneficiario { get; set; } = null!;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = null!;
    public DateOnly FechaEmision { get; set; }
    public DateOnly? FechaDiferimiento { get; set; }
    public string Estado { get; set; } = null!;
    public int? MotivoRechazo { get; set; }
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
