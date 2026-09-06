// Tests de las secciones Fase A del detalle de echeqs: aceptación, cadena de
// endosos y devoluciones (RF-F10/F11/F12).
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import PaginaDetalle from '../presentation/features/detalle/PaginaDetalle';
import type {
  IPuertoAceptacion,
  IPuertoDevoluciones,
  IPuertoEndosos,
  IPuertoInstrumentos,
} from '../application/puertos';

const echeqPendiente = {
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
  cuitBeneficiario: '27876543219',
  monto: 250000,
  moneda: 'D',
  fechaEmision: '2026-09-05',
  fechaDiferimiento: null,
  fechaVencimiento: '2026-10-05',
  estado: 'Pendiente',
  motivoRechazo: null,
  cantidadEndosos: 0,
  fechaCreacion: '2026-09-05T10:00:00Z',
};

const echeqEmitido = { ...echeqPendiente, estado: 'Emitido' };

const endosoPropuesto = {
  orden: 1,
  cuitEndosante: '27876543219',
  cuitEndosatario: '30511222334',
  estado: 'Propuesto',
  fechaCreacion: '2026-09-06T10:00:00Z',
};

const devolucionSolicitada = {
  numero: 1,
  cuitSolicitante: '20123456786',
  motivo: 'La necesito de vuelta',
  estado: 'Solicitada',
  fechaCreacion: '2026-09-06T10:00:00Z',
};

interface Puertos {
  instrumentos: IPuertoInstrumentos;
  aceptacion: IPuertoAceptacion;
  endosos: IPuertoEndosos;
  devoluciones: IPuertoDevoluciones;
}

function crearPuertos() {
  const base = {
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
  return {
    instrumentos: base,
    aceptacion: { aceptar: vi.fn() },
    endosos: {
      listar: vi.fn().mockResolvedValue([]),
      proponer: vi.fn(),
      resolver: vi.fn(),
      anular: vi.fn(),
    },
    devoluciones: {
      listar: vi.fn().mockResolvedValue([]),
      solicitar: vi.fn(),
      resolver: vi.fn(),
      anular: vi.fn(),
    },
  };
}

function renderDetalleEcheq(identificador: string, puertos: Puertos) {
  const cliente = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  function Envoltorio({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={cliente}>
        <MemoryRouter initialEntries={[`/echeqs/${identificador}`]}>{children}</MemoryRouter>
      </QueryClientProvider>
    );
  }
  return render(
    <Routes>
      <Route
        path="/echeqs/:identificador"
        element={
          <PaginaDetalle
            tipo="Echeq"
            puerto={puertos.instrumentos}
            puertoAceptacion={puertos.aceptacion}
            puertoEndosos={puertos.endosos}
            puertoDevoluciones={puertos.devoluciones}
          />
        }
      />
    </Routes>,
    { wrapper: Envoltorio },
  );
}

describe('SeccionAceptacion (RF-F10)', () => {
  it('un pendiente muestra Aceptar/Repudiar en vez de acciones de estado', async () => {
    const usuario = userEvent.setup();
    const puertos = crearPuertos();
    puertos.instrumentos.obtenerEcheq.mockResolvedValue(echeqPendiente);
    puertos.aceptacion.aceptar.mockResolvedValue({ ...echeqPendiente, estado: 'Emitido' });
    renderDetalleEcheq('ABCDEFGHIJK', puertos);

    expect(await screen.findByRole('button', { name: /^aceptar$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /repudiar/i })).toBeInTheDocument();
    expect(screen.queryByText('Acciones')).not.toBeInTheDocument();

    await usuario.click(screen.getByRole('button', { name: /^aceptar$/i }));

    expect(puertos.aceptacion.aceptar).toHaveBeenCalledWith('ABCDEFGHIJK', true);
  });
});

describe('CadenaEndosos (RF-F11)', () => {
  it('informa CUIT inválido sin proponer', async () => {
    const usuario = userEvent.setup();
    const puertos = crearPuertos();
    puertos.instrumentos.obtenerEcheq.mockResolvedValue(echeqEmitido);
    renderDetalleEcheq('ABCDEFGHIJK', puertos);

    await screen.findByText('Cadena de endosos');
    await usuario.type(screen.getByLabelText(/endosar a/i), '123');
    await usuario.click(screen.getByRole('button', { name: /proponer endoso/i }));

    expect(await screen.findByText(/11 dígitos/)).toBeInTheDocument();
    expect(puertos.endosos.proponer).not.toHaveBeenCalled();
  });

  it('admite un propuesto con la identidad del endosatario', async () => {
    const usuario = userEvent.setup();
    const puertos = crearPuertos();
    puertos.instrumentos.obtenerEcheq.mockResolvedValue(echeqEmitido);
    puertos.endosos.listar.mockResolvedValue([endosoPropuesto]);
    puertos.endosos.resolver.mockResolvedValue({ ...endosoPropuesto, estado: 'Vigente' });
    renderDetalleEcheq('ABCDEFGHIJK', puertos);

    await screen.findByText(/30511222334/);
    await usuario.type(screen.getByLabelText(/actuar como/i), '30511222334');
    await usuario.click(screen.getByRole('button', { name: /^admitir$/i }));

    expect(puertos.endosos.resolver).toHaveBeenCalledWith('ABCDEFGHIJK', 1, true, '30511222334');
  });
});

describe('SeccionDevoluciones (RF-F12)', () => {
  it('solicita y resuelve con las identidades correctas', async () => {
    const usuario = userEvent.setup();
    const puertos = crearPuertos();
    puertos.instrumentos.obtenerEcheq.mockResolvedValue(echeqEmitido);
    puertos.devoluciones.listar.mockResolvedValue([]);
    puertos.devoluciones.solicitar.mockResolvedValue(devolucionSolicitada);
    renderDetalleEcheq('ABCDEFGHIJK', puertos);

    await screen.findByText('Pedidos de devolución');
    await usuario.type(screen.getByLabelText(/pedir devolución como/i), '20123456786');
    await usuario.type(screen.getByLabelText(/motivo \(opcional\)/i), 'La necesito');
    await usuario.click(screen.getByRole('button', { name: /solicitar devolución/i }));

    expect(puertos.devoluciones.solicitar).toHaveBeenCalledWith(
      'ABCDEFGHIJK',
      '20123456786',
      'La necesito',
    );
  });

  it('acepta un pedido con el CUIT del tenedor', async () => {
    const usuario = userEvent.setup();
    const puertos = crearPuertos();
    puertos.instrumentos.obtenerEcheq.mockResolvedValue(echeqEmitido);
    puertos.devoluciones.listar.mockResolvedValue([devolucionSolicitada]);
    puertos.devoluciones.resolver.mockResolvedValue({
      ...devolucionSolicitada,
      estado: 'Aceptada',
    });
    renderDetalleEcheq('ABCDEFGHIJK', puertos);

    await screen.findByText(/necesito de vuelta/i);
    await usuario.click(screen.getByRole('button', { name: /^aceptar$/i }));

    expect(puertos.devoluciones.resolver).toHaveBeenCalledWith(
      'ABCDEFGHIJK',
      1,
      true,
      '27876543219',
    );
  });
});
