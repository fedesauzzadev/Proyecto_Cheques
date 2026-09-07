namespace Coelsa.Domain.Validaciones;

/// <summary>
/// Valida el documento del beneficiario/titular según su tipo (Fase B3):
/// CUIT, CUIL y CDI son 11 dígitos con verificador módulo 11.
/// </summary>
public static class ValidadorDocumento
{
    public const int Longitud = 11;
    public const int LongitudNombre = 120;

    public static bool EsValido(TipoDocumento tipo, string? numero)
    {
        return tipo is TipoDocumento.Cuit or TipoDocumento.Cuil or TipoDocumento.Cdi
            && ValidadorCuit.EsValido(numero);
    }

    public static bool EsValido(string? tipoCodigo, string? numero)
    {
        return CodigoATipo(tipoCodigo) is not null && EsValido(CodigoATipo(tipoCodigo)!.Value, numero);
    }

    public static TipoDocumento? CodigoATipo(string? codigo) => codigo switch
    {
        "CUIT" => TipoDocumento.Cuit,
        "CUIL" => TipoDocumento.Cuil,
        "CDI" => TipoDocumento.Cdi,
        _ => null
    };

    public static string TipoACodigo(TipoDocumento tipo) => tipo switch
    {
        TipoDocumento.Cuil => "CUIL",
        TipoDocumento.Cdi => "CDI",
        _ => "CUIT"
    };

    public static void ValidarNombre(string? nombre, string campo)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > LongitudNombre)
        {
            throw new ValidacionException(
                $"El campo '{campo}' es obligatorio y debe tener hasta {LongitudNombre} caracteres.");
        }
    }
}
