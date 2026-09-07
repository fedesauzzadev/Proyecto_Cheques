// Espejo de Coelsa.UnitTests/ValidadorCbuTests.cs.
import { describe, it, expect } from 'vitest';
import { esCbuValido, bancoDeCbu, sucursalDeCbu } from '../domain/cbu';

const CBU_VALIDO = '0110001300000000000017';

describe('esCbuValido', () => {
  it('acepta CBU con verificadores correctos', () => {
    expect(esCbuValido(CBU_VALIDO)).toBe(true);
  });

  it.each([null, undefined, '', '123', '0110001300000000000018', '0110001400000000000017'])(
    'rechaza %s',
    (valor) => {
      expect(esCbuValido(valor)).toBe(false);
    },
  );
});

describe('desglose de CBU', () => {
  it('extrae banco y sucursal', () => {
    expect(bancoDeCbu(CBU_VALIDO)).toBe('011');
    expect(sucursalDeCbu(CBU_VALIDO)).toBe('0001');
  });
});
