// Espejo de Coelsa.Domain/Validaciones/TransicionesEstado.cs (SPEC 5.3):
// Emitido → Depositado → Compensado → Pagado; Emitido → Anulado; Depositado → Rechazado.
import type { EstadoInstrumento } from './tipos';

const TRANSICIONES_VALIDAS: Readonly<Record<EstadoInstrumento, readonly EstadoInstrumento[]>> = {
  Emitido: ['Depositado', 'Anulado'],
  Depositado: ['Compensado', 'Rechazado'],
  Compensado: ['Pagado'],
  Rechazado: [],
  Anulado: [],
  Pagado: [],
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
