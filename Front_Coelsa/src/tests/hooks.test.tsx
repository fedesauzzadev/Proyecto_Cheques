// Tests de los hooks de application con puertos falsos: verifican que los hooks
// llamen al puerto correcto, que las queries no disparen con CUIT inválido y que
// las mutaciones invaliden el tipo (espejo del versionado de caché del backend).
import { describe, it, expect, vi } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { useListarInstrumentos } from '../application/hooks/useListarInstrumentos';
import { useObtenerInstrumento } from '../application/hooks/useObtenerInstrumento';
import { useCrearInstrumento } from '../application/hooks/useCrearInstrumento';
import { useCambiarEstado } from '../application/hooks/useCambiarEstado';
import { useBajaLogica } from '../application/hooks/useBajaLogica';
import { useSaludBackend } from '../application/hooks/useSaludBackend';
import { claveDetalle, claveListado } from '../application/clavesConsulta';
import type { IPuertoInstrumentos, IPuertoSalud } from '../application/puertos';

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
  fechaDiferimiento: null,
  fechaCreacion: '2026-09-05T10:00:00Z',
};

const echeq = {
  identificador: 'ABCDEFGHIJK',
  tipo: 'Echeq',
  cmc7: '011000114250000123400001234567',
  desgloseCmc7: {
    banco: '011',
    sucursal: '0001',
    codigoPostal: '1425',
    numeroCheque: '00001234',
    numeroCuenta: '00001234567',
  },
  cuitLibrador: '20123456786',
  cuitBeneficiario: '30511222334',
  monto: 250000,
  moneda: 'D',
  fechaEmision: '2026-09-05',
  fechaDiferimiento: null,
  estado: 'Emitido',
  motivoRechazo: null,
  cantidadEndosos: 0,
  fechaCreacion: '2026-09-05T10:00:00Z',
};

const paginaVacia = { items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 };

function crearPuertoFalso() {
  return {
    listarCheques: vi.fn().mockResolvedValue(paginaVacia),
    listarEcheqs: vi.fn().mockResolvedValue(paginaVacia),
    obtenerCheque: vi.fn().mockResolvedValue(cheque),
    obtenerEcheq: vi.fn().mockResolvedValue(echeq),
    crearCheque: vi.fn(),
    crearEcheq: vi.fn(),
    cambiarEstadoCheque: vi.fn(),
    cambiarEstadoEcheq: vi.fn(),
    eliminarCheque: vi.fn().mockResolvedValue(undefined),
    eliminarEcheq: vi.fn().mockResolvedValue(undefined),
  } satisfies IPuertoInstrumentos;
}

