using System.ComponentModel.DataAnnotations;

namespace Coelsa.Application.Dtos;

public class CambiarEstadoRequest
{
    [Required(ErrorMessage = "El estado es obligatorio.")]
    public string Estado { get; set; } = null!;

    [Range(11, 25, ErrorMessage = "El motivo de rechazo debe ser un código válido (11, 12, 21 o 25).")]
    public int? MotivoRechazo { get; set; }
}
