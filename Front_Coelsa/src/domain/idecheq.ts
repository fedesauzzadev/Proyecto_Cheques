// Espejo de la validación de IDECHEQ en Echeq.Crear (SPEC 5.2 del backend):
// alfabético de 11 letras mayúsculas, generado por el simulador.

export const IDECHEQ_LONGITUD = 11;

export function esIdEcheqValido(valor: string | null | undefined): boolean {
  return !!valor && valor.length === IDECHEQ_LONGITUD && /^[A-Z]{11}$/.test(valor);
}
