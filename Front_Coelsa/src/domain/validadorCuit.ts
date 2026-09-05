// Espejo de Coelsa.Domain/Validaciones/ValidadorCuit.cs (módulo 11).

const PESOS = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2] as const;

/** Dígito verificador módulo 11: resto 0 => 0, resto 1 => 9, resto n => 11 - n. */
export function calcularDigitoVerificador(primerosDiezDigitos: string): number {
  let suma = 0;
  for (let i = 0; i < 10; i++) {
    suma += Number(primerosDiezDigitos[i]) * PESOS[i];
  }
  const resto = suma % 11;
  return resto === 0 ? 0 : resto === 1 ? 9 : 11 - resto;
}

export function esCuitValido(cuit: string | null | undefined): boolean {
  if (!cuit || cuit.length !== 11 || !/^\d{11}$/.test(cuit)) {
    return false;
  }
  return calcularDigitoVerificador(cuit.slice(0, 10)) === Number(cuit[10]);
}

/** Completa una base de 10 dígitos con su dígito verificador (útil para seeds y tests). */
export function completarCuit(baseDiezDigitos: string): string {
  return baseDiezDigitos + String(calcularDigitoVerificador(baseDiezDigitos));
}
