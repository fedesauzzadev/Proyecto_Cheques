// Cadena de endosos del echeq (RF-F11): trazabilidad completa, propuesta de nuevos
// endosos, admisión/repudio por el endosatario y anulación por el endosante.
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
import type { EstadoEndoso } from '@/domain/tipos';
import type { IPuertoEndosos } from '@/application/puertos';
import {
  useAnularEndoso,
  useEndosos,
  useProponerEndoso,
  useResolverEndoso,
} from '@/application/hooks/useEndosos';

const APARIENCIA_ENDOSO: Record<EstadoEndoso, string> = {
  Propuesto: 'bg-secondary text-secondary-foreground',
  Vigente: 'bg-green-700 text-white',
  Repudiado: 'bg-destructive/10 text-destructive',
  Anulado: 'text-muted-foreground',
  Revertido: 'text-muted-foreground',
};

interface Props {
  idecheq: string;
  /** Solo en Emitido se pueden proponer endosos. */
  puedeEndosar: boolean;
  puerto?: IPuertoEndosos;
}

export default function CadenaEndosos({ idecheq, puedeEndosar, puerto }: Props) {
  const cadena = useEndosos(idecheq, puerto);
  const proponer = useProponerEndoso(puerto);
  const resolver = useResolverEndoso(puerto);
  const anular = useAnularEndoso(puerto);

  const [cuitNuevo, setCuitNuevo] = useState('');
  const [errorNuevo, setErrorNuevo] = useState<string | null>(null);
  const [identidad, setIdentidad] = useState('');

  function alProponer(evento: FormEvent) {
    evento.preventDefault();
    const cuit = cuitNuevo.trim();
    if (!esCuitValido(cuit)) {
      setErrorNuevo('Ingresá un CUIT/CUIL válido de 11 dígitos.');
      return;
    }
    setErrorNuevo(null);
    proponer.mutate(
      { idecheq, cuitEndosatario: cuit },
      {
        onSuccess: (endoso) => {
          toast.success(`Endoso ${endoso.orden} propuesto.`);
          setCuitNuevo('');
        },
        onError: (error) => {
          toast.error('No se pudo proponer el endoso.', { description: error.message });
        },
      },
    );
  }

  function alResolver(orden: number, admitido: boolean) {
    const cuit = identidad.trim();
    if (!esCuitValido(cuit)) {
      toast.error('Indicá con qué CUIT actuás (debe ser el endosatario).');
      return;
    }
    resolver.mutate(
      { idecheq, orden, admitido, cuit },
      {
        onSuccess: () => {
          toast.success(admitido ? `Endoso ${orden} admitido.` : `Endoso ${orden} repudiado.`);
        },
        onError: (error) => {
          toast.error('No se pudo resolver el endoso.', { description: error.message });
        },
      },
    );
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="actuar-como">Actuar como (CUIT)</Label>
        <Input
          id="actuar-como"
          inputMode="numeric"
          placeholder="CUIT con el que admitís o repudiás"
          value={identidad}
          onChange={(evento) => setIdentidad(evento.target.value)}
          className="max-w-xs font-mono"
        />
        <p className="text-xs text-muted-foreground">
          Solo el endosatario puede admitir o repudiar cada endoso propuesto.
        </p>
      </div>

      {puedeEndosar && (
        <form onSubmit={alProponer} className="flex flex-col gap-2" noValidate>
          <Label htmlFor="cuit-endosatario">Endosar a (CUIT/CUIL)</Label>
          <div className="flex flex-wrap gap-2">
            <Input
              id="cuit-endosatario"
              inputMode="numeric"
              placeholder="30511222334"
              value={cuitNuevo}
              onChange={(evento) => setCuitNuevo(evento.target.value)}
              aria-invalid={errorNuevo !== null}
              className="max-w-xs font-mono"
            />
            <Button type="submit" disabled={proponer.isPending}>
              {proponer.isPending ? 'Proponiendo…' : 'Proponer endoso'}
            </Button>
          </div>
          {errorNuevo && (
            <p role="alert" className="text-sm text-destructive">
              {errorNuevo}
            </p>
          )}
        </form>
      )}

      {cadena.isPending && (
        <div role="status" aria-label="Cargando cadena de endosos" className="flex flex-col gap-2">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
        </div>
      )}

      {cadena.isError && (
        <Card role="alert" className="border-destructive">
          <CardContent className="flex items-center gap-3 pt-4 text-sm">
            <span className="text-destructive">{cadena.error.message}</span>
            <Button variant="outline" size="sm" onClick={() => cadena.refetch()}>
              Reintentar
            </Button>
          </CardContent>
        </Card>
      )}

      {cadena.isSuccess && cadena.data.length === 0 && (
        <p className="text-sm text-muted-foreground">Todavía no hay endosos en este echeq.</p>
      )}

      {cadena.isSuccess && cadena.data.length > 0 && (
        <ol className="flex flex-col gap-2">
          {cadena.data.map((endoso) => (
            <li
              key={endoso.orden}
              className="flex flex-wrap items-center gap-2 rounded-md border p-3 text-sm"
            >
              <span className="font-mono font-medium">#{endoso.orden}</span>
              <span className="font-mono">
                {endoso.cuitEndosante} → {endoso.cuitEndosatario}
              </span>
              <Badge className={APARIENCIA_ENDOSO[endoso.estado]}>{endoso.estado}</Badge>
              {endoso.estado === 'Propuesto' && (
                <span className="ml-auto flex gap-2">
                  <Button
                    size="sm"
                    disabled={resolver.isPending}
                    onClick={() => alResolver(endoso.orden, true)}
                  >
                    Admitir
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    disabled={resolver.isPending}
                    onClick={() => alResolver(endoso.orden, false)}
                  >
                    Repudiar
                  </Button>
                  <Button
                    size="sm"
                    variant="ghost"
                    disabled={anular.isPending}
                    onClick={() =>
                      anular.mutate(
                        { idecheq, orden: endoso.orden },
                        {
                          onSuccess: () => toast.success(`Endoso ${endoso.orden} anulado.`),
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
