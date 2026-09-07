// Tests de los hooks de Fase A (aceptación, endosos, devoluciones) con puertos falsos.
import { describe, it, expect, vi } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { useAceptarEcheq } from '../application/hooks/useAceptarEcheq';
import { useEndosos, useProponerEndoso, useResolverEndoso } from '../application/hooks/useEndosos';
import {
  useDevoluciones,
  useSolicitarDevolucion,
  useResolverDevolucion,
} from '../application/hooks/useDevoluciones';
import { claveEndosos } from '../application/clavesConsulta';
import type {
  IPuertoAceptacion,
  IPuertoDevoluciones,
  IPuertoEndosos,
} from '../application/puertos';

function crearEnvoltorio() {
  const cliente = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  function Envoltorio({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={cliente}>{children}</QueryClientProvider>;
  }
  return { Envoltorio, cliente };
}

const endoso = {
  orden: 1,
  cuitEndosante: '20123456786',
  cuitEndosatario: '30511222334',
  estado: 'Propuesto' as const,
  fechaCreacion: '2026-09-06T10:00:00Z',
};

const devolucion = {
  numero: 1,
  cuitSolicitante: '20123456786',
  motivo: 'Devolución de prueba',
  estado: 'Solicitada' as const,
  fechaCreacion: '2026-09-06T10:00:00Z',
};

describe('useAceptarEcheq (RF-F10)', () => {
  it('acepta e invalida el tipo', async () => {
    const aceptar = vi.fn().mockResolvedValue({ identificador: 'ABCDEFGHIJK', estado: 'Emitido' });
    const puerto: IPuertoAceptacion = { aceptar };
    const { Envoltorio, cliente } = crearEnvoltorio();
    cliente.setQueryData(['instrumentos', 'Echeq', 'listado', '20123456786', 1, 10], {
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
    });

    const { result } = renderHook(() => useAceptarEcheq(puerto), { wrapper: Envoltorio });

    await act(async () => {
      result.current.mutate({ idecheq: 'ABCDEFGHIJK', aceptada: true });
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(aceptar).toHaveBeenCalledWith('ABCDEFGHIJK', true, undefined);
    expect(result.current.data?.estado).toBe('Emitido');
  });

  it('repudia con motivo e invalida el tipo (D2)', async () => {
    const aceptar = vi
      .fn()
      .mockResolvedValue({ identificador: 'ABCDEFGHIJK', estado: 'Repudiado' });
    const puerto: IPuertoAceptacion = { aceptar };
    const { Envoltorio } = crearEnvoltorio();

    const { result } = renderHook(() => useAceptarEcheq(puerto), { wrapper: Envoltorio });

    await act(async () => {
      result.current.mutate({ idecheq: 'ABCDEFGHIJK', aceptada: false, motivo: 'No la pedí' });
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(aceptar).toHaveBeenCalledWith('ABCDEFGHIJK', false, 'No la pedí');
  });
});

describe('useEndosos (RF-F11)', () => {
  function crearPuerto(overrides: Partial<IPuertoEndosos> = {}): IPuertoEndosos {
    return {
      listar: vi.fn().mockResolvedValue([endoso]),
      proponer: vi.fn().mockResolvedValue(endoso),
      resolver: vi.fn().mockResolvedValue({ ...endoso, estado: 'Vigente' }),
      anular: vi.fn().mockResolvedValue(undefined),
      ...overrides,
    };
  }

  it('lista la cadena del echeq', async () => {
    const puerto = crearPuerto();
    const { Envoltorio } = crearEnvoltorio();

    const { result } = renderHook(() => useEndosos('ABCDEFGHIJK', puerto), { wrapper: Envoltorio });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(puerto.listar).toHaveBeenCalledWith('ABCDEFGHIJK', expect.any(AbortSignal));
    expect(result.current.data).toHaveLength(1);
  });

  it('propone e invalida la cadena', async () => {
    const puerto = crearPuerto();
    const { Envoltorio, cliente } = crearEnvoltorio();
    const clave = claveEndosos('ABCDEFGHIJK');
    cliente.setQueryData(clave, []);

    const { result } = renderHook(() => useProponerEndoso(puerto), { wrapper: Envoltorio });

    await act(async () => {
      result.current.mutate({ idecheq: 'ABCDEFGHIJK', cuitEndosatario: '30511222334' });
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(puerto.proponer).toHaveBeenCalledWith('ABCDEFGHIJK', '30511222334');
    expect(cliente.getQueryState(clave)?.isInvalidated).toBe(true);
  });

  it('resuelve con admisión e identidad', async () => {
    const puerto = crearPuerto();
    const { Envoltorio } = crearEnvoltorio();

    const { result } = renderHook(() => useResolverEndoso(puerto), { wrapper: Envoltorio });

    await act(async () => {
      result.current.mutate({
        idecheq: 'ABCDEFGHIJK',
        orden: 1,
        admitido: true,
        cuit: '30511222334',
      });
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(puerto.resolver).toHaveBeenCalledWith('ABCDEFGHIJK', 1, true, '30511222334');
    expect(result.current.data?.estado).toBe('Vigente');
  });
});

describe('useDevoluciones (RF-F12)', () => {
  function crearPuerto(overrides: Partial<IPuertoDevoluciones> = {}): IPuertoDevoluciones {
    return {
      listar: vi.fn().mockResolvedValue([devolucion]),
      solicitar: vi.fn().mockResolvedValue(devolucion),
      resolver: vi.fn().mockResolvedValue({ ...devolucion, estado: 'Aceptada' }),
      anular: vi.fn().mockResolvedValue(undefined),
      ...overrides,
    };
  }

  it('lista los pedidos del echeq', async () => {
    const puerto = crearPuerto();
    const { Envoltorio } = crearEnvoltorio();

    const { result } = renderHook(() => useDevoluciones('ABCDEFGHIJK', puerto), {
      wrapper: Envoltorio,
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data?.[0].numero).toBe(1);
  });

  it('solicita con CUIT y motivo', async () => {
    const puerto = crearPuerto();
    const { Envoltorio } = crearEnvoltorio();

    const { result } = renderHook(() => useSolicitarDevolucion(puerto), { wrapper: Envoltorio });

    await act(async () => {
      result.current.mutate({
        idecheq: 'ABCDEFGHIJK',
        cuitSolicitante: '20123456786',
        motivo: 'X',
      });
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(puerto.solicitar).toHaveBeenCalledWith('ABCDEFGHIJK', '20123456786', 'X');
  });

  it('resuelve con aceptación e identidad del tenedor', async () => {
    const puerto = crearPuerto();
    const { Envoltorio } = crearEnvoltorio();

    const { result } = renderHook(() => useResolverDevolucion(puerto), { wrapper: Envoltorio });

    await act(async () => {
      result.current.mutate({
        idecheq: 'ABCDEFGHIJK',
        numero: 1,
        aceptada: false,
        cuitResolutor: '30511222334',
      });
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(puerto.resolver).toHaveBeenCalledWith('ABCDEFGHIJK', 1, false, '30511222334');
  });
});
