// Espejo de Coelsa.Domain/Validaciones/TransicionesEstado.cs (SPEC 5.3 + Fase A + B5).
// Pendiente, Repudiado y EnCustodia son exclusivos de echeqs en la práctica:
// los cheques físicos nacen en Emitido y nunca los alcanzan.
import type { EstadoInstrumento } from './tipos';

const TRANSICIONES_VALIDAS: Readonly<Record<EstadoInstrumento, readonly EstadoInstrumento[]>> = {
  Pendiente: ['Emitido', 'Repudiado', 'Anulado'],
  Emitido: ['Depositado', 'Anulado', 'EnCustodia', 'Caducado'],
  Depositado: ['Compensado', 'Rechazado'],
  Compensado: ['Pagado'],
  EnCustodia: ['Emitido', 'Depositado', 'Caducado'],
  Rechazado: [],
  Anulado: [],
  Pagado: [],
  Repudiado: [],
  Caducado: [],
};

export function esTransicionValida(desde: EstadoInstrumento, hacia: EstadoInstrumento): boolean {
  return TRANSICIONES_VALIDAS[desde]?.includes(hacia) ?? false;
}

export function destinosDesde(estado: EstadoInstrumento): readonly EstadoInstrumento[] {
  return TRANSICIONES_VALIDAS[estado] ?? [];
}

/** RF-F05: solo Rechazado admite (y exige) motivo de rechazo. */
export function requiereMotivoRechazo(hacia: EstadoInstrumento): boolean {
  return hacia === 'Rechazado';
}
