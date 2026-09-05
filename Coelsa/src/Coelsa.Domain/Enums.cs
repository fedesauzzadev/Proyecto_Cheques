namespace Coelsa.Domain;

public enum EstadoInstrumento
{
    Emitido = 1,
    Depositado = 2,
    Compensado = 3,
    Rechazado = 4,
    Anulado = 5,
    Pagado = 6
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
