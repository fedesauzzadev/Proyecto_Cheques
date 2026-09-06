// Espejo de Coelsa.UnitTests/EstrategiasCreacionTests.cs: validaciones de las
// estrategias de creación con los mismos mensajes que el backend.
import { describe, it, expect } from 'vitest';
import { estrategiaChequeFisico, estrategiaEcheq } from '../domain/estrategias';
import type { CrearChequeFisicoRequest, CrearEcheqRequest } from '../domain/tipos';

const HOY = '2026-09-05';

const chequeValido: CrearChequeFisicoRequest = {
  cmc7: '060000114250000123400001234567',
  cuitLibrador: '20123456786',
  cuitBeneficiario: '27876543219',
  monto: 1500000.5,
  moneda: 'P',
  fechaEmision: '2026-09-05',
  fechaDiferimiento: '2026-10-05',
};

const echeqValido: CrearEcheqRequest = {
  cmc7: '011000114250000123400001234567',
  cuitLibrador: '20123456786',
  cuitBeneficiario: '30511222334',
  monto: 250000,
  moneda: 'D',
  fechaEmision: '2026-09-05',
};

describe('estrategiaChequeFisico', () => {
  it('acepta un request válido sin errores', () => {
    expect(estrategiaChequeFisico.validar(chequeValido, HOY)).toEqual({});
  });

  it('rechaza CMC7 mal formado con el mensaje del dominio', () => {
    const errores = estrategiaChequeFisico.validar({ ...chequeValido, cmc7: '123' }, HOY);
    expect(errores.cmc7).toMatch(/30 dígitos/);
  });

  it('marca el campo del CUIT inválido', () => {
    const errores = estrategiaChequeFisico.validar(
      { ...chequeValido, cuitLibrador: '2012345678A' },
      HOY,
    );
    expect(errores.cuitLibrador).toMatch(/librador/);
    expect(errores.cuitBeneficiario).toBeUndefined();
  });

  it('rechaza monto no positivo', () => {
    expect(estrategiaChequeFisico.validar({ ...chequeValido, monto: 0 }, HOY).monto).toMatch(
      /mayor a cero/,
    );
  });

  it('rechaza fecha de emisión futura (máximo hoy+1)', () => {
    const errores = estrategiaChequeFisico.validar(
      { ...chequeValido, fechaEmision: '2026-09-10' },
      HOY,
    );
    expect(errores.fechaEmision).toMatch(/no puede ser mayor a 2026-09-06/);
  });

  it('rechaza diferimiento anterior a la emisión', () => {
    const errores = estrategiaChequeFisico.validar(
      { ...chequeValido, fechaDiferimiento: '2026-09-01' },
      HOY,
    );
    expect(errores.fechaDiferimiento).toMatch(/anterior a la fecha de emisión/);
  });
});

describe('estrategiaEcheq', () => {
  it('acepta un request válido sin errores', () => {
    expect(estrategiaEcheq.validar(echeqValido, HOY)).toEqual({});
  });

  it('rechaza CMC7 mal formado con el mensaje del dominio', () => {
    const errores = estrategiaEcheq.validar({ ...echeqValido, cmc7: '123' }, HOY);
    expect(errores.cmc7).toMatch(/30 dígitos/);
  });
});

describe('construir (request builder)', () => {
  it('arma el request del cheque normalizando strings y monto', () => {
    const request = estrategiaChequeFisico.construir({
      cmc7: ' 060000114250000123400001234567 ',
      cuitLibrador: '20123456786',
      cuitBeneficiario: '27876543219',
      monto: '150000.50',
      moneda: 'P',
      fechaEmision: '2026-09-05',
      fechaDiferimiento: '',
    });

    expect(request).toEqual({
      cmc7: '060000114250000123400001234567',
      cuitLibrador: '20123456786',
      cuitBeneficiario: '27876543219',
      monto: 150000.5,
      moneda: 'P',
      fechaEmision: '2026-09-05',
      fechaDiferimiento: null,
    });
  });

  it('arma el request del echeq normalizando el CMC7', () => {
    const request = estrategiaEcheq.construir({
      cmc7: ' 011000114250000123400001234567 ',
      cuitLibrador: '20123456786',
      cuitBeneficiario: '30511222334',
      monto: '250000',
      moneda: 'D',
      fechaEmision: '2026-09-05',
      fechaDiferimiento: '',
    });

    expect(request.cmc7).toBe('011000114250000123400001234567');
    expect(request.moneda).toBe('D');
    expect(estrategiaEcheq.validar(request, HOY)).toEqual({});
  });
});

describe('acumulación de errores', () => {
  it('acumula varios errores a la vez', () => {
    const errores = estrategiaEcheq.validar(
      { ...echeqValido, cmc7: 'no', cuitLibrador: '1', monto: -5 },
      HOY,
    );
    expect(Object.keys(errores).sort()).toEqual(['cmc7', 'cuitLibrador', 'monto']);
  });
});
