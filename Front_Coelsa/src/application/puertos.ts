// Puertos de la aplicación (espejo de Coelsa.Application/Puertos/Puertos.cs).
// Los hooks consumen estas interfaces; la infraestructura las implementa.
import type {
  CambiarEstadoRequest,
  ChequeResponse,
  CrearChequeFisicoRequest,
  CrearEcheqRequest,
  EcheqResponse,
  PagedResponse,
} from '@/domain/tipos';

export interface ConsultaPaginada {
  cuit: string;
  page: number;
  pageSize: number;
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
