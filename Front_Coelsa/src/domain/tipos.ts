// Espejo de Coelsa.Domain/Enums.cs y Coelsa.Application/Dtos/Responses.cs.
// Fuente de verdad del contrato: Coelsa/docs/openapi.yaml del backend.

export const ESTADOS_INSTRUMENTO = [
  'Emitido',
  'Depositado',
  'Compensado',
  'Rechazado',
  'Anulado',
  'Pagado',
] as const;
export type EstadoInstrumento = (typeof ESTADOS_INSTRUMENTO)[number];

// Códigos de rechazo estandarizados (SPEC 5.4 del backend).
export const MOTIVOS_RECHAZO = {
  FaltaDeFondos: 11,
  CuentaInexistenteOEmbargada: 12,
  DefectoFormal: 21,
  ChequeAdulteradoOFalsificado: 25,
} as const;
export type MotivoRechazo = (typeof MOTIVOS_RECHAZO)[keyof typeof MOTIVOS_RECHAZO];

export const ETIQUETAS_MOTIVO_RECHAZO: Readonly<Record<MotivoRechazo, string>> = {
  11: 'Falta de fondos',
  12: 'Cuenta inexistente / embargada',
  21: 'Defecto formal',
  25: 'Cheque adulterado o falsificado',
};

export type Moneda = 'P' | 'D';

export interface DesgloseCmc7 {
  banco: string;
  sucursal: string;
  codigoPostal: string;
  numeroCheque: string;
  numeroCuenta: string;
}

export interface InstrumentoBase {
  /** CMC7 (cheque físico) o IDECHEQ (echeq): identificador de negocio. */
  identificador: string;
  estado: EstadoInstrumento;
  motivoRechazo: MotivoRechazo | null;
  cuitLibrador: string;
  cuitBeneficiario: string;
  monto: number;
  moneda: Moneda;
  fechaEmision: string; // YYYY-MM-DD
  fechaDiferimiento: string | null;
  fechaVencimiento: string; // YYYY-MM-DD, posterior a emisión (y a diferimiento si viene)
  fechaCreacion: string; // ISO-8601
}

export interface ChequeResponse extends InstrumentoBase {
  tipo: 'ChequeFisico';
  desgloseCmc7: DesgloseCmc7;
}

export interface EcheqResponse extends InstrumentoBase {
  tipo: 'Echeq';
  cmc7: string;
  desgloseCmc7: DesgloseCmc7;
  cantidadEndosos: number;
}

export type Instrumento = ChequeResponse | EcheqResponse;

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// Requests de creación (SPEC sección 6 del backend).
export interface CrearChequeFisicoRequest {
  cmc7: string;
  cuitLibrador: string;
  cuitBeneficiario: string;
  monto: number;
  moneda: Moneda;
  fechaEmision: string;
  fechaDiferimiento?: string | null;
  fechaVencimiento: string;
}

export interface CrearEcheqRequest {
  cmc7: string;
  cuitLibrador: string;
  cuitBeneficiario: string;
  monto: number;
  moneda: Moneda;
  fechaEmision: string;
  fechaDiferimiento?: string | null;
  fechaVencimiento: string;
}

export interface CambiarEstadoRequest {
  estado: EstadoInstrumento;
  motivoRechazo?: MotivoRechazo | null;
}
