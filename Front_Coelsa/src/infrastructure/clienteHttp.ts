// Cliente HTTP único de la app (RNF-F10): fetch + timeout + ProblemDetails
// RFC 7807 en español (RNF-07 del backend) + reintento único en 429 respetando
// Retry-After (RF-08 del backend).

const BASE_URL: string = import.meta.env.VITE_API_URL ?? '';

const TIMEOUT_POR_DEFECTO = 15_000;
const REINTENTO_429_MAX_ESPERA = 5_000;

/** Error uniforme de la API: toda falla HTTP o de red llega a la UI como ErrorCoelsa. */
export class ErrorCoelsa extends Error {
  /** Código HTTP; 0 => error de red o timeout. */
  readonly estadoHttp: number;
  readonly titulo: string;
  readonly detalle?: string;
  readonly esRed: boolean;

  constructor(estadoHttp: number, titulo: string, detalle?: string, esRed = false) {
    super(detalle ?? titulo);
    this.name = 'ErrorCoelsa';
    this.estadoHttp = estadoHttp;
    this.titulo = titulo;
    this.detalle = detalle;
    this.esRed = esRed;
  }
}

export interface RespuestaHttp<T> {
  datos: T;
  cabeceras: Headers;
}

export interface OpcionesPeticion {
  metodo?: 'GET' | 'POST' | 'PATCH' | 'DELETE';
  cuerpo?: unknown;
  cabeceras?: Record<string, string>;
  senal?: AbortSignal;
  timeoutMs?: number;
}

const TITULOS_POR_ESTADO: Record<number, string> = {
  400: 'Solicitud inválida',
  404: 'Recurso no encontrado',
  409: 'Conflicto',
  422: 'Transición de estado inválida',
  429: 'Demasiadas solicitudes',
  500: 'Error interno',
};

/** Retry-After en segundos o HTTP-date; null si está ausente o ilegible. */
function parseRetryAfter(valor: string | null): number | null {
  if (!valor) return null;
  const segundos = Number(valor);
  if (Number.isFinite(segundos) && segundos >= 0) return segundos * 1000;
  const fecha = Date.parse(valor);
  return Number.isNaN(fecha) ? null : Math.max(0, fecha - Date.now());
}

function demorar(ms: number): Promise<void> {
  return new Promise((resolver) => setTimeout(resolver, ms));
}

async function aErrorCoelsa(respuesta: Response): Promise<ErrorCoelsa> {
  let titulo = TITULOS_POR_ESTADO[respuesta.status] ?? `Error HTTP ${respuesta.status}`;
  let detalle: string | undefined;

  try {
    const cuerpo = (await respuesta.json()) as { title?: string; detail?: string };
    if (cuerpo?.title) titulo = cuerpo.title;
    if (cuerpo?.detail) detalle = cuerpo.detail;
  } catch {
    // Sin cuerpo JSON: nos quedamos con el título por estado.
  }

  return new ErrorCoelsa(respuesta.status, titulo, detalle);
}

async function ejecutar<T>(
  ruta: string,
  opciones: OpcionesPeticion,
  reintentar429: boolean,
): Promise<RespuestaHttp<T>> {
  const controlador = new AbortController();
  const timeout = setTimeout(() => controlador.abort(), opciones.timeoutMs ?? TIMEOUT_POR_DEFECTO);

  if (opciones.senal) {
    if (opciones.senal.aborted) controlador.abort();
    else opciones.senal.addEventListener('abort', () => controlador.abort(), { once: true });
  }

  try {
    const respuesta = await fetch(`${BASE_URL}${ruta}`, {
      method: opciones.metodo ?? 'GET',
      headers: {
        ...(opciones.cuerpo !== undefined ? { 'Content-Type': 'application/json' } : {}),
        ...opciones.cabeceras,
      },
      body: opciones.cuerpo !== undefined ? JSON.stringify(opciones.cuerpo) : undefined,
      signal: controlador.signal,
    });

    if (respuesta.ok) {
      if (respuesta.status === 204) {
        return { datos: null as T, cabeceras: respuesta.headers };
      }
      const texto = await respuesta.text();
      return { datos: (texto ? JSON.parse(texto) : null) as T, cabeceras: respuesta.headers };
    }

    // RF-08: un único reintento automático si la API pide esperar un rato razonable.
    if (respuesta.status === 429 && reintentar429) {
      const espera = parseRetryAfter(respuesta.headers.get('retry-after'));
      if (espera !== null && espera <= REINTENTO_429_MAX_ESPERA) {
        await demorar(espera);
        return ejecutar<T>(ruta, opciones, false);
      }
    }

    throw await aErrorCoelsa(respuesta);
  } catch (error) {
    // Las cancelaciones de TanStack Query no son errores de la app: se propagan tal cual.
    if (opciones.senal?.aborted) throw error;
    if (error instanceof ErrorCoelsa) throw error;
    throw new ErrorCoelsa(
      0,
      'Error de red',
      'No se pudo conectar con la API. Intente nuevamente.',
      true,
    );
  } finally {
    clearTimeout(timeout);
  }
}

export function pedir<T>(ruta: string, opciones: OpcionesPeticion = {}): Promise<RespuestaHttp<T>> {
  return ejecutar<T>(ruta, opciones, true);
}
