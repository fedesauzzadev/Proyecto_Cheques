using Coelsa.Application.Common;
using Coelsa.Application.Dtos;
using Coelsa.Application.Puertos;
using Coelsa.Domain;
using Coelsa.Domain.Validaciones;

namespace Coelsa.Application.CasosUso;

/// <summary>
/// Consulta de echeqs por CUIT con filtros opcionales (Fase B6, espejo de la
/// consulta BEE): CBU emisor, estado y rangos de emisión/vencimiento de hasta
/// 360 días más número de cheque. Paginada y cacheada (SPEC RF-03, sección 7).
/// </summary>
public sealed class ListarEcheqsHandler(
    IEcheqRepository repository,
    IGestorCacheConsultas cache)
{
    public async Task<PagedResponse<EcheqResponse>> Ejecutar(
        string? cuit,
        FiltrosEcheq filtros,
        int? page,
        int? pageSize,
        CancellationToken ct)
    {
        if (!ValidadorCuit.EsValido(cuit))
        {
            throw new ValidacionException(
                "El parámetro 'cuit' es obligatorio y debe ser un CUIT/CUIL válido de 11 dígitos.");
        }

        var validados = filtros.Validado();
        var (pagina, tamanio) = Paginacion.Normalizar(page, pageSize);

        var version = await cache.ObtenerVersionAsync(TipoInstrumento.Echeq, cuit!, ct);
        var clave = $"coelsa:echeqs:cuit:{cuit}:v{version}:p{pagina}:{tamanio}:f{validados.ClaveCache()}";

        return await cache.ObtenerOCargarAsync(clave, async token =>
        {
            var (items, totalCount) = await repository.ListarFiltradoAsync(cuit!, validados, pagina, tamanio, token);

            return new PagedResponse<EcheqResponse>
            {
                Items = items.Select(e => e.AResponse()).ToList(),
                Page = pagina,
                PageSize = tamanio,
                TotalCount = totalCount
            };
        }, ct);
    }
}
