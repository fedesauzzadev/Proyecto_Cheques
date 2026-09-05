// Espejo de Coelsa.UnitTests/Cmc7Tests.cs.
import { describe, it, expect } from 'vitest';
import { esCmc7Valido, desglosarCmc7 } from '../domain/cmc7';

const CMC7_VALIDO = '060000114250000123400001234567';

describe('esCmc7Valido', () => {
  it('acepta CMC7 de 30 dígitos', () => {
    expect(esCmc7Valido(CMC7_VALIDO)).toBe(true);
  });

  it.each([
    null,
    undefined,
    '',
    '06000011425000012340000123456', // 29 dígitos
    '0600001142500001234000012345678', // 31 dígitos
    '06000011425000012340000123456A', // letra
  ])('rechaza el formato inválido %s', (valor) => {
    expect(esCmc7Valido(valor)).toBe(false);
  });
});

describe('desglosarCmc7', () => {
  it('devuelve los tramos según el SPEC: banco(3) + sucursal(4) + CP(4) + cheque(8) + cuenta(11)', () => {
    const desglose = desglosarCmc7(CMC7_VALIDO);

    expect(desglose.banco).toBe('060');
    expect(desglose.sucursal).toBe('0001');
    expect(desglose.codigoPostal).toBe('1425');
    expect(desglose.numeroCheque).toBe('00001234');
    expect(desglose.numeroCuenta).toBe('00001234567');
  });

  it('con valor inválido lanza error de validación', () => {
    expect(() => desglosarCmc7('123')).toThrowError(/30 dígitos/);
  });
});
