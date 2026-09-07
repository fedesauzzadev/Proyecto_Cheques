// Espejo de los casos de ValidadorDocumento (Fase B3).
import { describe, it, expect } from 'vitest';
import {
  esDocumentoValido,
  esNombreValido,
  esTipoDocumento,
} from '../domain/documento';

describe('esTipoDocumento', () => {
  it.each(['CUIT', 'CUIL', 'CDI'])('acepta %s', (tipo) => {
    expect(esTipoDocumento(tipo)).toBe(true);
  });

  it.each(['DNI', '', null, undefined])('rechaza %s', (tipo) => {
    expect(esTipoDocumento(tipo)).toBe(false);
  });
});

describe('esDocumentoValido', () => {
  it('acepta CDI de 11 dígitos con verificador', () => {
    expect(esDocumentoValido('CDI', '20123456786')).toBe(true);
  });

  it('rechaza número inválido aunque el tipo sea válido', () => {
    expect(esDocumentoValido('CUIT', '123')).toBe(false);
  });

  it('rechaza tipo inválido', () => {
    expect(esDocumentoValido(null, '20123456786')).toBe(false);
  });
});

describe('esNombreValido', () => {
  it('acepta nombres con contenido hasta 120 caracteres', () => {
    expect(esNombreValido('Alfa S.R.L.')).toBe(true);
  });

  it.each(['', '   ', null, undefined, 'x'.repeat(121)])('rechaza %s', (valor) => {
    expect(esNombreValido(valor)).toBe(false);
  });
});
