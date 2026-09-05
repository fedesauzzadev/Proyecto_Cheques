namespace Coelsa.Domain.Validaciones;

/// <summary>
/// Máquina de estados compartida por cheques físicos y echeqs (SPEC 5.3):
/// Emitido → Depositado → Compensado → Pagado; Emitido → Anulado; Depositado → Rechazado.
/// </summary>
public static class TransicionesEstado
{
    private static readonly IReadOnlyDictionary<EstadoInstrumento, EstadoInstrumento[]> TransicionesValidas =
        new Dictionary<EstadoInstrumento, EstadoInstrumento[]>
        {
            [EstadoInstrumento.Emitido] = [EstadoInstrumento.Depositado, EstadoInstrumento.Anulado],
            [EstadoInstrumento.Depositado] = [EstadoInstrumento.Compensado, EstadoInstrumento.Rechazado],
            [EstadoInstrumento.Compensado] = [EstadoInstrumento.Pagado],
            [EstadoInstrumento.Rechazado] = [],
            [EstadoInstrumento.Anulado] = [],
            [EstadoInstrumento.Pagado] = []
        };

    public static bool EsValida(EstadoInstrumento desde, EstadoInstrumento hacia)
    {
        return TransicionesValidas.TryGetValue(desde, out var destinos)
            && destinos.Contains(hacia);
    }

    public static IReadOnlyCollection<EstadoInstrumento> DestinosDesde(EstadoInstrumento estado)
    {
        return TransicionesValidas.TryGetValue(estado, out var destinos)
            ? destinos
            : Array.Empty<EstadoInstrumento>();
    }
}
