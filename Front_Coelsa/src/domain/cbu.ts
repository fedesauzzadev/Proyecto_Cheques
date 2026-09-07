// Espejo de Coelsa.Domain/Validaciones/ValidadorCbu.cs.
// CBU argentino de 22 dígitos: entidad(3) + sucursal(4) + dv1 + cuenta(13) + dv2.
export const CBU_LONGITUD = 22;

const PESOS_BLOQUE_1 = [7, 1, 3, 9, 7, 1, 3];
const PESOS_BLOQUE_2 = [3, 9, 7, 1, 3, 9, 7, 1, 3, 9, 7, 1, 3];

function digitoVerificador(bloque: string, pesos: number[]): number {
  const suma = [...bloque].reduce((acc, digito, i) => acc + Number(digito) * pesos[i], 0);
  return (10 - (suma % 10)) % 10;
}

/** CBU válido: 22 dígitos con ambos verificadores correctos. */
export function esCbuValido(valor: string | null | undefined): boolean {
  if (!valor || valor.length !== CBU_LONGITUD || !/^\d{22}$/.test(valor)) {
    return false;
  }

  return (
    digitoVerificador(valor.slice(0, 7), PESOS_BLOQUE_1) === Number(valor[7]) &&
    digitoVerificador(valor.slice(8, 21), PESOS_BLOQUE_2) === Number(valor[21])
  );
}

export function bancoDeCbu(cbu: string): string {
  return cbu.slice(0, 3);
}

export function sucursalDeCbu(cbu: string): string {
  return cbu.slice(3, 7);
}
