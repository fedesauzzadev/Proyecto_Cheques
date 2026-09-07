// Certificado para ejercer acciones civiles (Fase D3, espejo de RF-14):
// CUD determinista + datos del rechazo. Solo para echeqs en estado Rechazado.
import { Badge } from '@/presentation/ui/badge';
import { Button } from '@/presentation/ui/button';
import { Card, CardContent } from '@/presentation/ui/card';
import { Skeleton } from '@/presentation/ui/skeleton';
import { ETIQUETAS_MOTIVO_RECHAZO } from '@/domain/tipos';
import type { IPuertoCertificado } from '@/application/puertos';
import { useCertificado } from '@/application/hooks/useCertificado';

interface Props {
  idecheq: string;
  puerto?: IPuertoCertificado;
}

export default function TarjetaCertificado({ idecheq, puerto }: Props) {
  const certificado = useCertificado(idecheq, true, puerto);

  if (certificado.isPending) {
    return (
      <div role="status" aria-label="Cargando certificado" className="flex flex-col gap-2">
        <Skeleton className="h-8 w-2/3" />
        <Skeleton className="h-5 w-full" />
      </div>
    );
  }

  // No rechazado (422): la consulta ni se dispara o falla; se muestra aviso.
  if (certificado.isError) {
    return (
      <p className="text-sm text-muted-foreground">
        {certificado.error.message}
      </p>
    );
  }

  const c = certificado.data;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-col gap-1.5">
        <p className="text-xs text-muted-foreground">CUD (Clave Única Digital)</p>
        <p className="break-all font-mono text-sm">{c.cud}</p>
        <p className="text-xs text-muted-foreground">
          Código de visualización: <span className="font-mono">{c.codigoVisualizacion}</span>
        </p>
      </div>
      <dl className="grid grid-cols-1 gap-x-6 gap-y-2 text-sm sm:grid-cols-2">
        <div className="flex justify-between gap-4 sm:block">
          <dt className="text-muted-foreground">Motivo del rechazo</dt>
          <dd>
            <Badge variant="destructive">
              {c.motivoRechazo !== null
                ? `${c.motivoRechazo} · ${ETIQUETAS_MOTIVO_RECHAZO[c.motivoRechazo]}`
                : '—'}
            </Badge>
          </dd>
        </div>
        <div className="flex justify-between gap-4 sm:block">
          <dt className="text-muted-foreground">Monto</dt>
          <dd className="font-medium">
            {c.moneda === 'P' ? '$' : 'US$'} {c.monto.toLocaleString('es-AR')}
          </dd>
        </div>
        <div className="flex justify-between gap-4 sm:block">
          <dt className="text-muted-foreground">Librador</dt>
          <dd>
            <span className="font-mono">{c.cuitLibrador}</span>{' '}
            <span className="text-muted-foreground">{c.nombreLibrador}</span>
          </dd>
        </div>
        <div className="flex justify-between gap-4 sm:block">
          <dt className="text-muted-foreground">Beneficiario</dt>
          <dd>
            <span className="font-mono">{c.cuitBeneficiario}</span>{' '}
            <span className="text-muted-foreground">{c.nombreBeneficiario}</span>
          </dd>
        </div>
        <div className="flex justify-between gap-4 sm:block">
          <dt className="text-muted-foreground">Vencimiento</dt>
          <dd>{c.fechaVencimiento}</dd>
        </div>
        <div className="flex justify-between gap-4 sm:block">
          <dt className="text-muted-foreground">Fecha del rechazo</dt>
          <dd>{c.fechaRechazo ? new Date(c.fechaRechazo).toLocaleString('es-AR') : '—'}</dd>
        </div>
      </dl>
      <Card className="border-dashed print:hidden">
        <CardContent className="pt-4 text-xs text-muted-foreground">
          Certificado simulado para ejercer acciones civiles (art. 61, Ley 24.452). Habilita el
          reclamo judicial por rechazo y documenta la contragarantía en el descuento.
          <div className="mt-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => navigator.clipboard?.writeText(c.cud)}
            >
              Copiar CUD
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
