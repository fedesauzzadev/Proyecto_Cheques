// Espejo de Coelsa.Domain/Validaciones/ValidacionesInstrumento.cs (SPEC 5.1 / 5.2).
// Devuelve errores por campo (vacío = válido) para poder mostrarlos en los formularios.
import { esCuitValido } from './validadorCuit';
import { esTransicionValida, destinosDesde } from './transiciones';
import type { EstadoInstrumento, MotivoRechazo } from './tipos';

export type ErroresCampo = Partial<Record<string, string>>;

export interface DatosComunesInstrumento {
  cuitLibrador: string;
  cuitBeneficiario: string;
  monto: number;
  fechaEmision: string; // YYYY-MM-DD
  fechaDiferimiento?: string | null;
}

function hoyMasUnDia(hoy?: string): string {
  const base = hoy ?? new Date().toISOString().slice(0, 10);
  const fecha = new Date(`${base}T00:00:00Z`);
  fecha.setUTCDate(fecha.getUTCDate() + 1);
  return fecha.toISOString().slice(0, 10);
}

export function validarComunes(datos: DatosComunesInstrumento, hoy?: string): ErroresCampo {
  const errores: Record<string, string> = {};

  if (!esCuitValido(datos.cuitLibrador)) {
    errores.cuitLibrador =
      `El CUIT/CUIL del librador '${datos.cuitLibrador}' no es válido ` +
      '(se espera 11 dígitos con verificador módulo 11).';
  }

  if (!esCuitValido(datos.cuitBeneficiario)) {
    errores.cuitBeneficiario =
      `El CUIT/CUIL del beneficiario '${datos.cuitBeneficiario}' no es válido ` +
      '(se espera 11 dígitos con verificador módulo 11).';
  }

  if (!(datos.monto > 0)) {
    errores.monto = 'El monto debe ser mayor a cero.';
  }

  const limiteEmision = hoyMasUnDia(hoy);
  if (datos.fechaEmision > limiteEmision) {
    errores.fechaEmision = `La fecha de emisión ${datos.fechaEmision} no puede ser mayor a ${limiteEmision}.`;
  }

  if (datos.fechaDiferimiento && datos.fechaDiferimiento < datos.fechaEmision) {
    errores.fechaDiferimiento = `La fecha de diferimiento ${datos.fechaDiferimiento} no puede ser anterior a la fecha de emisión ${datos.fechaEmision}.`;
  }

  return errores;
}

/** Espejo de ValidarEstadoCambio: null = válido; string = mensaje de error. */
export function validarCambioEstado(
  actual: EstadoInstrumento,
  nuevo: EstadoInstrumento,
  motivo: MotivoRechazo | null | undefined,
): string | null {
  if (!esTransicionValida(actual, nuevo)) {
    const destinos = destinosDesde(actual);
    const lista = destinos.length === 0 ? 'ninguno (estado terminal)' : destinos.join(', ');
    return `No se puede pasar del estado ${actual} al estado ${nuevo}. Destinos válidos desde ${actual}: ${lista}.`;
  }

  if (nuevo === 'Rechazado' && (motivo === null || motivo === undefined)) {
    return 'El estado Rechazado exige un motivo de rechazo.';
  }

  if (nuevo !== 'Rechazado' && motivo !== null && motivo !== undefined) {
    return 'Solo el estado Rechazado admite un motivo de rechazo.';
  }

  return null;
}
