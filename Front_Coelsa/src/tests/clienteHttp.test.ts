// Tests del cliente HTTP: ProblemDetails RFC 7807, 429 con Retry-After,
// timeout y errores de red (espejo de RF-07/RNF-07 y RF-08 del backend).
import { describe, it, expect, vi, afterEach } from 'vitest';
import { pedir, ErrorCoelsa } from '../infrastructure/clienteHttp';

function respuestaJson(cuerpo: unknown, estado = 200, cabeceras: Record<string, string> = {}) {
  return new Response(JSON.stringify(cuerpo), {
    status: estado,
    headers: { 'Content-Type': 'application/problem+json', ...cabeceras },
  });
}

/** Ejecuta una petición esperando que falle y devuelve el ErrorCoelsa. */
async function capturarError(promesa: Promise<unknown>): Promise<ErrorCoelsa> {
  try {
    await promesa;
  } catch (e) {
    return e as ErrorCoelsa;
  }
  throw new Error('Se esperaba un ErrorCoelsa pero la petición tuvo éxito');
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('pedir — éxito', () => {
  it('parsea el JSON de una respuesta 200 y expone las cabeceras', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(respuestaJson({ hola: 'mundo' }, 200, { 'X-Prueba': '1' }));
    vi.stubGlobal('fetch', fetchMock);

    const r = await pedir<{ hola: string }>('/api/v1/cheques');

    expect(r.datos).toEqual({ hola: 'mundo' });
    expect(r.cabeceras.get('X-Prueba')).toBe('1');
    expect(fetchMock).toHaveBeenCalledOnce();
  });

  it('devuelve datos null en 204 (baja lógica)', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 204 })));
    const r = await pedir<void>('/api/v1/cheques/123', { metodo: 'DELETE' });
    expect(r.datos).toBeNull();
  });
});

describe('pedir — ProblemDetails (RFC 7807)', () => {
  it('convierte un 400 con problem+json en ErrorCoelsa con título y detalle', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        respuestaJson(
          {
            type: 'https://tools.ietf.org/html/rfc7807',
            title: 'Solicitud inválida',
            status: 400,
            detail: 'El parámetro es obligatorio.',
          },
          400,
        ),
      ),
    );

    const error = await capturarError(pedir('/api/v1/cheques'));

    expect(error).toBeInstanceOf(ErrorCoelsa);
    expect(error.estadoHttp).toBe(400);
    expect(error.titulo).toBe('Solicitud inválida');
    expect(error.message).toBe('El parámetro es obligatorio.');
    expect(error.esRed).toBe(false);
  });

  it('sin cuerpo JSON usa el título por defecto del estado', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response('Server Error', { status: 500 })),
    );

    const error = await capturarError(pedir('/health'));

    expect(error.estadoHttp).toBe(500);
    expect(error.titulo).toBe('Error interno');
  });
});

describe('pedir — rate limit 429 (RF-08)', () => {
  it('reintenta una única vez cuando Retry-After es corto', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(
        respuestaJson({ title: 'Demasiadas solicitudes', status: 429 }, 429, {
          'Retry-After': '1',
        }),
      )
      .mockResolvedValueOnce(respuestaJson({ ok: true }));
    vi.stubGlobal('fetch', fetchMock);

    const r = await pedir<{ ok: boolean }>('/api/v1/cheques');

    expect(r.datos).toEqual({ ok: true });
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it('no reintenta si Retry-After es mayor al máximo tolerado', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      respuestaJson({ title: 'Demasiadas solicitudes', status: 429 }, 429, {
        'Retry-After': '30',
      }),
    );
    vi.stubGlobal('fetch', fetchMock);

    const error = await capturarError(pedir('/api/v1/cheques'));

    expect(error.estadoHttp).toBe(429);
    expect(fetchMock).toHaveBeenCalledOnce();
  });

  it('un segundo 429 ya no reintenta y propaga el error', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      respuestaJson({ title: 'Demasiadas solicitudes', status: 429 }, 429, {
        'Retry-After': '0',
      }),
    );
    vi.stubGlobal('fetch', fetchMock);

    const error = await capturarError(pedir('/api/v1/cheques'));

    expect(error.estadoHttp).toBe(429);
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });
});

describe('pedir — fallas de transporte', () => {
  it('un fallo de fetch se reporta como error de red', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));

    const error = await capturarError(pedir('/api/v1/cheques'));

    expect(error.esRed).toBe(true);
    expect(error.estadoHttp).toBe(0);
  });

  it('el timeout aborta la petición y se reporta como error de red', async () => {
    // Mock que respeta la señal, como el fetch real: rechaza con AbortError al abortar.
    vi.stubGlobal(
      'fetch',
      vi.fn().mockImplementation((_url: string, init?: { signal?: AbortSignal }) => {
        return new Promise((_resolver, rechazar) => {
          init?.signal?.addEventListener('abort', () =>
            rechazar(new DOMException('The operation was aborted.', 'AbortError')),
          );
        });
      }),
    );

    const error = await capturarError(pedir('/api/v1/cheques', { timeoutMs: 20 }));

    expect(error.esRed).toBe(true);
  });
});
