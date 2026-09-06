using System.Text.Json;

namespace IrmaRios.Identidad.Application.Common;

/// <summary>Reglas de paginación compartidas por las consultas del servicio.</summary>
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
            throw new Domain.ValidacionException("El parámetro 'page' debe ser mayor o igual a 1.");
        }

        if (tamanio < 1 || tamanio > PageSizeMaximo)
        {
            throw new Domain.ValidacionException($"El parámetro 'pageSize' debe estar entre 1 y {PageSizeMaximo}.");
        }

        return (pagina, tamanio);
    }
}

/// <summary>Hash SHA-256 del body para detectar reuso de una Idempotency-Key con otro payload.</summary>
public static class Hasher
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public static string DelObjeto<T>(T valor)
        => Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(
               JsonSerializer.SerializeToUtf8Bytes(valor, JsonOpts)));
}

/// <summary>Resultado idempotente: la respuesta original o un replay marcado.</summary>
public sealed record ResultadoIdempotente<T>(T Respuesta, bool EsReplay);
