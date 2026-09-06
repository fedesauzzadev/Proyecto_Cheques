using System.ComponentModel.DataAnnotations;

namespace Coelsa.Application.Dtos;

public class CrearEcheqRequest
{
    [Required(ErrorMessage = "El CMC7 es obligatorio.")]
    [RegularExpression(@"^\d{30}$",
        ErrorMessage = "El CMC7 debe ser un código magnetizable de 30 dígitos (banco + sucursal + código postal + número de cheque + cuenta).")]
    public string Cmc7 { get; set; } = null!;

    [Required(ErrorMessage = "El código de banco es obligatorio.")]
    [RegularExpression(@"^\d{3}$", ErrorMessage = "El código de banco debe ser numérico de 3 dígitos (código BCRA).")]
    public string CodigoBanco { get; set; } = null!;

    [Required(ErrorMessage = "El número de cuenta es obligatorio.")]
    [RegularExpression(@"^\d{12}$", ErrorMessage = "El número de cuenta debe ser numérico de 12 dígitos.")]
    public string NumeroCuenta { get; set; } = null!;

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
}
