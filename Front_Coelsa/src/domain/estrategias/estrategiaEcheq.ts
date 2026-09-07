// Espejo de EcheqCreationStrategy del backend (CMC7 e IDECHEQ autogenerados,
// CBU de la cuenta de débito obligatorio con chequera vigente).
import { esCbuValido } from '../cbu';
import { esDocumentoValido, esNombreValido, esTipoDocumento } from '../documento';
import { validarComunes } from '../validacionesInstrumento';
import type { ErroresCampo } from '../validacionesInstrumento';
import type { CrearEcheqRequest } from '../tipos';
import type { EstrategiaEcheq } from './estrategiaCreacion';

function textoOpcional(valor: string | null | undefined): string | null {
  const normalizado = (valor ?? '').trim();
  return normalizado === '' ? null : normalizado;
}

function validarGestion(
  valor: string | null | undefined,
  maximo: number,
  campo: string,
  errores: Record<string, string>,
  esEmail = false,
): void {
  const normalizado = (valor ?? '').trim();
  if (normalizado === '') return;
  if (normalizado.length > maximo) {
    errores[campo] = `El campo '${campo}' debe tener hasta ${maximo} caracteres.`;
    return;
  }
  if (esEmail && !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(normalizado)) {
    errores[campo] = `El campo '${campo}' debe ser un email válido.`;
  }
}

export const estrategiaEcheq: EstrategiaEcheq = {
  tipo: 'Echeq',
  titulo: 'Nuevo echeq',
  descripcion:
    'Cheque electrónico identificado por su IDECHEQ (11 letras) y CMC7 (30 dígitos), ambos generados por el simulador al crear.',
  identificadorEtiqueta: 'IDECHEQ + CMC7 (generados por la API)',

  campos: [
    {
      nombre: 'cbuEmisor',
      etiqueta: 'CBU cuenta de débito',
      tipo: 'texto',
      obligatorio: true,
      placeholder: '0110001300000000000017',
      ayuda: 'Cuenta con e-chequera vigente. El librador debe ser su titular.',
    },
    {
      nombre: 'caracter',
      etiqueta: 'Carácter',
      tipo: 'caracter',
      obligatorio: true,
      ayuda: "Solo los 'A la orden' se endosan.",
    },
    {
      nombre: 'tipoDocBeneficiario',
      etiqueta: 'Tipo de documento del beneficiario',
      tipo: 'tipodoc',
      obligatorio: true,
    },
    {
      nombre: 'nombreLibrador',
      etiqueta: 'Nombre del librador',
      tipo: 'texto',
      obligatorio: true,
      placeholder: 'Alfa S.R.L.',
    },
    {
      nombre: 'nombreBeneficiario',
      etiqueta: 'Nombre del beneficiario',
      tipo: 'texto',
      obligatorio: true,
      placeholder: 'Beta S.A.',
      ayuda: 'Usá la lupa para validar el documento y autocompletarlo.',
    },
    {
      nombre: 'concepto',
      etiqueta: 'Concepto',
      tipo: 'texto',
      obligatorio: false,
      placeholder: 'Pago a proveedores',
    },
    {
      nombre: 'motivo',
      etiqueta: 'Motivo',
      tipo: 'texto',
      obligatorio: false,
    },
    {
      nombre: 'referencia',
      etiqueta: 'Referencia',
      tipo: 'texto',
      obligatorio: false,
      placeholder: 'FAC-123',
    },
    {
      nombre: 'emailNotificacion',
      etiqueta: 'Email de aviso al beneficiario',
      tipo: 'texto',
      obligatorio: false,
      placeholder: 'cobros@demo.local',
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

    if (!esCbuValido(request.cbuEmisor)) {
      errores.cbuEmisor =
        'El CBU de la cuenta de débito debe tener 22 dígitos con verificadores válidos.';
    }

    if (request.caracter !== 'AlaOrden' && request.caracter !== 'NoAlaOrden') {
      errores.caracter = "El carácter debe ser 'A la orden' o 'No a la orden'.";
    }

    if (!esTipoDocumento(request.tipoDocBeneficiario)) {
      errores.tipoDocBeneficiario = "El tipo de documento debe ser 'CUIT', 'CUIL' o 'CDI'.";
    } else if (!esDocumentoValido(request.tipoDocBeneficiario, request.cuitBeneficiario)) {
      errores.cuitBeneficiario = `El documento no es válido para el tipo ${request.tipoDocBeneficiario}.`;
    }

    if (!esNombreValido(request.nombreLibrador)) {
      errores.nombreLibrador = 'El nombre del librador es obligatorio (hasta 120 caracteres).';
    }

    if (!esNombreValido(request.nombreBeneficiario)) {
      errores.nombreBeneficiario = 'El nombre del beneficiario es obligatorio (hasta 120 caracteres).';
    }

    validarGestion(request.concepto, 60, 'concepto', errores);
    validarGestion(request.motivo, 280, 'motivo', errores);
    validarGestion(request.referencia, 60, 'referencia', errores);
    validarGestion(request.emailNotificacion, 160, 'emailNotificacion', errores, true);

    return { ...errores, ...validarComunes(request, hoy) };
  },

  construir(valores: Record<string, string>): CrearEcheqRequest {
    return {
      cbuEmisor: (valores.cbuEmisor ?? '').trim(),
      caracter: valores.caracter === 'NoAlaOrden' ? 'NoAlaOrden' : 'AlaOrden',
      tipoDocBeneficiario: esTipoDocumento(valores.tipoDocBeneficiario)
        ? valores.tipoDocBeneficiario
        : 'CUIT',
      nombreLibrador: (valores.nombreLibrador ?? '').trim(),
      nombreBeneficiario: (valores.nombreBeneficiario ?? '').trim(),
      concepto: textoOpcional(valores.concepto),
      motivo: textoOpcional(valores.motivo),
      referencia: textoOpcional(valores.referencia),
      emailNotificacion: textoOpcional(valores.emailNotificacion),
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
