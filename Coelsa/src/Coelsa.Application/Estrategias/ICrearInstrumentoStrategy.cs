namespace Coelsa.Application.Estrategias;

/// <summary>
/// Estrategia de creación de instrumentos (SPEC RF-01): cada tipo de instrumento
/// define sus validaciones, chequeos de unicidad y persistencia.
/// </summary>
public interface ICrearInstrumentoStrategy<in TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    Task<ResultadoCreacion<TResponse>> CrearAsync(TRequest request, string idempotencyKey, CancellationToken ct);
}

/// <summary>Respuesta de una creación: recurso + indicador de replay idempotente.</summary>
public sealed record ResultadoCreacion<TResponse>(TResponse Respuesta, bool EsReplay);
