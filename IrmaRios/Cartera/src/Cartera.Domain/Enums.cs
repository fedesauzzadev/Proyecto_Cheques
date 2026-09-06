namespace IrmaRios.Cartera.Domain;

/// <summary>Tipo de instrumento; cheques físicos y echeqs nunca se mezclan (HU-02/03).</summary>
public enum TipoInstrumento
{
    ChequeFisico = 1,
    Echeq = 2
}

/// <summary>
/// Espejo de los estados del clearing (simulador COELSA). Se guarda como string
/// para tolerar estados nuevos sin romper el espejo.
/// </summary>
public enum EstadoClearing
{
    Pendiente = 0,
    Emitido = 1,
    Depositado = 2,
    Compensado = 3,
    Rechazado = 4,
    Anulado = 5,
    Pagado = 6,
    Repudiado = 7,
    EnCustodia = 8
}

/// <summary>Ciclo de vida del instrumento dentro del banco (el slice usa EnCartera).</summary>
public enum EstadoCartera
{
    EnCartera = 1,
    Negociado = 2,
    Endosado = 3
}

public enum Moneda
{
    Pesos = 1,
    Dolares = 2
}
