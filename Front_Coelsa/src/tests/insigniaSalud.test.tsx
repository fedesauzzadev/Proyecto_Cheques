import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import InsigniaSalud from '../presentation/layout/InsigniaSalud';
import { ErrorCoelsa } from '../infrastructure/clienteHttp';
import type { IPuertoSalud } from '../application/puertos';

function renderInsignia(puerto: IPuertoSalud) {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={cliente}>
      <InsigniaSalud puerto={puerto} />
    </QueryClientProvider>,
  );
}

describe('InsigniaSalud (RF-F08)', () => {
  it('muestra API operativa con el detalle de componentes', async () => {
    const puerto: IPuertoSalud = {
      consultar: vi.fn().mockResolvedValue({
        status: 'Healthy',
        checks: { postgres: 'Healthy', redis: 'Healthy' },
      }),
    };

    renderInsignia(puerto);

    // findByRole resolvería con el badge intermedio "Verificando…": se espera al texto final.
    await screen.findByText(/api operativa/i);
    const insignia = screen.getByText(/api operativa/i).closest('[role="status"]');
    expect(insignia).toHaveAttribute('title', expect.stringContaining('PostgreSQL'));
  });

  it('muestra API degradada en ámbar conceptual', async () => {
    const puerto: IPuertoSalud = {
      consultar: vi.fn().mockResolvedValue({
        status: 'Degraded',
        checks: { postgres: 'Healthy', redis: 'Unhealthy' },
      }),
    };

    renderInsignia(puerto);

    expect(await screen.findByText(/api degradada/i)).toBeInTheDocument();
  });

  it('muestra API no disponible si la consulta falla', async () => {
    const puerto: IPuertoSalud = {
      consultar: vi.fn().mockRejectedValue(new ErrorCoelsa(0, 'Error de red', undefined, true)),
    };

    renderInsignia(puerto);

    expect(await screen.findByText(/api no disponible/i)).toBeInTheDocument();
  });
});
