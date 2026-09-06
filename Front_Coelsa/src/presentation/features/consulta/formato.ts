// Formato de presentación de instrumentos (lenguaje de dominio en español).
import { ETIQUETAS_MOTIVO_RECHAZO } from '@/domain/tipos';
import type { Moneda, MotivoRechazo } from '@/domain/tipos';

export function formatearMonto(monto: number, moneda: Moneda): string {
  return new Intl.NumberFormat('es-AR', {
    style: 'currency',
    currency: moneda === 'P' ? 'ARS' : 'USD',
  }).format(monto);
}

/** 'YYYY-MM-DD' → 'DD/MM/YYYY'. */
export function formatearFecha(fechaIso: string): string {
  const [anio, mes, dia] = fechaIso.split('-');
  return `${dia}/${mes}/${anio}`;
}

/** Diferimiento nulo = cheque a la vista (SPEC 5.1 del backend). */
export function describirDiferimiento(fecha: string | null): string {
  return fecha ? formatearFecha(fecha) : 'A la vista';
}

export function describirMotivo(motivo: MotivoRechazo | null): string {
  return motivo === null ? '—' : `${motivo} · ${ETIQUETAS_MOTIVO_RECHAZO[motivo]}`;
}
