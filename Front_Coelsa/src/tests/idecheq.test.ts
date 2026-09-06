// Espejo del formato de IDECHEQ del backend: 11 letras mayúsculas.
import { describe, it, expect } from 'vitest';
import { IDECHEQ_LONGITUD, esIdEcheqValido } from '../domain/idecheq';

describe('esIdEcheqValido', () => {
  it('acepta 11 letras mayúsculas', () => {
    expect(IDECHEQ_LONGITUD).toBe(11);
    expect(esIdEcheqValido('ABCDEFGHIJK')).toBe(true);
  });

  it.each([
    null,
    undefined,
    '',
    'ABCDEFGHIJ', // 10 letras
    'ABCDEFGHIJKL', // 12 letras
    'ABC12345678', // con dígitos
    'abcdefghijk', // minúsculas
  ])('rechaza %s', (valor) => {
    expect(esIdEcheqValido(valor)).toBe(false);
  });
});
