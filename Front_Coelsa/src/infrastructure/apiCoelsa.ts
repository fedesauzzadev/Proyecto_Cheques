// Adaptador de los puertos contra la API real (espejo de Coelsa.Infrastructure:
// implementa las interfaces de application sin que la UI conozca fetch ni URLs).
import type {
  CambiarEstadoRequest,
  ChequeResponse,
  Devolucion,
  EcheqResponse,
  Endoso,
  PagedResponse,
} from '@/domain/tipos';
import type {
  ConsultaPaginada,
  EstadoSalud,
  IPuertoAceptacion,
  IPuertoDevoluciones,
  IPuertoEndosos,
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

export const apiAceptacion: IPuertoAceptacion = {
  aceptar(idecheq, aceptada) {
    return pedir<EcheqResponse>(`${BASE}/echeqs/${idecheq}/aceptacion`, {
      metodo: 'POST',
      cuerpo: { aceptada },
    }).then((r) => r.datos);
  },
};

export const apiEndosos: IPuertoEndosos = {
  listar(idecheq, senal) {
    return pedir<Endoso[]>(`${BASE}/echeqs/${idecheq}/endosos`, { senal }).then((r) => r.datos);
  },

  proponer(idecheq, cuitEndosatario) {
    return pedir<Endoso>(`${BASE}/echeqs/${idecheq}/endosos`, {
      metodo: 'POST',
      cuerpo: { cuitEndosatario },
    }).then((r) => r.datos);
  },

  resolver(idecheq, orden, admitido, cuit) {
    return pedir<Endoso>(`${BASE}/echeqs/${idecheq}/endosos/${orden}/admision`, {
      metodo: 'POST',
      cuerpo: { admitido, cuit },
    }).then((r) => r.datos);
  },

  async anular(idecheq, orden) {
    await pedir<void>(`${BASE}/echeqs/${idecheq}/endosos/${orden}`, { metodo: 'DELETE' });
  },
};

export const apiDevoluciones: IPuertoDevoluciones = {
  listar(idecheq, senal) {
    return pedir<Devolucion[]>(`${BASE}/echeqs/${idecheq}/devoluciones`, { senal }).then(
      (r) => r.datos,
    );
  },

  solicitar(idecheq, cuitSolicitante, motivo) {
    return pedir<Devolucion>(`${BASE}/echeqs/${idecheq}/devoluciones`, {
      metodo: 'POST',
      cuerpo: { cuitSolicitante, motivo: motivo ?? null },
    }).then((r) => r.datos);
  },

  resolver(idecheq, numero, aceptada, cuitResolutor) {
    return pedir<Devolucion>(`${BASE}/echeqs/${idecheq}/devoluciones/${numero}/resolucion`, {
      metodo: 'POST',
      cuerpo: { aceptada, cuitResolutor },
    }).then((r) => r.datos);
  },

  async anular(idecheq, numero) {
    await pedir<void>(`${BASE}/echeqs/${idecheq}/devoluciones/${numero}`, { metodo: 'DELETE' });
  },
};
