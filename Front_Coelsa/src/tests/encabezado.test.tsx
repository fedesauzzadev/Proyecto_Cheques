import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import Encabezado from '../presentation/layout/Encabezado';

afterEach(() => {
  vi.unstubAllGlobals();
});

function renderEncabezado() {
  vi.stubGlobal(
    'fetch',
    vi
      .fn()
      .mockResolvedValue(
        new Response(
          JSON.stringify({ status: 'Healthy', checks: { postgres: 'Healthy', redis: 'Healthy' } }),
          { status: 200 },
        ),
      ),
  );

  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Envoltorio({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={cliente}>
        <MemoryRouter>{children}</MemoryRouter>
      </QueryClientProvider>
    );
  }
  return render(<Encabezado />, { wrapper: Envoltorio });
}

describe('Encabezado móvil', () => {
  it('abre y cierra el menú hamburguesa', async () => {
    const usuario = userEvent.setup();
    renderEncabezado();

    const boton = screen.getByRole('button', { name: /abrir menú/i });
    expect(screen.queryByRole('navigation', { name: /principal móvil/i })).not.toBeInTheDocument();

    await usuario.click(boton);

    expect(screen.getByRole('button', { name: /cerrar menú/i })).toBeInTheDocument();
    const movil = screen.getByRole('navigation', { name: /principal móvil/i });
    expect(movil).toBeInTheDocument();

    await usuario.click(boton);
    expect(screen.queryByRole('navigation', { name: /principal móvil/i })).not.toBeInTheDocument();
  });

  it('al elegir un enlace se cierra el menú', async () => {
    const usuario = userEvent.setup();
    renderEncabezado();

    await usuario.click(screen.getByRole('button', { name: /abrir menú/i }));
    const enlaces = screen.getAllByRole('link', { name: 'Contratos' });
    await usuario.click(enlaces[enlaces.length - 1]);

    expect(screen.queryByRole('navigation', { name: /principal móvil/i })).not.toBeInTheDocument();
  });
});
