namespace IrmaRios.Cartera.Infrastructure.Persistence;

/// <summary>Registro de idempotencia (puerto IAlmacenIdempotencia, estándar del ecosistema).</summary>
public class EntradaIdempotencia
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string BodyHash { get; set; } = string.Empty;

    public string ResponseJson { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; }
}
