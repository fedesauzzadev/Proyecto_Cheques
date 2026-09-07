// Tests de la sección de cesiones (Fase D1, espejo de RF-13): solo visible en
// echeqs "no a la orden"; solicitud con domicilio, resolución por el cesionario.
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import PaginaDetalle from '../presentation/features/detalle/PaginaDetalle';
import type {
  IPuertoAceptacion,
  IPuertoCesiones,
  IPuertoDevoluciones,
  IPuertoEndosos,
  IPuertoInstrumentos,
} from '../application/puertos';

const echeqNoAlaOrden = {
  identificador: 'BBBBBBBBBBB',
  tipo: 'Echeq',
  cbuEmisor: '0110001300000000000017',
  numeroChequera: 1,
  numeroCheque: 5,
  caracter: 'NoAlaOrden',
  modo: 'Cruzado',
  tipoDocBeneficiario: 'CUIT',
  nombreLibrador: 'Alfa S.R.L.',
  nombreBeneficiario: 'Beta S.A.',
  concepto: null,
  motivo: null,
  referencia: null,
  emailNotificacion: null,
  motivoRepudio: null,
  cmc7: '011000120770000000500000000001',
  desgloseCmc7: {
    banco: '011',
    sucursal: '0001',
    codigoPostal: '2077',
    numeroCheque: '00000005',
    numeroCuenta: '00000000001',
  },
  cuitLibrador: '20123456786',
  cuitBeneficiario: '27876543219',
  monto: 150000,
  moneda: 'P',
  fechaEmision: '2026-09-05',
  fechaDiferimiento: null,
  fechaVencimiento: '2026-10-05',
  estado: 'Emitido',
  motivoRechazo: null,
  cantidadEndosos: 0,
  fechaCreacion: '2026-09-05T10:00:00Z',
};

const echeqAlaOrden = { ...echeqNoAlaOrden, identificador: 'AAAAAAAAAAA', caracter: 'AlaOrden' };

const cesionSolicitada = {
  numero: 1,
  cuitCedente: '27876543219',
  cuitCesionario: '30511222334',
  domicilioCesionario: 'Calle 123',
  estado: 'Solicitada',
  fechaCreacion: '2026-09-06T10:00:00Z',
};

interface Puertos {
  instrumentos: IPuertoInstrumentos;
  aceptacion: IPuertoAceptacion;
  endosos: IPuertoEndosos;
  devoluciones: IPuertoDevoluciones;
  cesiones: IPuertoCesiones;
}

function crearPuertos() {
  const instrumentos = {
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
  };
  return {
    instrumentos,
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
    cesiones: {
      listar: vi.fn().mockResolvedValue([]),
      solicitar: vi.fn(),
      resolver: vi.fn(),
      anular: vi.fn(),
    },
  };
}

function renderDetalle(identificador: string, puertos: Puertos) {
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
            puertoCesiones={puertos.cesiones}
          />
        }
      />
    </Routes>,
    { wrapper: Envoltorio },
  );
}

describe('SeccionCesiones (Fase D1)', () => {
  it("solo aparece en echeqs 'No a la orden'", async () => {
    const puertos = crearPuertos();
    puertos.instrumentos.obtenerEcheq.mockResolvedValue(echeqAlaOrden);
    renderDetalle('AAAAAAAAAAA', puertos);

    await screen.findByText('Cadena de endosos');
    expect(screen.queryByText('Cesiones')).not.toBeInTheDocument();
  });

  it('solicita con CUIT y domicilio válidos', async () => {
    const usuario = userEvent.setup();
    const puertos = crearPuertos();
    puertos.instrumentos.obtenerEcheq.mockResolvedValue(echeqNoAlaOrden);
    puertos.cesiones.solicitar.mockResolvedValue(cesionSolicitada);
    renderDetalle('BBBBBBBBBBB', puertos);

    await screen.findByText('Cesiones');
    await usuario.type(screen.getByLabelText(/ceder a/i), '30511222334');
    await usuario.type(screen.getByLabelText(/domicilio del cesionario/i), 'Calle 123');
    await usuario.click(screen.getByRole('button', { name: /solicitar cesión/i }));

    expect(puertos.cesiones.solicitar).toHaveBeenCalledWith(
      'BBBBBBBBBBB',
      '30511222334',
      'Calle 123',
    );
  });

  it('informa CUIT inválido sin llamar a la API', async () => {
    const usuario = userEvent.setup();
    const puertos = crearPuertos();
    puertos.instrumentos.obtenerEcheq.mockResolvedValue(echeqNoAlaOrden);
    renderDetalle('BBBBBBBBBBB', puertos);

    await screen.findByText('Cesiones');
    await usuario.type(screen.getByLabelText(/ceder a/i), '123');
    await usuario.click(screen.getByRole('button', { name: /solicitar cesión/i }));

    expect(await screen.findByText(/11 dígitos/)).toBeInTheDocument();
    expect(puertos.cesiones.solicitar).not.toHaveBeenCalled();
  });

  it('acepta una solicitada con la identidad del cesionario', async () => {
    const usuario = userEvent.setup();
    const puertos = crearPuertos();
    puertos.instrumentos.obtenerEcheq.mockResolvedValue(echeqNoAlaOrden);
    puertos.cesiones.listar.mockResolvedValue([cesionSolicitada]);
    puertos.cesiones.resolver.mockResolvedValue({ ...cesionSolicitada, estado: 'Aceptada' });
    renderDetalle('BBBBBBBBBBB', puertos);

    await screen.findByText(/30511222334/);
    await usuario.type(screen.getByLabelText(/actuar como cesionario/i), '30511222334');
    await usuario.click(screen.getByRole('button', { name: /^aceptar$/i }));

    expect(puertos.cesiones.resolver).toHaveBeenCalledWith('BBBBBBBBBBB', 1, true, '30511222334');
  });
});
