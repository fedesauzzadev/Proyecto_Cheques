using Coelsa.Domain;

namespace Coelsa.Infrastructure.Persistence;

/// <summary>
/// Entidad EF del registro de idempotencia (SPEC RF-02). Se persiste en la
/// misma transacción que el instrumento creado, por lo que sobrevive reinicios.
/// </summary>
public class EntradaIdempotencia
{
    public string Key { get; set; } = null!;
    public string BodyHash { get; set; } = null!;
    public string ResponseJson { get; set; } = null!;
    public TipoInstrumento Tipo { get; set; }
    public DateTime FechaCreacion { get; set; }
}
