// Claves de caché de TanStack Query (espejo conceptual del versionado de caché
// Redis del backend, SPEC sección 7: las escrituras invalidan por prefijo de tipo).
import type { TipoInstrumentoForm } from '@/domain/estrategias/estrategiaCreacion';

export const CLAVE_SALUD = ['salud'] as const;

export function claveListado(
  tipo: TipoInstrumentoForm,
  cuit: string,
  page: number,
  pageSize: number,
) {
  return ['instrumentos', tipo, 'listado', cuit, page, pageSize] as const;
}

export function claveDetalle(tipo: TipoInstrumentoForm, identificador: string) {
  return ['instrumentos', tipo, 'detalle', identificador] as const;
}

/** Prefijo para invalidar listados y detalles de un tipo tras una escritura. */
export function prefijoTipo(tipo: TipoInstrumentoForm) {
  return ['instrumentos', tipo] as const;
}
