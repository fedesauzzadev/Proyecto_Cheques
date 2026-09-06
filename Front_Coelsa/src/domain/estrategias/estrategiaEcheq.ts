// Espejo de EcheqCreationStrategy del backend.
import { esCmc7Valido } from '../cmc7';
import { validarComunes } from '../validacionesInstrumento';
import type { ErroresCampo } from '../validacionesInstrumento';
import type { CrearEcheqRequest } from '../tipos';
import type { EstrategiaEcheq } from './estrategiaCreacion';

export const estrategiaEcheq: EstrategiaEcheq = {
  tipo: 'Echeq',
  titulo: 'Nuevo echeq',
  descripcion:
    'Cheque electrónico identificado por su IDECHEQ (11 letras, generado por el simulador al crear).',
  identificadorEtiqueta: 'IDECHEQ (generado por la API)',

  campos: [
    {
      nombre: 'cmc7',
      etiqueta: 'CMC7',
      tipo: 'texto',
      obligatorio: true,
      placeholder: '011000114250000123400001234567',
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
    {
      nombre: 'fechaVencimiento',
      etiqueta: 'Fecha de vencimiento',
      tipo: 'fecha',
      obligatorio: true,
      ayuda: 'Posterior a la emisión (y al diferimiento si viene).',
    },
  ],

  validar(request: CrearEcheqRequest, hoy?: string): ErroresCampo {
    const errores: Record<string, string> = {};

    if (!esCmc7Valido(request.cmc7)) {
      errores.cmc7 =
        `El CMC7 debe ser un código magnetizable de 30 dígitos ` +
        '(banco + sucursal + código postal + número de cheque + cuenta).';
    }

    return { ...errores, ...validarComunes(request, hoy) };
  },

  construir(valores: Record<string, string>): CrearEcheqRequest {
    return {
      cmc7: (valores.cmc7 ?? '').trim(),
      cuitLibrador: (valores.cuitLibrador ?? '').trim(),
      cuitBeneficiario: (valores.cuitBeneficiario ?? '').trim(),
      monto: Number(valores.monto),
      moneda: valores.moneda === 'D' ? 'D' : 'P',
      fechaEmision: valores.fechaEmision ?? '',
      fechaDiferimiento: valores.fechaDiferimiento?.trim() ? valores.fechaDiferimiento : null,
      fechaVencimiento: valores.fechaVencimiento ?? '',
    };
  },
};
