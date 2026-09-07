// Espejo de Coelsa.UnitTests/ListarEcheqsHandlerTests.cs (Fase B6).
import { describe, it, expect } from 'vitest';
import {
  serializarFiltrosEcheq,
  validarFiltrosEcheq,
} from '../domain/filtrosEcheq';

describe('validarFiltrosEcheq', () => {
  it('acepta filtros vacíos', () => {
    expect(validarFiltrosEcheq({})).toEqual({});
  });

  it('acepta CBU, estado, rangos y número válidos', () => {
    expect(
      validarFiltrosEcheq({
        cbu: '0110001300000000000017',
        estado: 'Emitido',
        desdeEmision: '2026-09-01',
        hastaEmision: '2026-09-30',
        numeroCheque: 7,
      }),
    ).toEqual({});
  });

  it('rechaza CBU, estado y número inválidos', () => {
    const errores = validarFiltrosEcheq({
      cbu: '123',
      estado: 'Volando' as never,
      numeroCheque: 0,
    });
    expect(errores.cbu).toMatch(/22 dígitos/);
    expect(errores.estado).toMatch(/no es válido/);
    expect(errores.numeroCheque).toMatch(/mayor a cero/);
  });

  it('rechaza rango invertido y rango mayor a 360 días', () => {
    expect(
      validarFiltrosEcheq({ desdeEmision: '2026-09-10', hastaEmision: '2026-09-01' }).hastaEmision,
    ).toMatch(/anterior/);
    expect(
      validarFiltrosEcheq({ desdeEmision: '2025-01-01', hastaEmision: '2026-09-05' }).hastaEmision,
    ).toMatch(/360 días/);
  });
});

describe('serializarFiltrosEcheq', () => {
  it('distingue combinaciones de filtros', () => {
    expect(serializarFiltrosEcheq({})).not.toBe(
      serializarFiltrosEcheq({ estado: 'Emitido' }),
    );
  });
});
