// Paneles de estado del envío: error del servidor, conflicto 409 con nueva clave,
// replay explícito y la clave del intento (transparencia didáctica de RF-F04).
import { Link } from 'react-router-dom';
import { Button } from '@/presentation/ui/button';
import { Card, CardContent } from '@/presentation/ui/card';

interface Props {
  intentoId: string;
  errorServidor: string | null;
  conflicto: string | null;
  replayHref: string | null;
  onNuevaClave: () => void;
}

export default function EstadoEnvio({
  intentoId,
  errorServidor,
  conflicto,
  replayHref,
  onNuevaClave,
}: Props) {
  return (
    <>
      <p className="text-xs text-muted-foreground">
        Intento: <span className="font-mono">{intentoId.slice(0, 8)}…</span> (viaja como{' '}
        <span className="font-mono">Idempotency-Key</span>)
      </p>

      {errorServidor && (
        <Card role="alert" className="border-destructive">
          <CardContent className="pt-4 text-sm text-destructive">{errorServidor}</CardContent>
        </Card>
      )}

      {conflicto && (
        <Card role="alert" className="border-amber-500">
          <CardContent className="flex flex-col items-start gap-3 pt-4">
            <p className="text-sm">
              <strong>Conflicto:</strong> {conflicto} El identificador ya existe con otro intento.
            </p>
            <Button type="button" variant="outline" onClick={onNuevaClave}>
              Reintentar con nueva clave
            </Button>
          </CardContent>
        </Card>
      )}

      {replayHref && (
        <Card role="status" className="border-green-600">
          <CardContent className="pt-4 text-sm">
            El instrumento ya existía para este intento (
            <span className="font-mono">Idempotent-Replay</span>).{' '}
            <Link to={replayHref} className="text-primary underline underline-offset-4">
              Ver el instrumento
            </Link>
          </CardContent>
        </Card>
      )}
    </>
  );
}
