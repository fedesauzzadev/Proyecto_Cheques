// Claves de caché de TanStack Query (espejo conceptual del versionado de caché
// Redis del backend, SPEC sección 7: las escrituras invalidan por prefijo de tipo).
import type { TipoInstrumentoForm } from '@/domain/estrategias/estrategiaCreacion';
import { serializarFiltrosEcheq } from '@/domain/filtrosEcheq';
import type { FiltrosEcheq } from '@/domain/filtrosEcheq';

export const CLAVE_SALUD = ['salud'] as const;

export function claveListado(
  tipo: TipoInstrumentoForm,
  cuit: string,
  page: number,
  pageSize: number,
  filtrosEcheq?: FiltrosEcheq,
) {
  return [
    'instrumentos',
    tipo,
    'listado',
    cuit,
    page,
    pageSize,
    filtrosEcheq ? serializarFiltrosEcheq(filtrosEcheq) : '',
  ] as const;
}

export function claveDetalle(tipo: TipoInstrumentoForm, identificador: string) {
  return ['instrumentos', tipo, 'detalle', identificador] as const;
}

/** Prefijo para invalidar listados y detalles de un tipo tras una escritura. */
export function prefijoTipo(tipo: TipoInstrumentoForm) {
  return ['instrumentos', tipo] as const;
}

export function claveEndosos(idecheq: string) {
  return ['instrumentos', 'Echeq', 'endosos', idecheq] as const;
}

export function claveDevoluciones(idecheq: string) {
  return ['instrumentos', 'Echeq', 'devoluciones', idecheq] as const;
}

export function claveCesiones(idecheq: string) {
  return ['instrumentos', 'Echeq', 'cesiones', idecheq] as const;
}

export function claveCertificado(idecheq: string) {
  return ['instrumentos', 'Echeq', 'certificado', idecheq] as const;
}

export function claveCuentas(cuit: string) {
  return ['cuentas', 'listado', cuit] as const;
}

export function claveChequeras(cbu: string) {
  return ['cuentas', 'chequeras', cbu] as const;
}

/** Prefijo para invalidar cuentas y chequeras tras una escritura. */
export function prefijoCuentas() {
  return ['cuentas'] as const;
}
