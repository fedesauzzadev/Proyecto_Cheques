import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { afterEach, describe, it, expect, vi } from 'vitest';
import App from '../App';

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('App', () => {
  it('muestra el encabezado de la consola y la navegación principal', () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify({
            status: 'Healthy',
            checks: { postgres: 'Healthy', redis: 'Healthy' },
          }),
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

    render(<App />, { wrapper: Envoltorio });

    expect(screen.getByRole('heading', { name: /consola de instrumentos/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Consultar' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Contratos' })).toHaveAttribute('href', '/contratos');
  });
});
