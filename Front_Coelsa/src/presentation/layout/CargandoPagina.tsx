import { Loader2 } from 'lucide-react';

export default function CargandoPagina() {
  return (
    <div role="status" className="flex flex-col items-center justify-center gap-3 py-24">
      <Loader2 className="size-8 animate-spin text-muted-foreground" aria-hidden />
      <p className="text-sm text-muted-foreground">Cargando…</p>
    </div>
  );
}
