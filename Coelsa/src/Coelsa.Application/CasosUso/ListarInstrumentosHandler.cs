using Coelsa.Application.Common;
using Coelsa.Application.Dtos;
using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Entidades;
using Coelsa.Domain.Validaciones;

namespace Coelsa.Application.CasosUso;

/// <summary>
/// Consulta por CUIT/CUIL separada por tipo, paginada y cacheada (SPEC RF-03).
/// La clave de cache incorpora la versión del CUIT (invalidación por escritura)
/// y la página solicitada; el TTL y la protección contra stampede los resuelve el adaptador.
/// </summary>
public abstract class ListarInstrumentosHandler<TEntidad, TResponse>(
    IInstrumentoRepository<TEntidad> repository,
    IGestorCacheConsultas cache)
    where TEntidad : class, IInstrumento
    where TResponse : class
{
    private readonly IInstrumentoRepository<TEntidad> _repository = repository;
    private readonly IGestorCacheConsultas _cache = cache;

    protected abstract TipoInstrumento Tipo { get; }

    protected abstract string PrefijoClaveCache { get; }

    protected abstract TResponse Mapear(TEntidad entidad);

    public async Task<PagedResponse<TResponse>> Ejecutar(string? cuit, int? page, int? pageSize, CancellationToken ct)
    {
        if (!ValidadorCuit.EsValido(cuit))
        {
            throw new ValidacionException(
                "El parámetro 'cuit' es obligatorio y debe ser un CUIT/CUIL válido de 11 dígitos.");
        }

        var (pagina, tamanio) = Paginacion.Normalizar(page, pageSize);

        var version = await _cache.ObtenerVersionAsync(Tipo, cuit!, ct);
        var clave = $"{PrefijoClaveCache}:cuit:{cuit}:v{version}:p{pagina}:{tamanio}";

        return await _cache.ObtenerOCargarAsync(clave, async token =>
        {
            var (items, totalCount) = await _repository.ListarPorCuitAsync(cuit!, pagina, tamanio, token);

            return new PagedResponse<TResponse>
            {
                Items = items.Select(Mapear).ToList(),
                Page = pagina,
                PageSize = tamanio,
                TotalCount = totalCount
            };
        }, ct);
    }
}

public sealed class ListarChequesHandler(
    IInstrumentoRepository<ChequeFisico> repository,
    IGestorCacheConsultas cache)
    : ListarInstrumentosHandler<ChequeFisico, ChequeResponse>(repository, cache)
{
    protected override TipoInstrumento Tipo => TipoInstrumento.ChequeFisico;
    protected override string PrefijoClaveCache => "coelsa:cheques";
    protected override ChequeResponse Mapear(ChequeFisico entidad) => entidad.AResponse();
}
