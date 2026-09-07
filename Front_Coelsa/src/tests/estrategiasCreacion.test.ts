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
  fechaVencimiento: '2026-11-05',
};

const CBU_VALIDO = '0110001300000000000017';

const echeqValido: CrearEcheqRequest = {
  cbuEmisor: CBU_VALIDO,
  caracter: 'AlaOrden',
  tipoDocBeneficiario: 'CUIT',
  nombreLibrador: 'Alfa S.R.L.',
  nombreBeneficiario: 'Beta S.A.',
  cuitLibrador: '20123456786',
  cuitBeneficiario: '30511222334',
  monto: 250000,
  moneda: 'D',
  fechaEmision: '2026-09-05',
  fechaVencimiento: '2026-10-05',
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

  it('rechaza vencimiento no posterior a la emisión', () => {
    const errores = estrategiaChequeFisico.validar(
      { ...chequeValido, fechaDiferimiento: null, fechaVencimiento: '2026-09-05' },
      HOY,
    );
    expect(errores.fechaVencimiento).toMatch(/posterior a la fecha de emisión/);
  });

  it('rechaza vencimiento no posterior al diferimiento', () => {
    const errores = estrategiaChequeFisico.validar(
      { ...chequeValido, fechaVencimiento: '2026-10-05' },
      HOY,
    );
    expect(errores.fechaVencimiento).toMatch(/posterior a la fecha de diferimiento/);
  });
});

describe('estrategiaEcheq', () => {
  it('acepta un request válido sin errores (con CBU de la cuenta)', () => {
    expect(estrategiaEcheq.validar(echeqValido, HOY)).toEqual({});
    expect(estrategiaEcheq.campos.some((campo) => campo.nombre === 'cbuEmisor')).toBe(true);
  });

  it('rechaza CBU mal formado con mensaje claro', () => {
    const errores = estrategiaEcheq.validar({ ...echeqValido, cbuEmisor: '123' }, HOY);
    expect(errores.cbuEmisor).toMatch(/22 dígitos/);
  });

  it('rechaza carácter inválido', () => {
    const errores = estrategiaEcheq.validar(
      { ...echeqValido, caracter: 'AlPortador' as never },
      HOY,
    );
    expect(errores.caracter).toMatch(/carácter/);
  });

  it('rechaza tipo de documento inválido y nombre vacío', () => {
    const errores = estrategiaEcheq.validar(
      { ...echeqValido, tipoDocBeneficiario: 'DNI' as never, nombreBeneficiario: '  ' },
      HOY,
    );
    expect(errores.tipoDocBeneficiario).toMatch(/CUIT/);
    expect(errores.nombreBeneficiario).toMatch(/obligatorio/);
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
      fechaVencimiento: '2026-11-05',
    });

    expect(request).toEqual({
      cmc7: '060000114250000123400001234567',
      cuitLibrador: '20123456786',
      cuitBeneficiario: '27876543219',
      monto: 150000.5,
      moneda: 'P',
      fechaEmision: '2026-09-05',
      fechaDiferimiento: null,
      fechaVencimiento: '2026-11-05',
    });
  });

  it('arma el request del echeq con el CBU de débito', () => {
    const request = estrategiaEcheq.construir({
      cbuEmisor: ` ${CBU_VALIDO} `,
      caracter: 'NoAlaOrden',
      tipoDocBeneficiario: 'CUIL',
      nombreLibrador: '  Alfa S.R.L. ',
      nombreBeneficiario: 'Beta S.A.',
      concepto: 'Pago a proveedores',
      motivo: '',
      referencia: '  ',
      emailNotificacion: 'cobros@demo.local',
      cuitLibrador: '20123456786',
      cuitBeneficiario: '30511222334',
      monto: '250000',
      moneda: 'D',
      fechaEmision: '2026-09-05',
      fechaDiferimiento: '',
      fechaVencimiento: '2026-10-05',
    });

    expect(request.cbuEmisor).toBe(CBU_VALIDO);
    expect(request.caracter).toBe('NoAlaOrden');
    expect(request.tipoDocBeneficiario).toBe('CUIL');
    expect(request.nombreLibrador).toBe('Alfa S.R.L.');
    expect(request.concepto).toBe('Pago a proveedores');
    expect(request.motivo).toBeNull();
    expect(request.referencia).toBeNull();
    expect(request.emailNotificacion).toBe('cobros@demo.local');
    expect(request.moneda).toBe('D');
    expect(request.fechaVencimiento).toBe('2026-10-05');
    expect(estrategiaEcheq.validar(request, HOY)).toEqual({});
  });

  it('rechaza gestión inválida (concepto largo, email mal formado)', () => {
    const errores = estrategiaEcheq.validar(
      { ...echeqValido, concepto: 'x'.repeat(61), emailNotificacion: 'sin-arroba' },
      HOY,
    );
    expect(errores.concepto).toMatch(/60 caracteres/);
    expect(errores.emailNotificacion).toMatch(/email válido/);
  });

  it('rechaza tenor mayor a 360 días', () => {
    const errores = estrategiaEcheq.validar({ ...echeqValido, fechaVencimiento: '2027-09-05' }, HOY);
    expect(errores.fechaVencimiento).toMatch(/360 días/);
  });
});

describe('acumulación de errores', () => {
  it('acumula varios errores a la vez', () => {
    const errores = estrategiaEcheq.validar(
      { ...echeqValido, cbuEmisor: 'no', cuitLibrador: '1', monto: -5, nombreBeneficiario: '' },
      HOY,
    );
    expect(Object.keys(errores).sort()).toEqual([
      'cbuEmisor',
      'cuitLibrador',
      'monto',
      'nombreBeneficiario',
    ]);
  });
});
