// Espejo de Coelsa.UnitTests/TransicionesEstadoTests.cs + ValidarEstadoCambio.
import { describe, it, expect } from 'vitest';
import { esTransicionValida, destinosDesde, requiereMotivoRechazo } from '../domain/transiciones';
import { validarCambioEstado } from '../domain/validacionesInstrumento';

describe('esTransicionValida', () => {
  it.each([
    ['Emitido', 'Depositado'],
    ['Emitido', 'Anulado'],
    ['Depositado', 'Compensado'],
    ['Depositado', 'Rechazado'],
    ['Compensado', 'Pagado'],
  ] as const)('acepta la transición del diagrama %s → %s', (desde, hacia) => {
    expect(esTransicionValida(desde, hacia)).toBe(true);
  });

  it.each([
    ['Emitido', 'Compensado'], // salto inválido
    ['Emitido', 'Pagado'],
    ['Anulado', 'Depositado'], // terminal
    ['Rechazado', 'Compensado'], // terminal
    ['Pagado', 'Emitido'], // terminal
    ['Compensado', 'Rechazado'], // solo desde Depositado
  ] as const)('rechaza la transición inválida %s → %s', (desde, hacia) => {
    expect(esTransicionValida(desde, hacia)).toBe(false);
  });

  it('los estados terminales no tienen destinos', () => {
    expect(destinosDesde('Anulado')).toEqual([]);
    expect(destinosDesde('Rechazado')).toEqual([]);
    expect(destinosDesde('Pagado')).toEqual([]);
  });
});

describe('requiereMotivoRechazo (RF-F05)', () => {
  it('solo Rechazado admite motivo', () => {
    expect(requiereMotivoRechazo('Rechazado')).toBe(true);
    expect(requiereMotivoRechazo('Depositado')).toBe(false);
    expect(requiereMotivoRechazo('Anulado')).toBe(false);
  });
});

describe('validarCambioEstado', () => {
  it('Rechazado sin motivo devuelve error', () => {
    expect(validarCambioEstado('Depositado', 'Rechazado', null)).toBe(
      'El estado Rechazado exige un motivo de rechazo.',
    );
  });

  it('un estado distinto de Rechazado con motivo devuelve error', () => {
    expect(validarCambioEstado('Emitido', 'Anulado', 11)).toBe(
      'Solo el estado Rechazado admite un motivo de rechazo.',
    );
  });

  it('transición inválida detalla los destinos válidos', () => {
    expect(validarCambioEstado('Compensado', 'Rechazado', null)).toBe(
      'No se puede pasar del estado Compensado al estado Rechazado. ' +
        'Destinos válidos desde Compensado: Pagado.',
    );
  });

  it('transición válida con motivo correcto no devuelve error', () => {
    expect(validarCambioEstado('Depositado', 'Rechazado', 11)).toBeNull();
  });
});
