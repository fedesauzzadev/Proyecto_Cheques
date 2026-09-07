using System.ComponentModel.DataAnnotations;

namespace Coelsa.Application.Dtos;

public class CrearCuentaRequest
{
    [Required(ErrorMessage = "El CBU es obligatorio.")]
    [RegularExpression(@"^\d{22}$", ErrorMessage = "El CBU debe tener 22 dígitos.")]
    public string Cbu { get; set; } = null!;

    [Required(ErrorMessage = "El CUIT/CUIL del titular es obligatorio.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "El CUIT/CUIL del titular debe tener 11 dígitos.")]
    public string CuitTitular { get; set; } = null!;

    [Required(ErrorMessage = "El nombre del titular es obligatorio.")]
    [StringLength(120, ErrorMessage = "El nombre del titular debe tener hasta 120 caracteres.")]
    public string NombreTitular { get; set; } = null!;

    [Required(ErrorMessage = "La moneda es obligatoria.")]
    [RegularExpression(@"^[PD]$", ErrorMessage = "La moneda debe ser 'P' (pesos) o 'D' (dólares).")]
    public string Moneda { get; set; } = null!;
}

public class CuentaResponse
{
    /// <summary>El CBU de 22 dígitos: identificador de negocio de la cuenta.</summary>
    public string Cbu { get; set; } = null!;
    public string Banco { get; set; } = null!;
    public string Sucursal { get; set; } = null!;
    public string NumeroCuenta { get; set; } = null!;
    public string CuitTitular { get; set; } = null!;
    public string NombreTitular { get; set; } = null!;
    public string Moneda { get; set; } = null!;
    public DateTime FechaCreacion { get; set; }
}

public class ChequeraResponse
{
    public int Numero { get; set; }
    public int CantidadTotal { get; set; }
    public int ProximoNumero { get; set; }
    public int Disponibles { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime FechaSolicitud { get; set; }
    public DateTime FechaHabilitacion { get; set; }
}

/// <summary>
/// Resultado del padrón simulado de titulares (Fase B3): valida el documento
/// y devuelve el nombre si el titular tiene cuenta; si no, bancarizado=false
/// y el nombre lo informa el usuario al emitir (divergencia documentada: el
/// banco real exige beneficiario bancarizado).
/// </summary>
public class TitularResponse
{
    public string TipoDoc { get; set; } = null!;
    public string Numero { get; set; } = null!;
    public string? Nombre { get; set; }
    public bool Bancarizado { get; set; }
}
