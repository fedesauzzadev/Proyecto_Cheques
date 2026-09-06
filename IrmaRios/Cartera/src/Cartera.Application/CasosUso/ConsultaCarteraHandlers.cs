using IrmaRios.Cartera.Application.Common;
using IrmaRios.Cartera.Application.Dtos;
using IrmaRios.Cartera.Application.Puertos;
using IrmaRios.Cartera.Domain;
using IrmaRios.Cartera.Domain.Validaciones;

namespace IrmaRios.Cartera.Application.CasosUso;

/// <summary>
/// HU-03: cartera de la empresa separada por tipo, paginada y cacheada con
/// invalidación por versión de CUIT (mismo esquema que RFC-001/ADR-004 del ecosistema).
/// </summary>
public sealed class ListarCarteraHandler(ICarteraRepository cartera, IGestorCacheConsultas cache)
{
    public async Task<PagedResponse<InstrumentoCarteraResponse>> Ejecutar(
        string tipoTexto, string cuit, int? page, int? pageSize, CancellationToken ct)
    {
        if (!ValidadorCuit.EsValido(cuit))
        {
            throw new ValidacionException("El CUIT de la empresa es inválido.");
        }

        if (!Enum.TryParse<TipoInstrumento>(tipoTexto, ignoreCase: true, out var tipo))
        {
            throw new ValidacionException(
                $"El tipo '{tipoTexto}' no es válido. Válidos: {string.Join(", ", Enum.GetNames<TipoInstrumento>())}.");
        }

        var (pagina, tamanio) = Paginacion.Normalizar(page, pageSize);

        var version = await cache.ObtenerVersionAsync(tipo, cuit, ct);
        var clave = $"irmarios:cartera:{tipo}:cuit:{cuit}:v{version}:p{pagina}:{tamanio}";

        return await cache.ObtenerOCargarAsync(clave, async token =>
        {
            var (items, totalCount) = await cartera.ListarPorTitularAsync(tipo, cuit, pagina, tamanio, token);

            return new PagedResponse<InstrumentoCarteraResponse>
            {
                Items = items.Select(Mapear).ToList(),
                Page = pagina,
                PageSize = tamanio,
                TotalCount = totalCount
            };
        }, ct);
    }

    private static InstrumentoCarteraResponse Mapear(Domain.Entidades.InstrumentoCartera i)
        => new(i.Tipo.ToString(), i.Identificador, i.CuitLibrador, i.CuitBeneficiario, i.TitularCuit,
            i.Monto, i.Moneda.ToString(), i.FechaVencimiento.ToString("yyyy-MM-dd"),
            i.EstadoClearing.ToString(), i.Estado.ToString());
}

/// <summary>Detalle de un instrumento de la cartera por su identificador (HU-03).</summary>
public sealed class ObtenerInstrumentoHandler(ICarteraRepository cartera)
{
    public async Task<InstrumentoCarteraResponse> Ejecutar(string identificador, CancellationToken ct)
    {
        var instrumento = await cartera.ObtenerPorIdentificadorAsync(
            identificador.Trim().ToUpperInvariant(), ct)
            ?? throw new NoEncontradoException($"No existe el instrumento {identificador} en la cartera del banco.");

        return new InstrumentoCarteraResponse(
            instrumento.Tipo.ToString(), instrumento.Identificador, instrumento.CuitLibrador,
            instrumento.CuitBeneficiario, instrumento.TitularCuit, instrumento.Monto,
            instrumento.Moneda.ToString(), instrumento.FechaVencimiento.ToString("yyyy-MM-dd"),
            instrumento.EstadoClearing.ToString(), instrumento.Estado.ToString());
    }
}
