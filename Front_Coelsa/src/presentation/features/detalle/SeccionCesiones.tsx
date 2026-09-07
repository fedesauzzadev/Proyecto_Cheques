// Cesiones del echeq "no a la orden" (Fase D1, espejo de RF-13): solicitud por
// el tenedor con domicilio del cesionario, resolución por el cesionario y
// anulación por el cedente.
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
import type { EstadoCesion } from '@/domain/tipos';
import type { IPuertoCesiones } from '@/application/puertos';
import {
  useAnularCesion,
  useCesiones,
  useResolverCesion,
  useSolicitarCesion,
} from '@/application/hooks/useCesiones';

const APARIENCIA_CESION: Record<EstadoCesion, string> = {
  Solicitada: 'bg-secondary text-secondary-foreground',
  Aceptada: 'bg-green-700 text-white',
  Rechazada: 'bg-destructive/10 text-destructive',
  Anulada: 'text-muted-foreground',
};

interface Props {
  idecheq: string;
  /** Solo en Emitido se pueden solicitar cesiones. */
  puedeSolicitar: boolean;
  puerto?: IPuertoCesiones;
}

export default function SeccionCesiones({ idecheq, puedeSolicitar, puerto }: Props) {
  const cesiones = useCesiones(idecheq, puerto);
  const solicitar = useSolicitarCesion(puerto);
  const resolver = useResolverCesion(puerto);
  const anular = useAnularCesion(puerto);

  const [cuitCesionario, setCuitCesionario] = useState('');
  const [domicilio, setDomicilio] = useState('');
  const [errorNuevo, setErrorNuevo] = useState<string | null>(null);
  const [cuitResolutor, setCuitResolutor] = useState('');

  function alSolicitar(evento: FormEvent) {
    evento.preventDefault();
    const cuit = cuitCesionario.trim();
    if (!esCuitValido(cuit)) {
      setErrorNuevo('Ingresá un CUIT/CUIL válido de 11 dígitos.');
      return;
    }
    if (domicilio.trim() === '' || domicilio.trim().length > 200) {
      setErrorNuevo('El domicilio del cesionario es obligatorio (hasta 200 caracteres).');
      return;
    }
    setErrorNuevo(null);
    solicitar.mutate(
      { idecheq, cuitCesionario: cuit, domicilioCesionario: domicilio.trim() },
      {
        onSuccess: (cesion) => {
          toast.success(`Cesión ${cesion.numero} solicitada.`);
          setCuitCesionario('');
          setDomicilio('');
        },
        onError: (error) => {
          toast.error('No se pudo solicitar la cesión.', { description: error.message });
        },
      },
    );
  }

  function alResolver(numero: number, aceptada: boolean) {
    const cuit = cuitResolutor.trim();
    if (!esCuitValido(cuit)) {
      toast.error('Indicá con qué CUIT actuás (debe ser el cesionario).');
      return;
    }
    resolver.mutate(
      { idecheq, numero, aceptada, cuitResolutor: cuit },
      {
        onSuccess: () => {
          toast.success(aceptada ? `Cesión ${numero} aceptada.` : `Cesión ${numero} rechazada.`);
        },
        onError: (error) => {
          toast.error('No se pudo resolver la cesión.', { description: error.message });
        },
      },
    );
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="cesion-actuar-como">Actuar como cesionario (CUIT)</Label>
        <Input
          id="cesion-actuar-como"
          inputMode="numeric"
          placeholder="CUIT con el que aceptás o rechazás"
          value={cuitResolutor}
          onChange={(evento) => setCuitResolutor(evento.target.value)}
          className="max-w-xs font-mono"
        />
        <p className="text-xs text-muted-foreground">
          Solo el cesionario puede aceptar o rechazar cada cesión solicitada.
        </p>
      </div>

      {puedeSolicitar && (
        <form onSubmit={alSolicitar} className="flex flex-col gap-2" noValidate>
          <Label htmlFor="cuit-cesionario">Ceder a (CUIT/CUIL bancarizado)</Label>
          <div className="flex flex-wrap gap-2">
            <Input
              id="cuit-cesionario"
              inputMode="numeric"
              placeholder="30511222334"
              value={cuitCesionario}
              onChange={(evento) => setCuitCesionario(evento.target.value)}
              aria-invalid={errorNuevo !== null}
              className="max-w-xs font-mono"
            />
            <Input
              id="domicilio-cesionario"
              placeholder="Domicilio del cesionario"
              value={domicilio}
              onChange={(evento) => setDomicilio(evento.target.value)}
              aria-label="Domicilio del cesionario"
              className="max-w-xs"
            />
            <Button type="submit" disabled={solicitar.isPending}>
              {solicitar.isPending ? 'Solicitando…' : 'Solicitar cesión'}
            </Button>
          </div>
          {errorNuevo && (
            <p role="alert" className="text-sm text-destructive">
              {errorNuevo}
            </p>
          )}
        </form>
      )}

      {cesiones.isPending && (
        <div role="status" aria-label="Cargando cesiones" className="flex flex-col gap-2">
          <Skeleton className="h-10 w-full" />
        </div>
      )}

      {cesiones.isError && (
        <Card role="alert" className="border-destructive">
          <CardContent className="flex items-center gap-3 pt-4 text-sm">
            <span className="text-destructive">{cesiones.error.message}</span>
            <Button variant="outline" size="sm" onClick={() => cesiones.refetch()}>
              Reintentar
            </Button>
          </CardContent>
        </Card>
      )}

      {cesiones.isSuccess && cesiones.data.length === 0 && (
        <p className="text-sm text-muted-foreground">Todavía no hay cesiones en este echeq.</p>
      )}

      {cesiones.isSuccess && cesiones.data.length > 0 && (
        <ol className="flex flex-col gap-2">
          {cesiones.data.map((cesion) => (
            <li
              key={cesion.numero}
              className="flex flex-wrap items-center gap-2 rounded-md border p-3 text-sm"
            >
              <span className="font-mono font-medium">#{cesion.numero}</span>
              <span className="font-mono">
                {cesion.cuitCedente} → {cesion.cuitCesionario}
              </span>
              <span className="text-muted-foreground">{cesion.domicilioCesionario}</span>
              <Badge className={APARIENCIA_CESION[cesion.estado]}>{cesion.estado}</Badge>
              {cesion.estado === 'Solicitada' && (
                <span className="ml-auto flex gap-2">
                  <Button
                    size="sm"
                    disabled={resolver.isPending}
                    onClick={() => alResolver(cesion.numero, true)}
                  >
                    Aceptar
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    disabled={resolver.isPending}
                    onClick={() => alResolver(cesion.numero, false)}
                  >
                    Rechazar
                  </Button>
                  <Button
                    size="sm"
                    variant="ghost"
                    disabled={anular.isPending}
                    onClick={() =>
                      anular.mutate(
                        { idecheq, numero: cesion.numero },
                        {
                          onSuccess: () => toast.success(`Cesión ${cesion.numero} anulada.`),
                          onError: (error) =>
                            toast.error('No se pudo anular.', { description: error.message }),
                        },
                      )
                    }
                  >
                    Anular
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
