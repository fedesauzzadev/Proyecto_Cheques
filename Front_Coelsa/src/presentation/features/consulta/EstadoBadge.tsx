import { Badge } from '@/presentation/ui/badge';
import { cn } from '@/lib/utils';
import type { EstadoInstrumento } from '@/domain/tipos';

const APARIENCIA: Record<
  EstadoInstrumento,
  { variante: 'default' | 'secondary' | 'destructive' | 'outline'; clase?: string }
> = {
  Emitido: { variante: 'secondary' },
  Depositado: { variante: 'default' },
  Compensado: { variante: 'default', clase: 'bg-indigo-600' },
  Pagado: { variante: 'default', clase: 'bg-green-700' },
  Rechazado: { variante: 'destructive' },
  Anulado: { variante: 'outline' },
};

export default function EstadoBadge({ estado }: { estado: EstadoInstrumento }) {
  const { variante, clase } = APARIENCIA[estado];
  return (
    <Badge variant={variante} className={cn(clase)}>
      {estado}
    </Badge>
  );
}
