// Tests de la página de consulta (RF-F01): validación de CUIT client-side,
// tabla paginada, estados vacío/error y cambio de tipo por pestañas.
import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import PaginaConsulta from '../presentation/features/consulta/PaginaConsulta';
import { ErrorCoelsa } from '../infrastructure/clienteHttp';
import type { IPuertoInstrumentos } from '../application/puertos';

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

const CUIT_VALIDO = '20123456786';

function crearPuertoFalso() {
  return {
    listarCheques: vi.fn(),
    listarEcheqs: vi.fn(),
    obtenerCheque: vi.fn(),
    obtenerEcheq: vi.fn(),
    crearCheque: vi.fn(),
    crearEcheq: vi.fn(),
    cambiarEstadoCheque: vi.fn(),
    cambiarEstadoEcheq: vi.fn(),
    eliminarCheque: vi.fn(),
    eliminarEcheq: vi.fn(),
  } satisfies IPuertoInstrumentos;
}

function renderPagina(puerto: IPuertoInstrumentos) {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Envoltorio({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={cliente}>
        <MemoryRouter>{children}</MemoryRouter>
      </QueryClientProvider>
    );
  }
  return render(<PaginaConsulta puerto={puerto} />, { wrapper: Envoltorio });
}

async function buscarCuit(usuario: ReturnType<typeof userEvent.setup>, cuit: string) {
  await usuario.type(screen.getByLabelText(/cuit/i), cuit);
  await usuario.click(screen.getByRole('button', { name: /buscar/i }));
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('PaginaConsulta (RF-F01)', () => {
  it('informa CUIT inválido sin llamar a la API', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    renderPagina(puerto);

    await buscarCuit(usuario, '123');

    expect(await screen.findByRole('alert')).toHaveTextContent(/11 dígitos/);
    expect(puerto.listarCheques).not.toHaveBeenCalled();
  });

  it('busca por CUIT válido y muestra la tabla con los datos', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    puerto.listarCheques.mockResolvedValue({
      items: [cheque],
      page: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1,
    });
    renderPagina(puerto);

    await buscarCuit(usuario, CUIT_VALIDO);

    expect(puerto.listarCheques).toHaveBeenCalledWith(
      { cuit: CUIT_VALIDO, page: 1, pageSize: 10 },
      expect.anything(),
    );
    expect(await screen.findByText(cheque.identificador)).toBeInTheDocument();
    // El CUIT también está en el input: se aserta la celda de la tabla.
    expect(screen.getByRole('cell', { name: CUIT_VALIDO })).toBeInTheDocument();
    expect(screen.getByText('Emitido')).toBeInTheDocument();
  });

  it('la pestaña Echeqs consulta el otro endpoint', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    puerto.listarEcheqs.mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
    });
    renderPagina(puerto);

    await usuario.click(screen.getByRole('tab', { name: /echeqs/i }));
    await buscarCuit(usuario, CUIT_VALIDO);

    expect(puerto.listarEcheqs).toHaveBeenCalledWith(
      { cuit: CUIT_VALIDO, page: 1, pageSize: 10 },
      expect.anything(),
    );
    expect(puerto.listarCheques).not.toHaveBeenCalled();
  });

  it('sin resultados muestra el estado vacío con acción de creación', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    puerto.listarCheques.mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
    });
    renderPagina(puerto);

    await buscarCuit(usuario, CUIT_VALIDO);

    expect(
      await screen.findByText(new RegExp(`No hay cheques físicos para el CUIT`)),
    ).toBeInTheDocument();
    const accion = screen.getByRole('link', { name: /crear cheque físico/i });
    expect(accion).toHaveAttribute('href', '/nuevo/cheque');
  });

  it('ante un error del servidor muestra el detalle y permite reintentar', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    puerto.listarCheques
      .mockRejectedValueOnce(new ErrorCoelsa(500, 'Error interno', 'Falló todo.'))
      .mockResolvedValueOnce({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 });
    renderPagina(puerto);

    await buscarCuit(usuario, CUIT_VALIDO);

    expect(await screen.findByText('Falló todo.')).toBeInTheDocument();
    await usuario.click(screen.getByRole('button', { name: /reintentar/i }));
    expect(puerto.listarCheques).toHaveBeenCalledTimes(2);
  });

  it('navega a la página siguiente', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    puerto.listarCheques
      .mockResolvedValueOnce({
        items: [cheque],
        page: 1,
        pageSize: 10,
        totalCount: 2,
        totalPages: 2,
      })
      .mockResolvedValueOnce({
        items: [cheque],
        page: 2,
        pageSize: 10,
        totalCount: 2,
        totalPages: 2,
      });
    renderPagina(puerto);

    await buscarCuit(usuario, CUIT_VALIDO);
    expect(await screen.findByText(/Página 1 de 2/)).toBeInTheDocument();

    await usuario.click(screen.getByRole('button', { name: /página siguiente/i }));

    expect(puerto.listarCheques).toHaveBeenLastCalledWith(
      { cuit: CUIT_VALIDO, page: 2, pageSize: 10 },
      expect.anything(),
    );
    expect(await screen.findByText(/Página 2 de 2/)).toBeInTheDocument();
  });
});