function crearEnvoltorio() {
  const cliente = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  function Envoltorio({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={cliente}>{children}</QueryClientProvider>;
  }
  return { Envoltorio, cliente };
}

describe('useListarInstrumentos (RF-F01)', () => {
  it('llama al puerto con los filtros y expone la página', async () => {
    const puerto = crearPuertoFalso();
    const { Envoltorio } = crearEnvoltorio();

    const { result } = renderHook(
      () =>
        useListarInstrumentos(
          'ChequeFisico',
          { cuit: '20123456786', page: 2, pageSize: 25 },
          puerto,
        ),
      { wrapper: Envoltorio },
    );

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(puerto.listarCheques).toHaveBeenCalledWith(
      { cuit: '20123456786', page: 2, pageSize: 25 },
      expect.any(AbortSignal),
    );
    expect(result.current.data?.totalCount).toBe(0);
  });

  it('no dispara la query con CUIT inválido', async () => {
    const puerto = crearPuertoFalso();
    const { Envoltorio } = crearEnvoltorio();

    const { result } = renderHook(
      () => useListarInstrumentos('Echeq', { cuit: '123', page: 1, pageSize: 10 }, puerto),
      { wrapper: Envoltorio },
    );

    await act(async () => {});
    expect(puerto.listarEcheqs).not.toHaveBeenCalled();
    expect(result.current.isPending).toBe(true);
    expect(result.current.fetchStatus).toBe('idle');
  });
});

describe('useObtenerInstrumento (RF-F02)', () => {
  it('obtiene el detalle por identificador de negocio', async () => {
    const puerto = crearPuertoFalso();
    const { Envoltorio } = crearEnvoltorio();

    const { result } = renderHook(() => useObtenerInstrumento('Echeq', 'ABCDEFGHIJK', puerto), {
      wrapper: Envoltorio,
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(puerto.obtenerEcheq).toHaveBeenCalledWith('ABCDEFGHIJK', expect.any(AbortSignal));
    expect(result.current.data?.identificador).toBe('ABCDEFGHIJK');
  });
});

describe('useCrearInstrumento (RF-F04)', () => {
  it('crea con request + key e invalida el tipo', async () => {
    const puerto = crearPuertoFalso();
    puerto.crearCheque.mockResolvedValue({ respuesta: cheque, esReplay: false });
    const { Envoltorio, cliente } = crearEnvoltorio();

    const clave = claveListado('ChequeFisico', '20123456786', 1, 10);
    cliente.setQueryData(clave, { ...paginaVacia, totalCount: 5 });

    const { result } = renderHook(() => useCrearInstrumento('ChequeFisico', puerto), {
      wrapper: Envoltorio,
    });

    await act(async () => {
      result.current.mutate({
        request: {
          cmc7: cheque.identificador,
          cuitLibrador: '20123456786',
          cuitBeneficiario: '27876543219',
          monto: 150000.5,
          moneda: 'P',
          fechaEmision: '2026-09-05',
        },
        idempotencyKey: 'guid-de-intento-1',
      });
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(puerto.crearCheque).toHaveBeenCalledWith(
      expect.objectContaining({ cmc7: cheque.identificador }),
      'guid-de-intento-1',
    );
    expect(result.current.data?.esReplay).toBe(false);
    expect(cliente.getQueryState(clave)?.isInvalidated).toBe(true);
  });
});

describe('useCambiarEstado (RF-F05)', () => {
  it('aplica el PATCH e invalida el detalle del tipo', async () => {
    const puerto = crearPuertoFalso();
    puerto.cambiarEstadoEcheq.mockResolvedValue({
      ...echeq,
      estado: 'Rechazado',
      motivoRechazo: 11,
    });
    const { Envoltorio, cliente } = crearEnvoltorio();

    const clave = claveDetalle('Echeq', 'ABCDEFGHIJK');
    cliente.setQueryData(clave, echeq);

    const { result } = renderHook(() => useCambiarEstado('Echeq', puerto), { wrapper: Envoltorio });

    await act(async () => {
      result.current.mutate({
        identificador: 'ABCDEFGHIJK',
        request: { estado: 'Rechazado', motivoRechazo: 11 },
      });
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(puerto.cambiarEstadoEcheq).toHaveBeenCalledWith('ABCDEFGHIJK', {
      estado: 'Rechazado',
      motivoRechazo: 11,
    });
    expect(cliente.getQueryState(clave)?.isInvalidated).toBe(true);
  });
});

describe('useBajaLogica (RF-F06)', () => {
  it('emite el DELETE e invalida el listado del tipo', async () => {
    const puerto = crearPuertoFalso();
    const { Envoltorio, cliente } = crearEnvoltorio();

    const clave = claveListado('ChequeFisico', '20123456786', 1, 10);
    cliente.setQueryData(clave, { ...paginaVacia, totalCount: 3 });

    const { result } = renderHook(() => useBajaLogica('ChequeFisico', puerto), {
      wrapper: Envoltorio,
    });

    await act(async () => {
      result.current.mutate('060000114250000123400001234567');
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(puerto.eliminarCheque).toHaveBeenCalledWith('060000114250000123400001234567');
    expect(cliente.getQueryState(clave)?.isInvalidated).toBe(true);
  });
});

describe('useSaludBackend (RF-F08)', () => {
  it('consulta el estado del backend', async () => {
    const puerto: IPuertoSalud = {
      consultar: vi.fn().mockResolvedValue({
        status: 'Healthy',
        checks: { postgres: 'Healthy', redis: 'Healthy' },
      }),
    };
    const { Envoltorio } = crearEnvoltorio();

    const { result } = renderHook(() => useSaludBackend(puerto), { wrapper: Envoltorio });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(puerto.consultar).toHaveBeenCalledOnce();
    expect(result.current.data?.status).toBe('Healthy');
  });
});
