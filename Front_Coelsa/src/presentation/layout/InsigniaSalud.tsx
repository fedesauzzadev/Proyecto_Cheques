// Badge de salud del backend (RF-F08, espejo de RF-07).
// Verde = operativa, ámbar = degradada, rojo = con fallas o inalcanzable.
import { Badge } from '@/presentation/ui/badge';
import { cn } from '@/lib/utils';
import { useSaludBackend } from '@/application/hooks/useSaludBackend';
import type { IPuertoSalud } from '@/application/puertos';

interface Props {
  puerto?: IPuertoSalud;
}

export default function InsigniaSalud({ puerto }: Props = {}) {
  const salud = useSaludBackend(puerto);

  if (salud.isPending) {
    return (
      <Badge variant="outline" role="status" title="Consultando el estado de la API…">
        <span className="mr-1.5 inline-block size-2 animate-pulse rounded-full bg-muted-foreground" />
        Verificando API…
      </Badge>
    );
  }

  if (salud.isError) {
    return (
      <Badge
        variant="destructive"
        role="status"
        title={`No se pudo consultar el estado de la API: ${salud.error.message}`}
      >
        <span className="mr-1.5 inline-block size-2 rounded-full bg-current" />
        API no disponible
      </Badge>
    );
  }

  const { status, checks } = salud.data;
  const detalle = `PostgreSQL: ${checks.postgres} · Redis: ${checks.redis}`;

  if (status === 'Healthy') {
    return (
      <Badge variant="outline" role="status" title={`API operativa. ${detalle}`}>
        <span className="mr-1.5 inline-block size-2 rounded-full bg-green-600" />
        API operativa
      </Badge>
    );
  }

  return (
    <Badge
      variant="outline"
      role="status"
      title={`API ${status === 'Degraded' ? 'degradada' : 'con fallas'}. ${detalle}`}
      className={cn(
        status === 'Degraded' ? 'border-amber-500 text-amber-600' : 'border-red-500 text-red-600',
      )}
    >
      <span
        className={cn(
          'mr-1.5 inline-block size-2 rounded-full',
          status === 'Degraded' ? 'bg-amber-500' : 'bg-red-600',
        )}
      />
      {status === 'Degraded' ? 'API degradada' : 'API con fallas'}
    </Badge>
  );
}
