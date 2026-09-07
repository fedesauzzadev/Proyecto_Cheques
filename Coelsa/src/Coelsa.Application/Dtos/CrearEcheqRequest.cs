using System.ComponentModel.DataAnnotations;

namespace Coelsa.Application.Dtos;

public class CrearEcheqRequest
{
    // Nota: el CMC7 y el IDECHEQ los genera el simulador al crear (operatoria
    // real: el usuario elige la cuenta de débito y el sistema informa CMC7 + ID
    // al confirmar la emisión). No vienen en el request.

    [Required(ErrorMessage = "El CBU de la cuenta de débito es obligatorio.")]
    [RegularExpression(@"^\d{22}$",
        ErrorMessage = "El CBU de la cuenta de débito debe tener 22 dígitos.")]
    public string CbuEmisor { get; set; } = null!;

    [Required(ErrorMessage = "El carácter es obligatorio.")]
    [RegularExpression(@"^(AlaOrden|NoAlaOrden)$",
        ErrorMessage = "El carácter debe ser 'AlaOrden' o 'NoAlaOrden'.")]
    public string Caracter { get; set; } = null!;

    [Required(ErrorMessage = "El tipo de documento del beneficiario es obligatorio.")]
    [RegularExpression(@"^(CUIT|CUIL|CDI)$",
        ErrorMessage = "El tipo de documento debe ser 'CUIT', 'CUIL' o 'CDI'.")]
    public string TipoDocBeneficiario { get; set; } = null!;

    [Required(ErrorMessage = "El nombre del librador es obligatorio.")]
    [StringLength(120, ErrorMessage = "El nombre del librador debe tener hasta 120 caracteres.")]
    public string NombreLibrador { get; set; } = null!;

    [Required(ErrorMessage = "El nombre del beneficiario es obligatorio.")]
    [StringLength(120, ErrorMessage = "El nombre del beneficiario debe tener hasta 120 caracteres.")]
    public string NombreBeneficiario { get; set; } = null!;

    [StringLength(60, ErrorMessage = "El concepto debe tener hasta 60 caracteres.")]
    public string? Concepto { get; set; }

    [StringLength(280, ErrorMessage = "El motivo debe tener hasta 280 caracteres.")]
    public string? Motivo { get; set; }

    [StringLength(60, ErrorMessage = "La referencia debe tener hasta 60 caracteres.")]
    public string? Referencia { get; set; }

    [StringLength(160, ErrorMessage = "El email debe tener hasta 160 caracteres.")]
    [EmailAddress(ErrorMessage = "El email de notificación debe ser válido.")]
    public string? EmailNotificacion { get; set; }

    [Required(ErrorMessage = "El CUIT/CUIL del librador es obligatorio.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "El CUIT/CUIL del librador debe tener 11 dígitos.")]
    public string CuitLibrador { get; set; } = null!;

    [Required(ErrorMessage = "El CUIT/CUIL del beneficiario es obligatorio.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "El CUIT/CUIL del beneficiario debe tener 11 dígitos.")]
    public string CuitBeneficiario { get; set; } = null!;

    [Required(ErrorMessage = "El monto es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
    public decimal Monto { get; set; }

    [Required(ErrorMessage = "La moneda es obligatoria.")]
    [RegularExpression(@"^[PD]$", ErrorMessage = "La moneda debe ser 'P' (pesos) o 'D' (dólares).")]
    public string Moneda { get; set; } = null!;

    [Required(ErrorMessage = "La fecha de emisión es obligatoria.")]
    public DateOnly FechaEmision { get; set; }

    public DateOnly? FechaDiferimiento { get; set; }

    [Required(ErrorMessage = "La fecha de vencimiento es obligatoria.")]
    public DateOnly FechaVencimiento { get; set; }
}
