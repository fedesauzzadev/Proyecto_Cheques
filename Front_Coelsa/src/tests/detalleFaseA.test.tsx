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
  IPuertoCertificado,
  IPuertoDevoluciones,
  IPuertoEndosos,
  IPuertoInstrumentos,
} from '../application/puertos';

const echeqPendiente = {
  identificador: 'ABCDEFGHIJK',
  tipo: 'Echeq',
  cbuEmisor: '0110001300000000000017',
  numeroChequera: 1,
  numeroCheque: 1,
  caracter: 'AlaOrden',
  modo: 'Cruzado',
  tipoDocBeneficiario: 'CUIT',
  nombreLibrador: 'Alfa S.R.L.',
  nombreBeneficiario: 'Beta S.A.',
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
  motivoRepudio: null,
  cantidadEndosos: 0,
  fechaCreacion: '2026-09-05T10:00:00Z',
};

const echeqEmitido = { ...echeqPendiente, estado: 'Emitido' };
const echeqRechazado = { ...echeqPendiente, estado: 'Rechazado', motivoRechazo: 11 };

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
  certificado: IPuertoCertificado;
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
    certificado: { obtener: vi.fn() },
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
            puertoCertificado={puertos.certificado}
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

    expect(puertos.aceptacion.aceptar).toHaveBeenCalledWith('ABCDEFGHIJK', true, undefined);
  });

  it('repudiar exige motivo y lo envía (D2)', async () => {
    const usuario = userEvent.setup();
    const puertos = crearPuertos();
    puertos.instrumentos.obtenerEcheq.mockResolvedValue(echeqPendiente);
    puertos.aceptacion.aceptar.mockResolvedValue({
      ...echeqPendiente,
      estado: 'Repudiado',
      motivoRepudio: 'No reconozco la operación',
    });
    renderDetalleEcheq('ABCDEFGHIJK', puertos);

    await usuario.click(await screen.findByRole('button', { name: /repudiar/i }));

    // Sin motivo no llama a la API.
    await usuario.click(screen.getByRole('button', { name: /confirmar repudio/i }));
    expect(await screen.findByText(/hasta 280 caracteres/)).toBeInTheDocument();
    expect(puertos.aceptacion.aceptar).not.toHaveBeenCalled();

    await usuario.type(screen.getByLabelText(/motivo del repudio/i), 'No reconozco la operación');
    await usuario.click(screen.getByRole('button', { name: /confirmar repudio/i }));

    expect(puertos.aceptacion.aceptar).toHaveBeenCalledWith(
      'ABCDEFGHIJK',
      false,
      'No reconozco la operación',
    );
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

  it("un 'No a la orden' emitido informa cesión y no ofrece proponer", async () => {
    const puertos = crearPuertos();
    puertos.instrumentos.obtenerEcheq.mockResolvedValue({
      ...echeqEmitido,
      caracter: 'NoAlaOrden',
    });
    renderDetalleEcheq('ABCDEFGHIJK', puertos);

    expect(await screen.findByText(/por cesión/)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /proponer endoso/i })).not.toBeInTheDocument();
  });

  it("el comprobante del echeq se imprime con el botón 'Imprimir comprobante'", async () => {
    const usuario = userEvent.setup();
    const puertos = crearPuertos();
    puertos.instrumentos.obtenerEcheq.mockResolvedValue(echeqEmitido);
    const imprimir = vi.fn();
    window.print = imprimir;
    renderDetalleEcheq('ABCDEFGHIJK', puertos);

    await usuario.click(
      await screen.findByRole('button', { name: /imprimir comprobante/i }),
    );

    expect(imprimir).toHaveBeenCalledOnce();
  });
});

describe('TarjetaCertificado (Fase D3)', () => {
  it('un rechazado muestra el certificado con CUD y datos del rechazo', async () => {
    const puertos = crearPuertos();
    puertos.instrumentos.obtenerEcheq.mockResolvedValue(echeqRechazado);
    puertos.certificado.obtener.mockResolvedValue({
      cud: 'A1B2C3D4E5F6A7B8C9D0E1F2A3B4C5D6E7F8A9B0C1D2E3F4A5B6C7D8E9F0A1B2',
      codigoVisualizacion: 'A1B2C3D4E5F6',
      idEcheq: 'ABCDEFGHIJK',
      cmc7: '011000114250000123400001234567',
      estado: 'Rechazado',
      motivoRechazo: 11,
      cuitLibrador: '20123456786',
      nombreLibrador: 'Alfa S.R.L.',
      cuitBeneficiario: '27876543219',
      nombreBeneficiario: 'Beta S.A.',
      monto: 250000,
      moneda: 'D',
      fechaEmision: '2026-09-05',
      fechaVencimiento: '2026-10-05',
      fechaRechazo: '2026-09-06T10:00:00Z',
    });
    renderDetalleEcheq('ABCDEFGHIJK', puertos);

    expect(
      await screen.findByText('Certificado para acciones civiles'),
    ).toBeInTheDocument();
    expect(puertos.certificado.obtener).toHaveBeenCalledWith('ABCDEFGHIJK', expect.anything());
expect(
      await screen.findByText(/A1B2C3D4E5F6A7B8C9D0E1F2A3B4C5D6E7F8A9B0C1D2E3F4A5B6C7D8E9F0A1B2/),
    ).toBeInTheDocument();
    // Verificar que el estado se muestra como Rechazado en algún lugar
    expect(screen.getByText(/Rechazado/)).toBeInTheDocument();
  });

  it('un echeq no rechazado no consulta el certificado', async () => {
    const puertos = crearPuertos();
    puertos.instrumentos.obtenerEcheq.mockResolvedValue(echeqEmitido);
    renderDetalleEcheq('ABCDEFGHIJK', puertos);

    await screen.findByText('Cadena de endosos');
    expect(screen.queryByText('Certificado para acciones civiles')).not.toBeInTheDocument();
    expect(puertos.certificado.obtener).not.toHaveBeenCalled();
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
