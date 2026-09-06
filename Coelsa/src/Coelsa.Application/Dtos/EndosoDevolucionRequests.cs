using System.ComponentModel.DataAnnotations;

namespace Coelsa.Application.Dtos;

public class AceptarEcheqRequest
{
    /// <summary>true = acepta (pasa a Emitido); false = repudia (terminal).</summary>
    public bool Aceptada { get; set; }
}

public class ProponerEndosoRequest
{
    [Required(ErrorMessage = "El CUIT/CUIL del endosatario es obligatorio.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "El CUIT/CUIL del endosatario debe tener 11 dígitos.")]
    public string CuitEndosatario { get; set; } = null!;
}

public class ResolverEndosoRequest
{
    /// <summary>true = admite (entra en vigencia); false = repudia.</summary>
    public bool Admitido { get; set; }

    [Required(ErrorMessage = "El CUIT de quien resuelve es obligatorio.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "El CUIT de quien resuelve debe tener 11 dígitos.")]
    public string Cuit { get; set; } = null!;
}

public class SolicitarDevolucionRequest
{
    [Required(ErrorMessage = "El CUIT/CUIL del solicitante es obligatorio.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "El CUIT/CUIL del solicitante debe tener 11 dígitos.")]
    public string CuitSolicitante { get; set; } = null!;

    [MaxLength(280, ErrorMessage = "El motivo no puede superar los 280 caracteres.")]
    public string? Motivo { get; set; }
}

public class ResolverDevolucionRequest
{
    /// <summary>true = acepta (la tenencia vuelve al solicitante); false = rechaza.</summary>
    public bool Aceptada { get; set; }

    [Required(ErrorMessage = "El CUIT de quien resuelve es obligatorio.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "El CUIT de quien resuelve debe tener 11 dígitos.")]
    public string CuitResolutor { get; set; } = null!;
}
