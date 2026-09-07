// Espejo de Coelsa.Domain/Enums.cs y Coelsa.Application/Dtos/Responses.cs.
// Fuente de verdad del contrato: Coelsa/docs/openapi.yaml del backend.

export const ESTADOS_INSTRUMENTO = [
  'Pendiente',
  'Emitido',
  'Depositado',
  'Compensado',
  'Rechazado',
  'Anulado',
  'Pagado',
  'Repudiado',
  'EnCustodia',
  'Caducado',
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

/** Carácter del echeq (Fase B): solo los 'A la orden' se endosan. */
export type Caracter = 'AlaOrden' | 'NoAlaOrden';

/** Tipo de documento del beneficiario (Fase B3): 11 dígitos con verificador. */
export type TipoDocumento = 'CUIT' | 'CUIL' | 'CDI';

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
  cbuEmisor: string;
  numeroChequera: number;
  numeroCheque: number;
  caracter: Caracter;
  /** Siempre 'Cruzado': el echeq solo se deposita en cuenta. */
  modo: 'Cruzado';
  tipoDocBeneficiario: TipoDocumento;
  nombreLibrador: string;
  nombreBeneficiario: string;
  concepto: string | null;
  motivo: string | null;
  referencia: string | null;
  emailNotificacion: string | null;
  motivoRepudio: string | null;
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
  // Nota: CMC7 e IDECHEQ los genera la API al crear (operatoria real).
  cbuEmisor: string;
  caracter: Caracter;
  tipoDocBeneficiario: TipoDocumento;
  nombreLibrador: string;
  nombreBeneficiario: string;
  concepto?: string | null;
  motivo?: string | null;
  referencia?: string | null;
  emailNotificacion?: string | null;
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

// Cuentas corrientes emisoras y e-chequeras (Fase B, espejo del back).
export interface CrearCuentaRequest {
  cbu: string;
  cuitTitular: string;
  nombreTitular: string;
  moneda: Moneda;
}

export interface Cuenta {
  /** CBU de 22 dígitos: identificador de negocio de la cuenta. */
  cbu: string;
  banco: string;
  sucursal: string;
  numeroCuenta: string;
  cuitTitular: string;
  nombreTitular: string;
  moneda: Moneda;
  fechaCreacion: string;
}

export type EstadoChequera = 'Vigente' | 'Agotada';

export interface Chequera {
  numero: number;
  cantidadTotal: number;
  proximoNumero: number;
  disponibles: number;
  estado: EstadoChequera;
  fechaSolicitud: string;
  fechaHabilitacion: string;
}

/** Resultado del padrón simulado de titulares ("lupa", Fase B3). */
export interface Titular {
  tipoDoc: TipoDocumento;
  numero: string;
  nombre: string | null;
  bancarizado: boolean;
}

/** Certificado para acciones civiles (Fase D3): CUD determinista, no se persiste. */
export interface Certificado {
  cud: string;
  codigoVisualizacion: string;
  idEcheq: string;
  cmc7: string;
  estado: 'Rechazado';
  motivoRechazo: MotivoRechazo | null;
  cuitLibrador: string;
  nombreLibrador: string;
  cuitBeneficiario: string;
  nombreBeneficiario: string;
  monto: number;
  moneda: Moneda;
  fechaEmision: string;
  fechaVencimiento: string;
  fechaRechazo: string | null;
}

export const ESTADOS_ENDOSO = [
  'Propuesto',
  'Vigente',
  'Repudiado',
  'Anulado',
  'Revertido',
] as const;
export type EstadoEndoso = (typeof ESTADOS_ENDOSO)[number];

export const ESTADOS_DEVOLUCION = ['Solicitada', 'Aceptada', 'Rechazada', 'Anulada'] as const;
export type EstadoDevolucion = (typeof ESTADOS_DEVOLUCION)[number];

export const ESTADOS_CESION = ['Solicitada', 'Aceptada', 'Rechazada', 'Anulada'] as const;
export type EstadoCesion = (typeof ESTADOS_CESION)[number];

export interface Endoso {
  orden: number;
  cuitEndosante: string;
  cuitEndosatario: string;
  estado: EstadoEndoso;
  fechaCreacion: string;
}

export interface Devolucion {
  numero: number;
  cuitSolicitante: string;
  motivo: string | null;
  estado: EstadoDevolucion;
  fechaCreacion: string;
}

/** Cesión de un echeq "no a la orden" a un tercero (Fase D1). */
export interface Cesion {
  numero: number;
  cuitCedente: string;
  cuitCesionario: string;
  domicilioCesionario: string;
  estado: EstadoCesion;
  fechaCreacion: string;
}
