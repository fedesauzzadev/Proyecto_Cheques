namespace Coelsa.Domain.Validaciones;

/// <summary>
/// Máquina de estados compartida por cheques físicos y echeqs (SPEC 5.3 + Fase A):
/// Emitido → Depositado → Compensado → Pagado; Emitido → Anulado; Depositado → Rechazado.
/// Solo echeqs: nacen en Pendiente (→ Emitido por aceptación, → Repudiado por repudio,
/// → Anulado pre-aceptación) y pueden pasar por EnCustodia (Emitido ⇄ EnCustodia,
/// EnCustodia → Depositado por débito automático al vencer). Los cheques físicos
/// nacen en Emitido y nunca alcanzan los estados exclusivos del echeq.
/// </summary>
public static class TransicionesEstado
{
    private static readonly IReadOnlyDictionary<EstadoInstrumento, EstadoInstrumento[]> TransicionesValidas =
        new Dictionary<EstadoInstrumento, EstadoInstrumento[]>
        {
            [EstadoInstrumento.Pendiente] = [EstadoInstrumento.Emitido, EstadoInstrumento.Repudiado, EstadoInstrumento.Anulado],
            [EstadoInstrumento.Emitido] = [EstadoInstrumento.Depositado, EstadoInstrumento.Anulado, EstadoInstrumento.EnCustodia],
            [EstadoInstrumento.Depositado] = [EstadoInstrumento.Compensado, EstadoInstrumento.Rechazado],
            [EstadoInstrumento.Compensado] = [EstadoInstrumento.Pagado],
            [EstadoInstrumento.EnCustodia] = [EstadoInstrumento.Emitido, EstadoInstrumento.Depositado],
            [EstadoInstrumento.Rechazado] = [],
            [EstadoInstrumento.Anulado] = [],
            [EstadoInstrumento.Pagado] = [],
            [EstadoInstrumento.Repudiado] = []
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
