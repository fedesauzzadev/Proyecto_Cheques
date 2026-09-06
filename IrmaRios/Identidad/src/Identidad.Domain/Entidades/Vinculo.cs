using IrmaRios.Identidad.Domain;

namespace IrmaRios.Identidad.Domain.Entidades;

/// <summary>
/// Asociación persona ↔ empresa con rol y vigencia (HU-01: una persona puede
/// estar autorizada en varias empresas, y una empresa tiene varias personas).
/// </summary>
public sealed class Vinculo
{
    public Guid Id { get; private set; }

    public Guid PersonaId { get; private set; }

    public Guid EmpresaId { get; private set; }

    public RolVinculo Rol { get; private set; }

    public DateTime DesdeUtc { get; private set; }

    public DateTime? HastaUtc { get; private set; }

    private Vinculo() { }

    public static Vinculo Vincular(Guid personaId, Guid empresaId, RolVinculo rol, DateTime ahoraUtc)
    {
        if (!Enum.IsDefined(rol))
        {
            throw new ValidacionException("El rol del vínculo no es válido.");
        }

        return new Vinculo
        {
            Id = Guid.NewGuid(),
            PersonaId = personaId,
            EmpresaId = empresaId,
            Rol = rol,
            DesdeUtc = ahoraUtc
        };
    }

    public bool EsVigente(DateTime ahoraUtc) => HastaUtc is null || HastaUtc > ahoraUtc;

    /// <summary>Mismo vínculo activo (persona + empresa + rol) que otro.</summary>
    public bool EsDuplicadoDe(Vinculo otro)
        => PersonaId == otro.PersonaId && EmpresaId == otro.EmpresaId && Rol == otro.Rol
           && EsVigente(DateTime.UtcNow) && otro.EsVigente(DateTime.UtcNow);

    public void Cerrar(DateTime hastaUtc)
    {
        if (hastaUtc < DesdeUtc)
        {
            throw new ValidacionException("La fecha de cierre no puede ser anterior al alta del vínculo.");
        }

        HastaUtc = hastaUtc;
    }
}
