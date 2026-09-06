// Badge de salud del backend (RF-F08, espejo de RF-07).
// Verde = operativa, ámbar = degradada, rojo = con fallas o inalcanzable.
// El botón de refresh permite reconsultar manualmente (útil tras un cold start,
// cuando la primera consulta puede fallar por timeout aunque la API esté sana).
import type { ReactNode } from 'react';
import { RefreshCw } from 'lucide-react';
import { Badge } from '@/presentation/ui/badge';
import { Button } from '@/presentation/ui/button';
import { cn } from '@/lib/utils';
import { useSaludBackend } from '@/application/hooks/useSaludBackend';
import type { IPuertoSalud } from '@/application/puertos';

interface Props {
  puerto?: IPuertoSalud;
}

export default function InsigniaSalud({ puerto }: Props = {}) {
  const salud = useSaludBackend(puerto);

  let insignia: ReactNode;
  if (salud.isPending) {
    insignia = (
      <Badge variant="outline" role="status" title="Consultando el estado de la API…">
        <span className="mr-1.5 inline-block size-2 animate-pulse rounded-full bg-muted-foreground" />
        Verificando API…
      </Badge>
    );
  } else if (salud.isError) {
    insignia = (
      <Badge
        variant="destructive"
        role="status"
        title={`No se pudo consultar el estado de la API: ${salud.error.message}. Probá con el botón de refresh.`}
      >
        <span className="mr-1.5 inline-block size-2 rounded-full bg-current" />
        API no disponible
      </Badge>
    );
  } else if (salud.data.status === 'Healthy') {
    const detalle = `PostgreSQL: ${salud.data.checks.postgres} · Redis: ${salud.data.checks.redis}`;
    insignia = (
      <Badge variant="outline" role="status" title={`API operativa. ${detalle}`}>
        <span className="mr-1.5 inline-block size-2 rounded-full bg-green-600" />
        API operativa
      </Badge>
    );
  } else {
    const degradada = salud.data.status === 'Degraded';
    const detalle = `PostgreSQL: ${salud.data.checks.postgres} · Redis: ${salud.data.checks.redis}`;
    insignia = (
      <Badge
        variant="outline"
        role="status"
        title={`API ${degradada ? 'degradada' : 'con fallas'}. ${detalle}`}
        className={cn(
          degradada ? 'border-amber-500 text-amber-600' : 'border-red-500 text-red-600',
        )}
      >
        <span
          className={cn(
            'mr-1.5 inline-block size-2 rounded-full',
            degradada ? 'bg-amber-500' : 'bg-red-600',
          )}
        />
        {degradada ? 'API degradada' : 'API con fallas'}
      </Badge>
    );
  }

  return (
    <span className="flex items-center gap-1">
      {insignia}
      <Button
        type="button"
        variant="ghost"
        size="icon"
        aria-label="Volver a consultar el estado de la API"
        title="Volver a consultar el estado de la API"
        disabled={salud.isFetching}
        onClick={() => {
          void salud.refetch();
        }}
      >
        <RefreshCw className={cn('size-4', salud.isFetching && 'animate-spin')} aria-hidden />
      </Button>
    </span>
  );
}
