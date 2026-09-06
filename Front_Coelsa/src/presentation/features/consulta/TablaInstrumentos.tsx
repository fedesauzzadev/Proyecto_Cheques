import { Link } from 'react-router-dom';
import {
  Table,
  TableBody,
  TableCaption,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/presentation/ui/table';
import { Skeleton } from '@/presentation/ui/skeleton';
import type { TipoInstrumentoForm } from '@/domain/estrategias/estrategiaCreacion';
import type { ChequeResponse, EcheqResponse, PagedResponse } from '@/domain/tipos';
import EstadoBadge from './EstadoBadge';
import { describirDiferimiento, describirMotivo, formatearFecha, formatearMonto } from './formato';

type Pagina = PagedResponse<ChequeResponse> | PagedResponse<EcheqResponse>;

interface Props {
  tipo: TipoInstrumentoForm;
  /** Null mientras la primera carga está en curso (se muestran esqueletos). */
  pagina: Pagina | null;
  /** True cuando se está cambiando de página con datos previos en pantalla. */
  actualizando?: boolean;
}

function rutaDetalle(tipo: TipoInstrumentoForm): string {
  return tipo === 'ChequeFisico' ? 'cheques' : 'echeqs';
}

export function EsqueletoTabla({ filas = 5 }: { filas?: number }) {
  return (
    <div role="status" aria-label="Cargando instrumentos" className="flex flex-col gap-2">
      {Array.from({ length: filas }, (_, i) => (
        <Skeleton key={i} className="h-10 w-full" />
      ))}
    </div>
  );
}

export default function TablaInstrumentos({ tipo, pagina, actualizando = false }: Props) {
  if (!pagina) return <EsqueletoTabla />;

  return (
    <div aria-busy={actualizando} className={actualizando ? 'opacity-60' : undefined}>
      <div className="overflow-x-auto rounded-md">
        <Table className="min-w-[760px]">
          <TableCaption>
            {pagina.totalCount} {pagina.totalCount === 1 ? 'instrumento' : 'instrumentos'} · página{' '}
            {pagina.page} de {pagina.totalPages}
          </TableCaption>
          <TableHeader>
            <TableRow>
              <TableHead>Identificador</TableHead>
              <TableHead>Librador</TableHead>
              <TableHead>Beneficiario</TableHead>
              <TableHead className="text-right">Monto</TableHead>
              <TableHead>Emisión</TableHead>
              <TableHead>Diferimiento</TableHead>
              <TableHead>Vencimiento</TableHead>
              <TableHead>Estado</TableHead>
              <TableHead>Motivo rechazo</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {pagina.items.map((item) => (
              <TableRow key={item.identificador}>
                <TableCell className="font-mono text-xs">
                  <Link
                    to={`/${rutaDetalle(tipo)}/${item.identificador}`}
                    className="text-primary underline-offset-4 hover:underline"
                  >
                    {item.identificador}
                  </Link>
                </TableCell>
                <TableCell className="font-mono">{item.cuitLibrador}</TableCell>
                <TableCell className="font-mono">{item.cuitBeneficiario}</TableCell>
                <TableCell className="text-right font-medium">
                  {formatearMonto(item.monto, item.moneda)}
                </TableCell>
                <TableCell>{formatearFecha(item.fechaEmision)}</TableCell>
                <TableCell>{describirDiferimiento(item.fechaDiferimiento)}</TableCell>
                <TableCell>{formatearFecha(item.fechaVencimiento)}</TableCell>
                <TableCell>
                  <EstadoBadge estado={item.estado} />
                </TableCell>
                <TableCell className="text-xs">{describirMotivo(item.motivoRechazo)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
