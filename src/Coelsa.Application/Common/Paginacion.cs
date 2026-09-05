using Coelsa.Domain;

namespace Coelsa.Application.Common;

/// <summary>Reglas de paginación de las consultas por CUIT (SPEC RF-03).</summary>
public static class Paginacion
{
    public const int PageDefault = 1;
    public const int PageSizeDefault = 10;
    public const int PageSizeMaximo = 100;

    public static (int Page, int PageSize) Normalizar(int? page, int? pageSize)
    {
        var pagina = page ?? PageDefault;
        var tamanio = pageSize ?? PageSizeDefault;

        if (pagina < 1)
        {
            throw new ValidacionException("El parámetro 'page' debe ser mayor o igual a 1.");
        }

        if (tamanio < 1 || tamanio > PageSizeMaximo)
        {
            throw new ValidacionException($"El parámetro 'pageSize' debe estar entre 1 y {PageSizeMaximo}.");
        }

        return (pagina, tamanio);
    }
}
