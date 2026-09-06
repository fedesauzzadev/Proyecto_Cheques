// Tests de la página de creación (RF-F03/F04): validación por estrategia,
// envío con Idempotency-Key, replay explícito y 409 con nueva clave.
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import PaginaCreacion from '../presentation/features/creacion/PaginaCreacion';
import { ErrorCoelsa } from '../infrastructure/clienteHttp';
import type { IPuertoInstrumentos } from '../application/puertos';

const chequeCreado = {
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

const VALORES_VALIDOS = {
  cmc7: '060000114250000123400001234567',
  cuitLibrador: '20123456786',
  cuitBeneficiario: '27876543219',
  monto: '150000.50',
  fechaEmision: '2026-09-05',
  fechaVencimiento: '2026-10-05',
};

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

function renderCreacion(tipo: 'ChequeFisico' | 'Echeq', puerto: IPuertoInstrumentos) {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Envoltorio({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={cliente}>
        <MemoryRouter>{children}</MemoryRouter>
      </QueryClientProvider>
    );
  }
  return render(<PaginaCreacion tipo={tipo} puerto={puerto} />, { wrapper: Envoltorio });
}

async function completarChequeValido(usuario: ReturnType<typeof userEvent.setup>) {
  await usuario.type(screen.getByLabelText(/cmc7/i), VALORES_VALIDOS.cmc7);
  await usuario.type(screen.getByLabelText(/cuit\/cuil librador/i), VALORES_VALIDOS.cuitLibrador);
  await usuario.type(
    screen.getByLabelText(/cuit\/cuil beneficiario/i),
    VALORES_VALIDOS.cuitBeneficiario,
  );
  await usuario.type(screen.getByLabelText(/^monto/i), VALORES_VALIDOS.monto);
  fireEvent.change(screen.getByLabelText(/fecha de emisión/i), {
    target: { value: VALORES_VALIDOS.fechaEmision },
  });
  fireEvent.change(screen.getByLabelText(/fecha de vencimiento/i), {
    target: { value: VALORES_VALIDOS.fechaVencimiento },
  });
}

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

describe('PaginaCreacion — cheque físico (RF-F03)', () => {
  it('con el formulario vacío muestra errores por campo y no llama a la API', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    renderCreacion('ChequeFisico', puerto);

    await usuario.click(screen.getByRole('button', { name: /^crear$/i }));

    // String exacto: la descripción de la tarjeta también menciona "30 dígitos".
    expect(
      await screen.findByText(
        'El CMC7 debe ser un código magnetizable de 30 dígitos (banco + sucursal + código postal + número de cheque + cuenta).',
      ),
    ).toBeInTheDocument();
    expect(puerto.crearCheque).not.toHaveBeenCalled();
  });

  it('envía el request con Idempotency-Key con formato GUID', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    puerto.crearCheque.mockResolvedValue({ respuesta: chequeCreado, esReplay: false });
    renderCreacion('ChequeFisico', puerto);

    await completarChequeValido(usuario);
    await usuario.click(screen.getByRole('button', { name: /^crear$/i }));

    expect(puerto.crearCheque).toHaveBeenCalledWith(
      {
        cmc7: VALORES_VALIDOS.cmc7,
        cuitLibrador: VALORES_VALIDOS.cuitLibrador,
        cuitBeneficiario: VALORES_VALIDOS.cuitBeneficiario,
        monto: 150000.5,
        moneda: 'P',
        fechaEmision: VALORES_VALIDOS.fechaEmision,
        fechaDiferimiento: null,
        fechaVencimiento: VALORES_VALIDOS.fechaVencimiento,
      },
      expect.stringMatching(UUID),
    );
  });

  it('informa el replay explícitamente sin navegar', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    puerto.crearCheque.mockResolvedValue({ respuesta: chequeCreado, esReplay: true });
    renderCreacion('ChequeFisico', puerto);

    await completarChequeValido(usuario);
    await usuario.click(screen.getByRole('button', { name: /^crear$/i }));

    expect(await screen.findByText(/ya existía para este intento/)).toBeInTheDocument();
  });

  it('ante 409 ofrece reintentar con nueva clave', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    puerto.crearCheque.mockRejectedValue(new ErrorCoelsa(409, 'Conflicto', 'CMC7 duplicado.'));
    renderCreacion('ChequeFisico', puerto);

    await completarChequeValido(usuario);
    await usuario.click(screen.getByRole('button', { name: /^crear$/i }));

    expect(await screen.findByText(/conflicto/i)).toBeInTheDocument();
    await usuario.click(screen.getByRole('button', { name: /nueva clave/i }));
    await usuario.click(screen.getByRole('button', { name: /^crear$/i }));

    expect(puerto.crearCheque).toHaveBeenCalledTimes(2);
    const primera = puerto.crearCheque.mock.calls[0][1] as string;
    const segunda = puerto.crearCheque.mock.calls[1][1] as string;
    expect(primera).toMatch(UUID);
    expect(segunda).toMatch(UUID);
    expect(primera).not.toBe(segunda);
  });
});

describe('PaginaCreacion — echeq', () => {
  it('envía el request del echeq con CMC7 completo', async () => {
    const usuario = userEvent.setup();
    const puerto = crearPuertoFalso();
    puerto.crearEcheq.mockResolvedValue({ respuesta: {}, esReplay: false });
    renderCreacion('Echeq', puerto);

    await usuario.type(screen.getByLabelText(/cmc7/i), '011000114250000123400001234567');
    await usuario.type(screen.getByLabelText(/cuit\/cuil librador/i), '20123456786');
    await usuario.type(screen.getByLabelText(/cuit\/cuil beneficiario/i), '30511222334');
    await usuario.type(screen.getByLabelText(/^monto/i), '250000');
    fireEvent.change(screen.getByLabelText(/fecha de emisión/i), {
      target: { value: '2026-09-05' },
    });
    fireEvent.change(screen.getByLabelText(/fecha de vencimiento/i), {
      target: { value: '2026-10-05' },
    });
    await usuario.click(screen.getByRole('button', { name: /^crear$/i }));

    expect(puerto.crearEcheq).toHaveBeenCalledWith(
      expect.objectContaining({
        cmc7: '011000114250000123400001234567',
        moneda: 'P',
        fechaVencimiento: '2026-10-05',
      }),
      expect.stringMatching(UUID),
    );
    expect(screen.getByText(/generado por la API/i)).toBeInTheDocument();
  });
});
