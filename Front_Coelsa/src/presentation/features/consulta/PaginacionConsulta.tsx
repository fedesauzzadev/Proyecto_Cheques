import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '@/presentation/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/presentation/ui/select';

const TAMANOS = [10, 25, 50];

interface Props {
  page: number;
  pageSize: number;
  totalPages: number;
  totalCount: number;
  onPagina: (page: number) => void;
  onTamano: (pageSize: number) => void;
}

export default function PaginacionConsulta({
  page,
  pageSize,
  totalPages,
  totalCount,
  onPagina,
  onTamano,
}: Props) {
  return (
    <nav
      aria-label="Paginación de resultados"
      className="flex flex-wrap items-center gap-3 pt-2 text-sm"
    >
      <div className="flex items-center gap-1">
        <Button
          variant="outline"
          size="sm"
          disabled={page <= 1}
          onClick={() => onPagina(page - 1)}
          aria-label="Página anterior"
        >
          <ChevronLeft className="size-4" aria-hidden />
          Anterior
        </Button>
        <span aria-live="polite" className="px-2 text-muted-foreground">
          Página {page} de {totalPages} ({totalCount} resultados)
        </span>
        <Button
          variant="outline"
          size="sm"
          disabled={page >= totalPages}
          onClick={() => onPagina(page + 1)}
          aria-label="Página siguiente"
        >
          Siguiente
          <ChevronRight className="size-4" aria-hidden />
        </Button>
      </div>
      <div className="ml-auto flex items-center gap-2">
        <label htmlFor="tamano-pagina" className="text-muted-foreground">
          Por página
        </label>
        <Select value={String(pageSize)} onValueChange={(valor) => onTamano(Number(valor))}>
          <SelectTrigger id="tamano-pagina" className="w-20" aria-label="Tamaño de página">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {TAMANOS.map((tamano) => (
              <SelectItem key={tamano} value={String(tamano)}>
                {tamano}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
    </nav>
  );
}
