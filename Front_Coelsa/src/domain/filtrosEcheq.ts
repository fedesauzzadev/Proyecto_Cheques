// Espejo de Coelsa.Application/Dtos/FiltrosEcheq.cs (Fase B6).
// Filtros opcionales del listado de echeqs: CBU, estado y rangos de hasta 360 días.
import { esCbuValido } from './cbu';
import type { EstadoInstrumento } from './tipos';
import { ESTADOS_INSTRUMENTO } from './tipos';
import type { ErroresCampo } from './validacionesInstrumento';

export interface FiltrosEcheq {
  cbu?: string | null;
  estado?: EstadoInstrumento | null;
  desdeEmision?: string | null;
  hastaEmision?: string | null;
  desdeVencimiento?: string | null;
  hastaVencimiento?: string | null;
  numeroCheque?: number | null;
}

export const RANGO_MAXIMO_DIAS = 360;

function diasEntre(desde: string, hasta: string): number {
  const ms = Date.parse(`${hasta}T00:00:00Z`) - Date.parse(`${desde}T00:00:00Z`);
  return Math.round(ms / 86400000);
}

function validarRango(
  desde: string | null | undefined,
  hasta: string | null | undefined,
  eje: string,
  campoHasta: string,
  errores: Record<string, string>,
): void {
  if (!desde || !hasta) return;
  if (hasta < desde) {
    errores[campoHasta] = `El rango de ${eje} es inválido ('hasta' anterior a 'desde').`;
    return;
  }
  if (diasEntre(desde, hasta) > RANGO_MAXIMO_DIAS) {
    errores[campoHasta] = `El rango de ${eje} no puede superar los ${RANGO_MAXIMO_DIAS} días.`;
  }
}

export function validarFiltrosEcheq(filtros: FiltrosEcheq): ErroresCampo {
  const errores: Record<string, string> = {};

  if (filtros.cbu && !esCbuValido(filtros.cbu.trim())) {
    errores.cbu = 'El filtro CBU debe ser un CBU válido de 22 dígitos.';
  }

  if (
    filtros.estado &&
    !(ESTADOS_INSTRUMENTO as readonly string[]).includes(filtros.estado)
  ) {
    errores.estado = 'El estado del filtro no es válido.';
  }

  validarRango(filtros.desdeEmision, filtros.hastaEmision, 'emisión', 'hastaEmision', errores);
  validarRango(
    filtros.desdeVencimiento,
    filtros.hastaVencimiento,
    'vencimiento',
    'hastaVencimiento',
    errores,
  );

  if (
    filtros.numeroCheque !== null &&
    filtros.numeroCheque !== undefined &&
    !(Number.isInteger(filtros.numeroCheque) && filtros.numeroCheque >= 1)
  ) {
    errores.numeroCheque = "El filtro 'número de cheque' debe ser mayor a cero.";
  }

  return errores;
}

/** Serialización estable para la clave de caché de TanStack Query. */
export function serializarFiltrosEcheq(filtros: FiltrosEcheq): string {
  return [
    filtros.cbu?.trim() ?? '-',
    filtros.estado ?? '-',
    filtros.desdeEmision ?? '-',
    filtros.hastaEmision ?? '-',
    filtros.desdeVencimiento ?? '-',
    filtros.hastaVencimiento ?? '-',
    filtros.numeroCheque ?? '-',
  ].join('|');
}
