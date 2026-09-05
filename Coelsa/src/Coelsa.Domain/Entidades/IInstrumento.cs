namespace Coelsa.Domain.Entidades;

/// <summary>
/// Contrato común de los instrumentos negociables (cheque físico y echeq)
/// usado por los casos de uso genéricos de la capa Application.
/// </summary>
public interface IInstrumento
{
    string CuitLibrador { get; }
    string CuitBeneficiario { get; }
    EstadoInstrumento Estado { get; }
    bool Activo { get; }

    /// <summary>Baja lógica del instrumento.</summary>
    void Eliminar();
}
