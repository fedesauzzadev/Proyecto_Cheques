namespace Coelsa.Api;

/// <summary>Parámetros de rate limiting (RF-08), configurables en appsettings.</summary>
public class RateLimitOpciones
{
    public int ConsultasTokenLimit { get; set; } = 100;
    public int CreacionesTokenLimit { get; set; } = 30;
    public int PeriodoSegundos { get; set; } = 60;
}
