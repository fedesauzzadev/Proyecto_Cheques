// Espejo de EcheqCreationStrategy del backend.
import { validarComunes } from '../validacionesInstrumento';
import type { ErroresCampo } from '../validacionesInstrumento';
import type { CrearEcheqRequest } from '../tipos';
import type { EstrategiaEcheq } from './estrategiaCreacion';

export const estrategiaEcheq: EstrategiaEcheq = {
  tipo: 'Echeq',
  titulo: 'Nuevo echeq',
  descripcion:
    'Cheque electrónico identificado por su IDECHEQ (generado por el simulador al crear).',
  identificadorEtiqueta: 'IDECHEQ (generado por la API)',

  campos: [
    {
      nombre: 'cud',
      etiqueta: 'CUD',
      tipo: 'texto',
      obligatorio: true,
      placeholder: 'a3f5… (64 caracteres hexadecimales)',
      ayuda: 'Clave Única Digital: hash SHA-256 en hexadecimal.',
    },
    {
      nombre: 'codigoBanco',
      etiqueta: 'Código de banco',
      tipo: 'texto',
      obligatorio: true,
      placeholder: '011',
    },
    {
      nombre: 'numeroCuenta',
      etiqueta: 'Número de cuenta',
      tipo: 'texto',
      obligatorio: true,
      placeholder: '000098765432',
    },
    {
      nombre: 'cuitLibrador',
      etiqueta: 'CUIT/CUIL librador',
      tipo: 'texto',
      obligatorio: true,
      placeholder: '20123456786',
    },
    {
      nombre: 'cuitBeneficiario',
      etiqueta: 'CUIT/CUIL beneficiario',
      tipo: 'texto',
      obligatorio: true,
      placeholder: '27876543219',
    },
    {
      nombre: 'monto',
      etiqueta: 'Monto',
      tipo: 'numero',
      obligatorio: true,
      placeholder: '250000.00',
    },
    { nombre: 'moneda', etiqueta: 'Moneda', tipo: 'moneda', obligatorio: true },
    { nombre: 'fechaEmision', etiqueta: 'Fecha de emisión', tipo: 'fecha', obligatorio: true },
    {
      nombre: 'fechaDiferimiento',
      etiqueta: 'Fecha de diferimiento',
      tipo: 'fecha',
      obligatorio: false,
    },
  ],

  validar(request: CrearEcheqRequest, hoy?: string): ErroresCampo {
    const errores: Record<string, string> = {};

    if (!/^[0-9a-f]{64}$/i.test(request.cud)) {
      errores.cud = 'El CUD debe ser un hash SHA-256 en hexadecimal (64 caracteres).';
    }

    if (!/^\d{3}$/.test(request.codigoBanco)) {
      errores.codigoBanco = 'El código de banco debe ser de 3 dígitos.';
    }

    if (!/^\d{12}$/.test(request.numeroCuenta)) {
      errores.numeroCuenta = 'El número de cuenta debe ser de 12 dígitos.';
    }

    return { ...errores, ...validarComunes(request, hoy) };
  },

  construir(valores: Record<string, string>): CrearEcheqRequest {
    return {
      cud: (valores.cud ?? '').trim().toLowerCase(),
      codigoBanco: (valores.codigoBanco ?? '').trim(),
      numeroCuenta: (valores.numeroCuenta ?? '').trim(),
      cuitLibrador: (valores.cuitLibrador ?? '').trim(),
      cuitBeneficiario: (valores.cuitBeneficiario ?? '').trim(),
      monto: Number(valores.monto),
      moneda: valores.moneda === 'D' ? 'D' : 'P',
      fechaEmision: valores.fechaEmision ?? '',
      fechaDiferimiento: valores.fechaDiferimiento?.trim() ? valores.fechaDiferimiento : null,
    };
  },
};
