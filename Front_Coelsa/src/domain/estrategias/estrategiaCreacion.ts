// Espejo de ICrearInstrumentoStrategy del backend (RF-01 ↔ RF-F03):
// cada tipo de instrumento define sus campos, etiquetas y validaciones.
import type { CrearChequeFisicoRequest, CrearEcheqRequest } from '../tipos';
import type { ErroresCampo } from '../validacionesInstrumento';

export type TipoInstrumentoForm = 'ChequeFisico' | 'Echeq';

export interface CampoFormulario<TRequest> {
  nombre: keyof TRequest & string;
  etiqueta: string;
  tipo: 'texto' | 'numero' | 'fecha' | 'moneda';
  obligatorio: boolean;
  placeholder?: string;
  ayuda?: string;
}

export interface EstrategiaCreacion<TRequest> {
  readonly tipo: TipoInstrumentoForm;
  readonly titulo: string;
  readonly descripcion: string;
  readonly identificadorEtiqueta: string;
  readonly campos: readonly CampoFormulario<TRequest>[];
  /** Devuelve errores por campo; objeto vacío = request válido. */
  validar(request: TRequest, hoy?: string): ErroresCampo;
}

export type EstrategiaChequeFisico = EstrategiaCreacion<CrearChequeFisicoRequest>;
export type EstrategiaEcheq = EstrategiaCreacion<CrearEcheqRequest>;
