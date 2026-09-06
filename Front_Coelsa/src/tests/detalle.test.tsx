// Tests de la página de detalle (RF-F02/F05/F06): desglose, acciones según la
// máquina de estados, rechazo con motivo y baja con confirmación.
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import PaginaDetalle from '../presentation/features/detalle/PaginaDetalle';
import { ErrorCoelsa } from '../infrastructure/clienteHttp';
import type { IPuertoInstrumentos } from '../application/puertos';

const chequeEmitido = {
  identificador: '060000114250000123400001234567',
  tipo: 'ChequeFisico',
  desgloseCmc7: {
    banco: '060',
    sucursal: '0001',
    codigoPostal: '1425',
    numeroCheque: '00001234',
    numeroCuenta: '00001234567',
  },
  cuitLibrador: '20123456786',
  cuitBeneficiario: '27876543219',
  monto: 150000.5,
  moneda: 'P',
  fechaEmision: '2026-09-05',
  fechaVencimiento: '2026-10-05',
  fechaDiferimiento: null,
  estado: 'Emitido',
  motivoRechazo: null,
  fechaCreacion: '2026-09-05T10:00:00Z',
};

const chequeDepositado = { ...chequeEmitido, estado: 'Depositado' };

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
    eliminarCheque: vi.fn().mockResolvedValue(undefined),
    eliminarEcheq: vi.fn().mockResolvedValue(undefined),
  } satisfies IPuertoInstrumentos;
}

function renderDetalle(identificador: string, puerto: IPuertoInstrumentos) {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Envoltorio({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={cliente}>
        <MemoryRouter initialEntries={[`/cheques/${identificador}`]}>{children}</MemoryRouter>
      </QueryClientProvider>
    );
  }
  return render(
    <Routes>
      <Route path="/" element={<div>Inicio</div>} />
      <Route
        path="/cheques/:identificador"
        element={<PaginaDetalle tipo="ChequeFisico" puerto={puerto} />}
      />
    </Routes>,
    { wrapper: Envoltorio },
  );
}

describe('PaginaDetalle (RF-F02)', () => {
  it('muestra el detalle con el desglose del CMC7 y solo acciones válidas', async () => {
    const puerto = crearPuertoFalso();
    puerto.obtenerCheque.mockResolvedValue(chequeEmitido);
    renderDetalle(chequeEmitido.identificador, puerto);

    expect(await screen.findByText('060')).toBeInTheDocument();
    expect(screen.getByText('0001')).toBeInTheDocument();
    // Emitido → solo Depositado y Anulado (nunca Compensado ni Rechazar directo).
    expect(screen.getByRole('button', { name: 'Depositado' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Anulado' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Compensado' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Rechazar' })).not.toBeInTheDocument();
  });

  it('con identificador inexistente muestra el mensaje 404', async () => {
    const puerto = crearPuertoFalso();
    puerto.obtenerCheque.mockRejectedValue(
      new ErrorCoelsa(404, 'Recurso no encontrado', 'No existe.'),
    );
    renderDetalle('999', puerto);

    expect(await screen.findByText(/instrumento inexistente/i)).toBeInTheDocument();
    // El volver aparece arriba y dentro de la tarjeta 404.
    expect(screen.getAllByRole('link', { name: /volver a la consulta/i })).toHaveLength(2);
  });
});

describe('AccionesEstado (RF-F05)', () => {
  it('cambia el estado y refresca el detalle', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    puerto.obtenerCheque
      .mockResolvedValueOnce(chequeEmitido)
      .mockResolvedValue({ ...chequeEmitido, estado: 'Depositado' });
    puerto.cambiarEstadoCheque.mockResolvedValue({ ...chequeEmitido, estado: 'Depositado' });
    renderDetalle(chequeEmitido.identificador, puerto);

    await usuario.click(await screen.findByRole('button', { name: 'Depositado' }));

    expect(puerto.cambiarEstadoCheque).toHaveBeenCalledWith(chequeEmitido.identificador, {
      estado: 'Depositado',
    });
    expect(await screen.findByText('Compensado')).toBeInTheDocument();
  });

  it('el rechazo exige motivo y lo envía', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    puerto.obtenerCheque.mockResolvedValue(chequeDepositado);
    puerto.cambiarEstadoCheque.mockResolvedValue({
      ...chequeDepositado,
      estado: 'Rechazado',
      motivoRechazo: 11,
    });
    renderDetalle(chequeDepositado.identificador, puerto);

    await usuario.click(await screen.findByRole('button', { name: 'Rechazar' }));
    await usuario.click(screen.getByRole('combobox', { name: /motivo de rechazo/i }));
    await usuario.click(screen.getByRole('option', { name: /falta de fondos/i }));
    await usuario.click(screen.getByRole('button', { name: /confirmar rechazo/i }));

    expect(puerto.cambiarEstadoCheque).toHaveBeenCalledWith(chequeDepositado.identificador, {
      estado: 'Rechazado',
      motivoRechazo: 11,
    });
  });
});

describe('BotonBaja (RF-F06)', () => {
  it('confirma la baja, llama al DELETE y vuelve al inicio', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    puerto.obtenerCheque.mockResolvedValue(chequeEmitido);
    renderDetalle(chequeEmitido.identificador, puerto);

    await usuario.click(await screen.findByRole('button', { name: /dar de baja/i }));
    expect(await screen.findByText(/dejará de listarse/)).toBeInTheDocument();

    await usuario.click(screen.getByRole('button', { name: /confirmar baja/i }));

    expect(puerto.eliminarCheque).toHaveBeenCalledWith(chequeEmitido.identificador);
    expect(await screen.findByText('Inicio')).toBeInTheDocument();
  });
});
