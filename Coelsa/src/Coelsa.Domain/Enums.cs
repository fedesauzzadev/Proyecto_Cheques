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
    EnCustodia = 8,
    /// <summary>Vencido el plazo de presentación (30 días). Terminal.</summary>
    Caducado = 9
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
/// Tipo de documento del titular/beneficiario (Fase B3): CUIT, CUIL o CDI.
/// Todos son 11 dígitos con verificador módulo 11.
/// </summary>
public enum TipoDocumento
{
    Cuit = 1,
    Cuil = 2,
    Cdi = 3
}

/// <summary>
/// Carácter del echeq (Fase B): solo los "a la orden" se endosan; los "no a la
/// orden" se transmiten por cesión (Fase D). Todos son cruzados (solo depósito).
/// </summary>
public enum Caracter
{
    AlaOrden = 1,
    NoAlaOrden = 2
}

/// <summary>
/// Ciclo de vida de una e-chequera (Fase B): se solicita y el simulador la
/// habilita de inmediato (en la realidad son 24h); se agota al reservar el
/// último número. Una cuenta puede tener varias chequeras vigentes.
/// </summary>
public enum EstadoChequera
{
    Vigente = 1,
    Agotada = 2
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

/// <summary>
/// Ciclo de vida de una cesión de echeq "no a la orden" (Fase D1): el tenedor
/// la propone, el cesionario la acepta o rechaza; el cedente puede anularla
/// mientras está solicitada. Tope de 10 cesiones por echeq (regla BCRA).
/// </summary>
public enum EstadoCesion
{
    Solicitada = 1,
    Aceptada = 2,
    Rechazada = 3,
    Anulada = 4
}
