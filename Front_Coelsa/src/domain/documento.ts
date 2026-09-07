// Espejo de Coelsa.Domain/Validaciones/ValidadorDocumento.cs (Fase B3).
// CUIT, CUIL y CDI: 11 dígitos con verificador módulo 11.
import { esCuitValido } from './validadorCuit';
import type { TipoDocumento } from './tipos';

export const TIPOS_DOCUMENTO: readonly TipoDocumento[] = ['CUIT', 'CUIL', 'CDI'] as const;

export const NOMBRE_LONGITUD_MAXIMA = 120;

export function esTipoDocumento(valor: string | null | undefined): valor is TipoDocumento {
  return valor === 'CUIT' || valor === 'CUIL' || valor === 'CDI';
}

export function esDocumentoValido(
  tipo: TipoDocumento | null | undefined,
  numero: string | null | undefined,
): boolean {
  return !!tipo && esTipoDocumento(tipo) && esCuitValido(numero);
}

export function esNombreValido(valor: string | null | undefined): boolean {
  return !!valor && valor.trim().length > 0 && valor.trim().length <= NOMBRE_LONGITUD_MAXIMA;
}
