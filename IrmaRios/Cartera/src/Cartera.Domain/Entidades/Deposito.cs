using IrmaRios.Cartera.Domain;

namespace IrmaRios.Cartera.Domain.Entidades;

/// <summary>Depósito de una tanda de instrumentos a cartera de una empresa (HU-02).</summary>
public sealed class Deposito
{
    private readonly List<DepositoDetalle> _detalles = [];

    public Guid Id { get; private set; }

    public string CuitEmpresa { get; private set; } = string.Empty;

    public DateTime FechaUtc { get; private set; }

    public IReadOnlyList<DepositoDetalle> Detalles => _detalles;

    private Deposito() { }

    public static Deposito Registrar(string cuitEmpresa, DateTime ahoraUtc)
        => new() { Id = Guid.NewGuid(), CuitEmpresa = cuitEmpresa, FechaUtc = ahoraUtc };

    public void AgregarInstrumento(Guid instrumentoCarteraId)
    {
        if (_detalles.Any(d => d.InstrumentoCarteraId == instrumentoCarteraId))
        {
            throw new ValidacionException("El instrumento ya está en el depósito.");
        }

        _detalles.Add(new DepositoDetalle { DepositoId = Id, InstrumentoCarteraId = instrumentoCarteraId });
    }
}

public class DepositoDetalle
{
    public Guid DepositoId { get; set; }

    public Guid InstrumentoCarteraId { get; set; }
}
