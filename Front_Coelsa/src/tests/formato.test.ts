import { describe, it, expect } from 'vitest';
import {
  describirDiferimiento,
  describirMotivo,
  formatearFecha,
  formatearMonto,
} from '../presentation/features/consulta/formato';

describe('formatearMonto', () => {
  it('formatea pesos en locale es-AR', () => {
    const texto = formatearMonto(150000.5, 'P');
    expect(texto).toContain('150.000,50');
    expect(texto).toContain('$');
  });

  it('formatea dólares con símbolo US$', () => {
    expect(formatearMonto(300000, 'D')).toContain('US$');
  });
});

describe('formatearFecha', () => {
  it('convierte YYYY-MM-DD a DD/MM/YYYY', () => {
    expect(formatearFecha('2026-09-05')).toBe('05/09/2026');
  });
});

describe('describirDiferimiento', () => {
  it('nulo significa cheque a la vista', () => {
    expect(describirDiferimiento(null)).toBe('A la vista');
  });

  it('con fecha la formatea', () => {
    expect(describirDiferimiento('2026-10-05')).toBe('05/10/2026');
  });
});

describe('describirMotivo', () => {
  it('muestra código y etiqueta en español', () => {
    expect(describirMotivo(11)).toBe('11 · Falta de fondos');
  });

  it('nulo se muestra como guion', () => {
    expect(describirMotivo(null)).toBe('—');
  });
});
