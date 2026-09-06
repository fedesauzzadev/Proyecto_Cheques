namespace Coelsa.Domain;

public enum EstadoInstrumento
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

public enum MotivoRechazo
{
    FaltaDeFondos = 11,
    CuentaInexistenteOEmbargada = 12,
    DefectoFormal = 21,
    ChequeAdulteradoOFalsificado = 25
}

public enum Moneda
{
    Pesos = 1,
    Dolares = 2
}

public enum TipoInstrumento
{
    ChequeFisico = 1,
    Echeq = 2
}

/// <summary>
/// Ciclo de vida de un endoso de echeq (SPEC Fase A): se propone, el endosatario
/// lo admite o repudia; el endosante puede anularlo mientras está propuesto;
/// una devolución aceptada revierte los endosos posteriores al solicitante.
/// </summary>
public enum EstadoEndoso
{
    Propuesto = 1,
    Vigente = 2,
    Repudiado = 3,
    Anulado = 4,
    Revertido = 5
}

/// <summary>
/// Ciclo de vida de un pedido de devolución de echeq (SPEC Fase A).
/// </summary>
public enum EstadoDevolucion
{
    Solicitada = 1,
    Aceptada = 2,
    Rechazada = 3,
    Anulada = 4
}
