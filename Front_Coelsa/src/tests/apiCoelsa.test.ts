// Tests del adaptador apiCoelsa: URLs, métodos, headers de idempotencia
// y lectura del header Idempotent-Replay (RF-02 del backend).
import { describe, it, expect, vi, afterEach } from 'vitest';
import { apiInstrumentos, apiSalud } from '../infrastructure/apiCoelsa';

afterEach(() => {
  vi.unstubAllGlobals();
});

const cheque = {
  identificador: '060000114250000123400001234567',
  tipo: 'ChequeFisico',
  estado: 'Emitido',
  motivoRechazo: null,
  cuitLibrador: '20123456786',
  cuitBeneficiario: '27876543219',
  monto: 150000.5,
  moneda: 'P',
  fechaEmision: '2026-09-05',
  fechaVencimiento: '2026-10-05',
  fechaDiferimiento: null,
  fechaCreacion: '2026-09-05T10:00:00Z',
};

describe('apiInstrumentos', () => {
  it('listarCheques arma el query string paginado por CUIT', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(
        JSON.stringify({ items: [cheque], page: 1, pageSize: 10, totalCount: 1, totalPages: 1 }),
        {
          status: 200,
        },
      ),
    );
    vi.stubGlobal('fetch', fetchMock);

    const pagina = await apiInstrumentos.listarCheques({
      cuit: '20123456786',
      page: 2,
      pageSize: 25,
    });

    const [url] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toContain('/api/v1/cheques?');
    expect(url).toContain('cuit=20123456786');
    expect(url).toContain('page=2');
    expect(url).toContain('pageSize=25');
    expect(pagina.totalCount).toBe(1);
  });

  it('crearCheque envía POST con Idempotency-Key y detecta el replay', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify(cheque), {
        status: 201,
        headers: { 'Idempotent-Replay': 'true' },
      }),
    );
    vi.stubGlobal('fetch', fetchMock);

    const resultado = await apiInstrumentos.crearCheque(
      {
        cmc7: cheque.identificador,
        cuitLibrador: '20123456786',
        cuitBeneficiario: '27876543219',
        monto: 150000.5,
        moneda: 'P',
        fechaEmision: '2026-09-05',
        fechaVencimiento: '2026-10-05',
      },
      '11111111-2222-3333-4444-555555555555',
    );

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url.endsWith('/api/v1/cheques')).toBe(true);
    expect(init.method).toBe('POST');
    expect(new Headers(init.headers).get('Idempotency-Key')).toBe(
      '11111111-2222-3333-4444-555555555555',
    );
    expect(JSON.parse(init.body as string).cmc7).toBe(cheque.identificador);
    expect(resultado.esReplay).toBe(true);
    expect(resultado.respuesta.identificador).toBe(cheque.identificador);
  });

  it('cambiarEstadoCheque hace PATCH al subrecurso /estado', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(
        new Response(JSON.stringify({ ...cheque, estado: 'Depositado' }), { status: 200 }),
      );
    vi.stubGlobal('fetch', fetchMock);

    await apiInstrumentos.cambiarEstadoCheque(cheque.identificador, { estado: 'Depositado' });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url.endsWith(`/api/v1/cheques/${cheque.identificador}/estado`)).toBe(true);
    expect(init.method).toBe('PATCH');
    expect(JSON.parse(init.body as string)).toEqual({ estado: 'Depositado' });
  });

  it('eliminarCheque emite DELETE', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);

    await apiInstrumentos.eliminarCheque(cheque.identificador);

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url.endsWith(`/api/v1/cheques/${cheque.identificador}`)).toBe(true);
    expect(init.method).toBe('DELETE');
  });
});

describe('apiSalud', () => {
  it('consultar mapea el contrato del health check', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify({ status: 'Healthy', checks: { postgres: 'Healthy', redis: 'Healthy' } }),
          {
            status: 200,
          },
        ),
      ),
    );

    const salud = await apiSalud.consultar();

    expect(salud.status).toBe('Healthy');
    expect(salud.checks.postgres).toBe('Healthy');
    expect(salud.checks.redis).toBe('Healthy');
  });
});
