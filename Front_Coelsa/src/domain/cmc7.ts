// Espejo de Coelsa.Domain/ValueObjects/Cmc7.cs.
import type { DesgloseCmc7 } from './tipos';

export const CMC7_LONGITUD = 30;

/** CMC7: código magnetizable de 30 dígitos de la banda inferior del cheque físico. */
export function esCmc7Valido(valor: string | null | undefined): boolean {
  return !!valor && valor.length === CMC7_LONGITUD && /^\d{30}$/.test(valor);
}

/**
 * Desglosa el CMC7 según el SPEC 5.1:
 * banco(3) + sucursal(4) + código postal(4) + número de cheque(8) + cuenta(11).
 */
export function desglosarCmc7(valor: string): DesgloseCmc7 {
  if (!esCmc7Valido(valor)) {
    throw new Error(
      `El CMC7 debe ser un código magnetizable de ${CMC7_LONGITUD} dígitos ` +
        '(banco + sucursal + código postal + número de cheque + cuenta).',
    );
  }
  return {
    banco: valor.slice(0, 3),
    sucursal: valor.slice(3, 7),
    codigoPostal: valor.slice(7, 11),
    numeroCheque: valor.slice(11, 19),
    numeroCuenta: valor.slice(19, 30),
  };
}
