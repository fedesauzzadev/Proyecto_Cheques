// Puertos de la aplicación (espejo de Coelsa.Application/Puertos/Puertos.cs).
// Los hooks consumen estas interfaces; la infraestructura las implementa.
import type {
  CambiarEstadoRequest,
  ChequeResponse,
  Chequera,
  CrearChequeFisicoRequest,
  CrearCuentaRequest,
  CrearEcheqRequest,
  Cuenta,
  Devolucion,
  EcheqResponse,
  Endoso,
  Cesion,
  Certificado,
  PagedResponse,
  TipoDocumento,
  Titular,
} from '@/domain/tipos';
import type { FiltrosEcheq } from '@/domain/filtrosEcheq';

export interface ConsultaPaginada {
  cuit: string;
  page: number;
  pageSize: number;
  /** Solo echeqs (Fase B6): CBU, estado, rangos y número. */
  filtrosEcheq?: FiltrosEcheq;
}

export interface ResultadoCreacion<T> {
  respuesta: T;
  /** true si la API respondió una creación previa con la misma Idempotency-Key (RF-02). */
  esReplay: boolean;
}

/** Puerto de instrumentos: cheques físicos y echeqs, jamás mezclados (RF-03). */
export interface IPuertoInstrumentos {
  listarCheques(
    filtros: ConsultaPaginada,
    senal?: AbortSignal,
  ): Promise<PagedResponse<ChequeResponse>>;
  listarEcheqs(
    filtros: ConsultaPaginada,
    senal?: AbortSignal,
  ): Promise<PagedResponse<EcheqResponse>>;
  obtenerCheque(cmc7: string, senal?: AbortSignal): Promise<ChequeResponse>;
  obtenerEcheq(idecheq: string, senal?: AbortSignal): Promise<EcheqResponse>;
  crearCheque(
    request: CrearChequeFisicoRequest,
    idempotencyKey: string,
  ): Promise<ResultadoCreacion<ChequeResponse>>;
  crearEcheq(
    request: CrearEcheqRequest,
    idempotencyKey: string,
  ): Promise<ResultadoCreacion<EcheqResponse>>;
  cambiarEstadoCheque(cmc7: string, request: CambiarEstadoRequest): Promise<ChequeResponse>;
  cambiarEstadoEcheq(idecheq: string, request: CambiarEstadoRequest): Promise<EcheqResponse>;
  eliminarCheque(cmc7: string): Promise<void>;
  eliminarEcheq(idecheq: string): Promise<void>;
}

export type EstadoComponente = 'Healthy' | 'Unhealthy';

export interface EstadoSalud {
  status: 'Healthy' | 'Degraded' | 'Unhealthy';
  checks: { postgres: EstadoComponente; redis: EstadoComponente };
}

/** Puerto de salud del backend (RF-07 ↔ RF-F08). */
export interface IPuertoSalud {
  consultar(senal?: AbortSignal): Promise<EstadoSalud>;
}

/** Puerto de cuentas corrientes y e-chequeras (Fase B, espejo del back). */
export interface IPuertoCuentas {
  crear(request: CrearCuentaRequest, idempotencyKey: string): Promise<ResultadoCreacion<Cuenta>>;
  listarPorCuit(cuit: string, senal?: AbortSignal): Promise<Cuenta[]>;
  obtener(cbu: string, senal?: AbortSignal): Promise<Cuenta>;
  solicitarChequera(cbu: string, idempotencyKey: string): Promise<ResultadoCreacion<Chequera>>;
  listarChequeras(cbu: string, senal?: AbortSignal): Promise<Chequera[]>;
  /** Padrón simulado ("lupa"): valida el documento y devuelve el nombre si tiene cuenta. */
  buscarTitular(tipoDoc: TipoDocumento, numero: string, senal?: AbortSignal): Promise<Titular>;
}

/** Puerto de aceptación de echeqs pendientes (RF-F10, espejo de RF-09). */
export interface IPuertoAceptacion {
  aceptar(idecheq: string, aceptada: boolean, motivo?: string | null): Promise<EcheqResponse>;
}

/** Puerto del certificado para acciones civiles (Fase D3, espejo de RF-14). */
export interface IPuertoCertificado {
  obtener(idecheq: string, senal?: AbortSignal): Promise<Certificado>;
}

/** Puerto de endosos de echeqs (RF-F11, espejo de RF-10). */
export interface IPuertoEndosos {
  listar(idecheq: string, senal?: AbortSignal): Promise<Endoso[]>;
  proponer(idecheq: string, cuitEndosatario: string): Promise<Endoso>;
  resolver(idecheq: string, orden: number, admitido: boolean, cuit: string): Promise<Endoso>;
  anular(idecheq: string, orden: number): Promise<void>;
}

/** Puerto de pedidos de devolución de echeqs (RF-F12, espejo de RF-12). */
export interface IPuertoDevoluciones {
  listar(idecheq: string, senal?: AbortSignal): Promise<Devolucion[]>;
  solicitar(idecheq: string, cuitSolicitante: string, motivo?: string | null): Promise<Devolucion>;
  resolver(
    idecheq: string,
    numero: number,
    aceptada: boolean,
    cuitResolutor: string,
  ): Promise<Devolucion>;
  anular(idecheq: string, numero: number): Promise<void>;
}

/** Puerto de cesiones de echeqs "no a la orden" (Fase D1, espejo de RF-13). */
export interface IPuertoCesiones {
  listar(idecheq: string, senal?: AbortSignal): Promise<Cesion[]>;
  solicitar(
    idecheq: string,
    cuitCesionario: string,
    domicilioCesionario: string,
  ): Promise<Cesion>;
  resolver(
    idecheq: string,
    numero: number,
    aceptada: boolean,
    cuitResolutor: string,
  ): Promise<Cesion>;
  anular(idecheq: string, numero: number): Promise<void>;
}
