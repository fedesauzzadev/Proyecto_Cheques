// Tests de la página de cuentas (Fase B, espejo de RF-01b): buscar por CUIT,
// listar chequeras, crear cuenta con validación y solicitar chequera.
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import PaginaCuentas from '../presentation/features/cuentas/PaginaCuentas';
import type { IPuertoCuentas } from '../application/puertos';

const CBU = '0110001300000000000017';

const cuenta = {
  cbu: CBU,
  banco: '011',
  sucursal: '0001',
  numeroCuenta: '0000000000001',
  cuitTitular: '20123456786',
  nombreTitular: 'Alfa S.R.L.',
  moneda: 'P',
  fechaCreacion: '2026-09-07T10:00:00Z',
};

const chequera = {
  numero: 1,
  cantidadTotal: 50,
  proximoNumero: 8,
  disponibles: 43,
  estado: 'Vigente',
  fechaSolicitud: '2026-09-07T10:00:00Z',
  fechaHabilitacion: '2026-09-07T10:00:00Z',
};

function crearPuertoFalso() {
  return {
    crear: vi.fn(),
    listarPorCuit: vi.fn().mockResolvedValue([cuenta]),
    obtener: vi.fn(),
    solicitarChequera: vi.fn(),
    listarChequeras: vi.fn().mockResolvedValue([chequera]),
    buscarTitular: vi.fn(),
  } satisfies IPuertoCuentas;
}

function renderCuentas(puerto: IPuertoCuentas) {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Envoltorio({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={cliente}>
        <MemoryRouter>{children}</MemoryRouter>
      </QueryClientProvider>
    );
  }
  return render(<PaginaCuentas puerto={puerto} />, { wrapper: Envoltorio });
}

describe('PaginaCuentas (Fase B)', () => {
  it('con CUIT inválido muestra error y no llama a la API', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    renderCuentas(puerto);

    await usuario.type(screen.getByLabelText('CUIT/CUIL titular'), '123');
    await usuario.click(screen.getByRole('button', { name: /^buscar$/i }));

    expect(await screen.findByText(/módulo 11/)).toBeInTheDocument();
    expect(puerto.listarPorCuit).not.toHaveBeenCalled();
  });

  it('lista cuentas con sus chequeras y permite solicitar una nueva', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    puerto.solicitarChequera.mockResolvedValue({ respuesta: chequera, esReplay: false });
    renderCuentas(puerto);

    await usuario.type(screen.getByLabelText('CUIT/CUIL titular'), '20123456786');
    await usuario.click(screen.getByRole('button', { name: /^buscar$/i }));

    expect(await screen.findByText(CBU)).toBeInTheDocument();
    expect(screen.getByText(/43 disp\./)).toBeInTheDocument();

    await usuario.click(screen.getByRole('button', { name: /solicitar chequera/i }));
    expect(puerto.solicitarChequera).toHaveBeenCalledWith(CBU, expect.any(String));
  });

  it('crear cuenta con CBU inválido no llama a la API', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    renderCuentas(puerto);

    await usuario.type(screen.getByLabelText('CBU *'), '123');
    await usuario.type(screen.getByLabelText('CUIT/CUIL titular *'), '20123456786');
    await usuario.click(screen.getByRole('button', { name: /^crear cuenta$/i }));

    expect(await screen.findByText(/22 dígitos/)).toBeInTheDocument();
    expect(puerto.crear).not.toHaveBeenCalled();
  });

  it('crear cuenta envía el nombre del titular', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    puerto.crear.mockResolvedValue({ respuesta: cuenta, esReplay: false });
    renderCuentas(puerto);

    await usuario.type(screen.getByLabelText('CBU *'), CBU);
    await usuario.type(screen.getByLabelText('CUIT/CUIL titular *'), '20123456786');
    await usuario.type(screen.getByLabelText('Nombre del titular *'), 'Alfa S.R.L.');
    await usuario.click(screen.getByRole('button', { name: /^crear cuenta$/i }));

    expect(puerto.crear).toHaveBeenCalledWith(
      { cbu: CBU, cuitTitular: '20123456786', nombreTitular: 'Alfa S.R.L.', moneda: 'P' },
      expect.any(String),
    );
  });
});
