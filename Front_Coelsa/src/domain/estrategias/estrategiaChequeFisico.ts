// Espejo de ChequeFisicoCreationStrategy del backend.
import { esCmc7Valido } from '../cmc7';
import { validarComunes } from '../validacionesInstrumento';
import type { ErroresCampo } from '../validacionesInstrumento';
import type { CrearChequeFisicoRequest } from '../tipos';
import type { EstrategiaChequeFisico } from './estrategiaCreacion';

export const estrategiaChequeFisico: EstrategiaChequeFisico = {
  tipo: 'ChequeFisico',
  titulo: 'Nuevo cheque físico',
  descripcion: 'Cheque de papel identificado por su CMC7 (banda magnetizable de 30 dígitos).',
  identificadorEtiqueta: 'CMC7',

  campos: [
    {
      nombre: 'cmc7',
      etiqueta: 'CMC7',
      tipo: 'texto',
      obligatorio: true,
      placeholder: '060000114250000123400001234567',
      ayuda: 'banco(3) + sucursal(4) + código postal(4) + número de cheque(8) + cuenta(11)',
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
      placeholder: '150000.50',
    },
    { nombre: 'moneda', etiqueta: 'Moneda', tipo: 'moneda', obligatorio: true },
    { nombre: 'fechaEmision', etiqueta: 'Fecha de emisión', tipo: 'fecha', obligatorio: true },
    {
      nombre: 'fechaDiferimiento',
      etiqueta: 'Fecha de diferimiento',
      tipo: 'fecha',
      obligatorio: false,
      ayuda: 'Opcional: cheques de pago diferido.',
    },
  ],

  validar(request: CrearChequeFisicoRequest, hoy?: string): ErroresCampo {
    const errores: Record<string, string> = {};

    if (!esCmc7Valido(request.cmc7)) {
      errores.cmc7 =
        `El CMC7 debe ser un código magnetizable de 30 dígitos ` +
        '(banco + sucursal + código postal + número de cheque + cuenta).';
    }

    return { ...errores, ...validarComunes(request, hoy) };
  },
};
