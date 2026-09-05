// Espejo de Coelsa.UnitTests/ValidadorCuitTests.cs.
import { describe, it, expect } from 'vitest';
import { calcularDigitoVerificador, completarCuit, esCuitValido } from '../domain/validadorCuit';

describe('completarCuit', () => {
  it.each(['2012345678', '2787654321', '3051122233', '2033445566'])(
    'genera CUITs válidos desde %s',
    (baseDiez) => {
      const cuit = completarCuit(baseDiez);
      expect(cuit).toHaveLength(11);
      expect(esCuitValido(cuit)).toBe(true);
    },
  );
});

describe('esCuitValido', () => {
  it('rechaza CUIT con dígito verificador incorrecto', () => {
    const cuit = completarCuit('2012345678');
    const cuitFalsificado = cuit.slice(0, 10) + (cuit[10] === '9' ? '0' : '9');
    expect(esCuitValido(cuitFalsificado)).toBe(false);
  });

  it.each([
    null,
    undefined,
    '',
    '201234567', // 9 dígitos
    '201234567890', // 12 dígitos
    '2012345678A', // letra
    '20 12345678 9', // espacios
  ])('rechaza el formato inválido %s', (cuit) => {
    expect(esCuitValido(cuit)).toBe(false);
  });

  it('el verificador módulo 11 coincide con el algoritmo del backend', () => {
    // Casos anclados al algoritmo: 2012345678 → verificador 6 (resto 5 → 11-5).
    expect(calcularDigitoVerificador('2012345678')).toBe(6);
    expect(calcularDigitoVerificador('2787654321')).toBe(9); // resto 1 → 9
    expect(calcularDigitoVerificador('3051122233')).toBe(4);
  });
});
