// Vista de detalle individual por identificador de negocio (RF-F02, espejo de RF-04).
// 404 → "instrumento inexistente" con vuelta al listado.
// En echeqs suma las secciones Fase A: aceptación (Pendiente), cadena de endosos
// y devoluciones. La custodia se opera desde Acciones (gobernada por la máquina).
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { Button } from '@/presentation/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/presentation/ui/card';
import { Skeleton } from '@/presentation/ui/skeleton';
import type { TipoInstrumentoForm } from '@/domain/estrategias/estrategiaCreacion';
import type {
  IPuertoAceptacion,
  IPuertoDevoluciones,
  IPuertoEndosos,
  IPuertoInstrumentos,
} from '@/application/puertos';
import { useObtenerInstrumento } from '@/application/hooks/useObtenerInstrumento';
import TarjetaDetalle from './TarjetaDetalle';
import AccionesEstado from './AccionesEstado';
import BotonBaja from './BotonBaja';
import SeccionAceptacion from './SeccionAceptacion';
import CadenaEndosos from './CadenaEndosos';
import SeccionDevoluciones from './SeccionDevoluciones';

interface Props {
  tipo: TipoInstrumentoForm;
  puerto?: IPuertoInstrumentos;
  puertoAceptacion?: IPuertoAceptacion;
  puertoEndosos?: IPuertoEndosos;
  puertoDevoluciones?: IPuertoDevoluciones;
}

export default function PaginaDetalle({
  tipo,
  puerto,
  puertoAceptacion,
  puertoEndosos,
  puertoDevoluciones,
}: Props) {
  const { identificador = '' } = useParams();
  const detalle = useObtenerInstrumento(tipo, identificador, puerto);
  const esEcheq = tipo === 'Echeq';
  const estaPendiente = detalle.isSuccess && detalle.data.estado === 'Pendiente';
  const estaEmitido = detalle.isSuccess && detalle.data.estado === 'Emitido';

  return (
    <div className="flex flex-col gap-4">
      <Button asChild variant="ghost" className="w-fit">
        <Link to="/">
          <ArrowLeft className="size-4" aria-hidden />
          Volver a la consulta
        </Link>
      </Button>

      {detalle.isPending && (
        <Card>
          <CardContent
            className="flex flex-col gap-2 pt-6"
            role="status"
            aria-label="Cargando detalle"
          >
            <Skeleton className="h-8 w-1/3" />
            <Skeleton className="h-5 w-full" />
            <Skeleton className="h-5 w-full" />
            <Skeleton className="h-5 w-2/3" />
          </CardContent>
        </Card>
      )}

      {detalle.isError && detalle.error.estadoHttp === 404 && (
        <Card>
          <CardHeader>
            <CardTitle>Instrumento inexistente</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col items-start gap-4">
            <p className="text-sm text-muted-foreground">
              No existe un {tipo === 'ChequeFisico' ? 'cheque físico' : 'echeq'} con identificador{' '}
              <span className="font-mono">{identificador}</span>.
            </p>
            <Button asChild>
              <Link to="/">Volver a la consulta</Link>
            </Button>
          </CardContent>
        </Card>
      )}

      {detalle.isError && detalle.error.estadoHttp !== 404 && (
        <Card role="alert" className="border-destructive">
          <CardHeader>
            <CardTitle className="text-destructive">No se pudo cargar el detalle</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col items-start gap-4">
            <p className="text-sm text-muted-foreground">{detalle.error.message}</p>
            <Button variant="outline" onClick={() => detalle.refetch()}>
              Reintentar
            </Button>
          </CardContent>
        </Card>
      )}

      {detalle.isSuccess && (
        <>
          <TarjetaDetalle item={detalle.data} />
          {esEcheq && estaPendiente ? (
            <Card>
              <CardHeader>
                <CardTitle>Aceptación del beneficiario</CardTitle>
              </CardHeader>
              <CardContent>
                <SeccionAceptacion idecheq={detalle.data.identificador} puerto={puertoAceptacion} />
              </CardContent>
            </Card>
          ) : (
            <Card>
              <CardHeader>
                <CardTitle>Acciones</CardTitle>
              </CardHeader>
              <CardContent className="flex flex-col gap-4">
                {esEcheq && detalle.data.estado === 'EnCustodia' && (
                  <p className="text-sm text-muted-foreground">
                    En custodia: el banco lo deposita automáticamente al vencer. Podés rescatarlo
                    para volver a operarlo.
                  </p>
                )}
                <AccionesEstado tipo={tipo} instrumento={detalle.data} puerto={puerto} />
                <div className="border-t pt-4">
                  <BotonBaja
                    tipo={tipo}
                    identificador={detalle.data.identificador}
                    monto={detalle.data.monto}
                    moneda={detalle.data.moneda}
                    puerto={puerto}
                  />
                </div>
              </CardContent>
            </Card>
          )}
          {esEcheq && (
            <Card>
              <CardHeader>
                <CardTitle>Cadena de endosos</CardTitle>
              </CardHeader>
              <CardContent>
                <CadenaEndosos
                  idecheq={detalle.data.identificador}
                  puedeEndosar={estaEmitido}
                  puerto={puertoEndosos}
                />
              </CardContent>
            </Card>
          )}
          {esEcheq && (
            <Card>
              <CardHeader>
                <CardTitle>Pedidos de devolución</CardTitle>
              </CardHeader>
              <CardContent>
                <SeccionDevoluciones
                  idecheq={detalle.data.identificador}
                  tenedorActual={detalle.data.cuitBeneficiario}
                  puedeSolicitar={estaEmitido}
                  puerto={puertoDevoluciones}
                />
              </CardContent>
            </Card>
          )}
        </>
      )}
    </div>
  );
}
