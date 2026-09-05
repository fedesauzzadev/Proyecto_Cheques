// Adaptador de los puertos contra la API real (espejo de Coelsa.Infrastructure:
// implementa las interfaces de application sin que la UI conozca fetch ni URLs).
import type {
  CambiarEstadoRequest,
  ChequeResponse,
  EcheqResponse,
  PagedResponse,
} from '@/domain/tipos';
import type {
  ConsultaPaginada,
  EstadoSalud,
  IPuertoInstrumentos,
  IPuertoSalud,
  ResultadoCreacion,
} from '@/application/puertos';
import { pedir } from './clienteHttp';

const BASE = '/api/v1';

function queryDeListado({ cuit, page, pageSize }: ConsultaPaginada): string {
  return new URLSearchParams({ cuit, page: String(page), pageSize: String(pageSize) }).toString();
}

export const apiInstrumentos: IPuertoInstrumentos = {
  listarCheques(filtros, senal) {
    return pedir<PagedResponse<ChequeResponse>>(`${BASE}/cheques?${queryDeListado(filtros)}`, {
      senal,
    }).then((r) => r.datos);
  },

  listarEcheqs(filtros, senal) {
    return pedir<PagedResponse<EcheqResponse>>(`${BASE}/echeqs?${queryDeListado(filtros)}`, {
      senal,
    }).then((r) => r.datos);
  },

  obtenerCheque(cmc7, senal) {
    return pedir<ChequeResponse>(`${BASE}/cheques/${cmc7}`, { senal }).then((r) => r.datos);
  },

  obtenerEcheq(idecheq, senal) {
    return pedir<EcheqResponse>(`${BASE}/echeqs/${idecheq}`, { senal }).then((r) => r.datos);
  },

  async crearCheque(request, idempotencyKey): Promise<ResultadoCreacion<ChequeResponse>> {
    const r = await pedir<ChequeResponse>(`${BASE}/cheques`, {
      metodo: 'POST',
      cuerpo: request,
      cabeceras: { 'Idempotency-Key': idempotencyKey },
    });
    return { respuesta: r.datos, esReplay: r.cabeceras.get('Idempotent-Replay') === 'true' };
  },

  async crearEcheq(request, idempotencyKey): Promise<ResultadoCreacion<EcheqResponse>> {
    const r = await pedir<EcheqResponse>(`${BASE}/echeqs`, {
      metodo: 'POST',
      cuerpo: request,
      cabeceras: { 'Idempotency-Key': idempotencyKey },
    });
    return { respuesta: r.datos, esReplay: r.cabeceras.get('Idempotent-Replay') === 'true' };
  },

  cambiarEstadoCheque(cmc7, request: CambiarEstadoRequest) {
    return pedir<ChequeResponse>(`${BASE}/cheques/${cmc7}/estado`, {
      metodo: 'PATCH',
      cuerpo: request,
    }).then((r) => r.datos);
  },

  cambiarEstadoEcheq(idecheq, request: CambiarEstadoRequest) {
    return pedir<EcheqResponse>(`${BASE}/echeqs/${idecheq}/estado`, {
      metodo: 'PATCH',
      cuerpo: request,
    }).then((r) => r.datos);
  },

  async eliminarCheque(cmc7) {
    await pedir<void>(`${BASE}/cheques/${cmc7}`, { metodo: 'DELETE' });
  },

  async eliminarEcheq(idecheq) {
    await pedir<void>(`${BASE}/echeqs/${idecheq}`, { metodo: 'DELETE' });
  },
};

export const apiSalud: IPuertoSalud = {
  consultar(senal) {
    return pedir<EstadoSalud>('/health', { senal }).then((r) => r.datos);
  },
};
