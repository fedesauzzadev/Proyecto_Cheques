// Pedidos de devolución del echeq (RF-F12): solicitud por cualquier integrante de
// la cadena, resolución por el tenedor actual y anulación por el solicitante.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { toast } from 'sonner';
import { Button } from '@/presentation/ui/button';
import { Card, CardContent } from '@/presentation/ui/card';
import { Input } from '@/presentation/ui/input';
import { Label } from '@/presentation/ui/label';
import { Badge } from '@/presentation/ui/badge';
import { Skeleton } from '@/presentation/ui/skeleton';
import { esCuitValido } from '@/domain/validadorCuit';
import type { EstadoDevolucion } from '@/domain/tipos';
import type { IPuertoDevoluciones } from '@/application/puertos';
import {
  useAnularDevolucion,
  useDevoluciones,
  useResolverDevolucion,
  useSolicitarDevolucion,
} from '@/application/hooks/useDevoluciones';

const APARIENCIA_DEVOLUCION: Record<EstadoDevolucion, string> = {
  Solicitada: 'bg-secondary text-secondary-foreground',
  Aceptada: 'bg-green-700 text-white',
  Rechazada: 'bg-destructive/10 text-destructive',
  Anulada: 'text-muted-foreground',
};

interface Props {
  idecheq: string;
  /** CUIT del tenedor actual (quien resuelve los pedidos). */
  tenedorActual: string;
  /** Solo en Emitido se pueden solicitar devoluciones. */
  puedeSolicitar: boolean;
  puerto?: IPuertoDevoluciones;
}

export default function SeccionDevoluciones({
  idecheq,
  tenedorActual,
  puedeSolicitar,
  puerto,
}: Props) {
  const pedidos = useDevoluciones(idecheq, puerto);
  const solicitar = useSolicitarDevolucion(puerto);
  const resolver = useResolverDevolucion(puerto);
  const anular = useAnularDevolucion(puerto);

  const [cuitSolicitante, setCuitSolicitante] = useState('');
  const [motivo, setMotivo] = useState('');
  const [errorSolicitante, setErrorSolicitante] = useState<string | null>(null);
  const [cuitResolutor, setCuitResolutor] = useState(tenedorActual);

  function alSolicitar(evento: FormEvent) {
    evento.preventDefault();
    const cuit = cuitSolicitante.trim();
    if (!esCuitValido(cuit)) {
      setErrorSolicitante('Ingresá un CUIT/CUIL válido de 11 dígitos.');
      return;
    }
    setErrorSolicitante(null);
    solicitar.mutate(
      { idecheq, cuitSolicitante: cuit, motivo: motivo.trim() ? motivo : null },
      {
        onSuccess: (pedido) => {
          toast.success(`Devolución ${pedido.numero} solicitada.`);
          setCuitSolicitante('');
          setMotivo('');
        },
        onError: (error) => {
          toast.error('No se pudo solicitar la devolución.', { description: error.message });
        },
      },
    );
  }

  function alResolver(numero: number, aceptada: boolean) {
    const cuit = cuitResolutor.trim();
    if (!esCuitValido(cuit)) {
      toast.error('El CUIT de quien resuelve debe ser válido (es el tenedor actual).');
      return;
    }
    resolver.mutate(
      { idecheq, numero, aceptada, cuitResolutor: cuit },
      {
        onSuccess: () => {
          toast.success(
            aceptada ? `Devolución ${numero} aceptada.` : `Devolución ${numero} rechazada.`,
          );
        },
        onError: (error) => {
          toast.error('No se pudo resolver el pedido.', { description: error.message });
        },
      },
    );
  }

  return (
    <div className="flex flex-col gap-4">
      {puedeSolicitar && (
        <form onSubmit={alSolicitar} className="flex flex-col gap-2" noValidate>
          <Label htmlFor="cuit-solicitante">Pedir devolución como (CUIT/CUIL de la cadena)</Label>
          <div className="flex flex-wrap gap-2">
            <Input
              id="cuit-solicitante"
              inputMode="numeric"
              placeholder="CUIT integrante distinto del tenedor"
              value={cuitSolicitante}
              onChange={(evento) => setCuitSolicitante(evento.target.value)}
              aria-invalid={errorSolicitante !== null}
              className="max-w-xs font-mono"
            />
            <Input
              id="motivo-devolucion"
              placeholder="Motivo (opcional)"
              value={motivo}
              onChange={(evento) => setMotivo(evento.target.value)}
              aria-label="Motivo (opcional)"
              className="max-w-xs"
            />
            <Button type="submit" disabled={solicitar.isPending}>
              {solicitar.isPending ? 'Solicitando…' : 'Solicitar devolución'}
            </Button>
          </div>
          {errorSolicitante && (
            <p role="alert" className="text-sm text-destructive">
              {errorSolicitante}
            </p>
          )}
        </form>
      )}

      {pedidos.isPending && (
        <div
          role="status"
          aria-label="Cargando pedidos de devolución"
          className="flex flex-col gap-2"
        >
          <Skeleton className="h-10 w-full" />
        </div>
      )}

      {pedidos.isError && (
        <Card role="alert" className="border-destructive">
          <CardContent className="flex items-center gap-3 pt-4 text-sm">
            <span className="text-destructive">{pedidos.error.message}</span>
            <Button variant="outline" size="sm" onClick={() => pedidos.refetch()}>
              Reintentar
            </Button>
          </CardContent>
        </Card>
      )}

      {pedidos.isSuccess && pedidos.data.length === 0 && (
        <p className="text-sm text-muted-foreground">No hay pedidos de devolución en este echeq.</p>
      )}

      {pedidos.isSuccess && pedidos.data.length > 0 && (
        <ol className="flex flex-col gap-2">
          {pedidos.data.map((pedido) => (
            <li
              key={pedido.numero}
              className="flex flex-wrap items-center gap-2 rounded-md border p-3 text-sm"
            >
              <span className="font-mono font-medium">#{pedido.numero}</span>
              <span className="font-mono">{pedido.cuitSolicitante}</span>
              {pedido.motivo && <span className="text-muted-foreground">“{pedido.motivo}”</span>}
              <Badge className={APARIENCIA_DEVOLUCION[pedido.estado]}>{pedido.estado}</Badge>
              {pedido.estado === 'Solicitada' && (
                <span className="ml-auto flex flex-wrap items-center gap-2">
                  <Input
                    aria-label={`CUIT de quien resuelve el pedido ${pedido.numero}`}
                    inputMode="numeric"
                    value={cuitResolutor}
                    onChange={(evento) => setCuitResolutor(evento.target.value)}
                    className="w-36 font-mono"
                  />
                  <Button
                    size="sm"
                    disabled={resolver.isPending}
                    onClick={() => alResolver(pedido.numero, true)}
                  >
                    Aceptar
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    disabled={resolver.isPending}
                    onClick={() => alResolver(pedido.numero, false)}
                  >
                    Rechazar
                  </Button>
                  <Button
                    size="sm"
                    variant="ghost"
                    disabled={anular.isPending}
                    onClick={() =>
                      anular.mutate(
                        { idecheq, numero: pedido.numero },
                        {
                          onSuccess: () => toast.success(`Devolución ${pedido.numero} anulada.`),
                          onError: (error) =>
                            toast.error('No se pudo anular.', { description: error.message }),
                        },
                      )
                    }
                  >
                    Anular pedido
                  </Button>
                </span>
              )}
            </li>
          ))}
        </ol>
      )}
    </div>
  );
}
