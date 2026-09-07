using Coelsa.Domain.Validaciones;

namespace Coelsa.Domain.Entidades;

/// <summary>
/// Cuenta corriente habilitada para emitir echeqs (Fase B).
/// El CBU de 22 dígitos identifica banco + sucursal + cuenta; el Guid interno
/// es solo PK de base de datos, la API expone el CBU.
/// </summary>
public class Cuenta
{
    public Guid Id { get; private set; }
    public string Cbu { get; private set; } = null!;
    public string CuitTitular { get; private set; } = null!;
    public string NombreTitular { get; private set; } = null!;
    public Moneda Moneda { get; private set; }
    public bool Activa { get; private set; } = true;
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaModificacion { get; private set; }
    public DateTime? FechaBaja { get; private set; }

    private Cuenta()
    {
    }

    public static Cuenta Crear(string cbu, string cuitTitular, string nombreTitular, Moneda moneda)
    {
        if (!ValidadorCbu.EsValido(cbu))
        {
            throw new ValidacionException(
                "El CBU debe tener 22 dígitos con verificadores válidos (entidad + sucursal + cuenta).");
        }

        if (!ValidadorCuit.EsValido(cuitTitular))
        {
            throw new ValidacionException($"El CUIT/CUIL del titular '{cuitTitular}' no es válido (se espera 11 dígitos con verificador módulo 11).");
        }

        Validaciones.ValidadorDocumento.ValidarNombre(nombreTitular, "nombre del titular");

        return new Cuenta
        {
            Id = Guid.NewGuid(),
            Cbu = cbu!,
            CuitTitular = cuitTitular!,
            NombreTitular = nombreTitular!.Trim(),
            Moneda = moneda,
            Activa = true,
            FechaCreacion = DateTime.UtcNow
        };
    }

    public string Banco => ValidadorCbu.Banco(Cbu);

    public string Sucursal => ValidadorCbu.Sucursal(Cbu);

    public string NumeroCuenta => ValidadorCbu.NumeroCuenta(Cbu);

    public void Desactivar()
    {
        if (!Activa)
        {
            throw new ValidacionException($"La cuenta con CBU {Cbu} ya se encuentra desactivada.");
        }

        Activa = false;
        FechaModificacion = DateTime.UtcNow;
        FechaBaja = DateTime.UtcNow;
    }
}
