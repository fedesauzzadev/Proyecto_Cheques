// Aceptación o repudio del beneficiario sobre un echeq pendiente (RF-F10 + D2:
// el repudio exige motivo de hasta 280 caracteres).
import { useState } from 'react';
import { toast } from 'sonner';
import { Button } from '@/presentation/ui/button';
import { Input } from '@/presentation/ui/input';
import { Label } from '@/presentation/ui/label';
import type { IPuertoAceptacion } from '@/application/puertos';
import { useAceptarEcheq } from '@/application/hooks/useAceptarEcheq';

interface Props {
  idecheq: string;
  puerto?: IPuertoAceptacion;
}

export default function SeccionAceptacion({ idecheq, puerto }: Props) {
  const aceptar = useAceptarEcheq(puerto);
  const [repudiando, setRepudiando] = useState(false);
  const [motivo, setMotivo] = useState('');
  const [errorMotivo, setErrorMotivo] = useState<string | null>(null);

  function alAceptar() {
    setErrorMotivo(null);
    aceptar.mutate(
      { idecheq, aceptada: true },
      {
        onSuccess: (echeq) => {
          toast.success('Echeq aceptado: entró en circulación.', {
            description: `IDECHEQ ${echeq.identificador} → ${echeq.estado}.`,
          });
        },
        onError: (error) => {
          toast.error('No se pudo registrar la respuesta.', { description: error.message });
        },
      },
    );
  }

  function alRepudiar() {
    const normalizado = motivo.trim();
    if (normalizado === '' || normalizado.length > 280) {
      setErrorMotivo('El repudio exige un motivo de hasta 280 caracteres.');
      return;
    }
    setErrorMotivo(null);
    aceptar.mutate(
      { idecheq, aceptada: false, motivo: normalizado },
      {
        onSuccess: (echeq) => {
          toast.success('Echeq repudiado.', {
            description: `IDECHEQ ${echeq.identificador} → ${echeq.estado}.`,
          });
          setRepudiando(false);
          setMotivo('');
        },
        onError: (error) => {
          toast.error('No se pudo registrar la respuesta.', { description: error.message });
        },
      },
    );
  }

  return (
    <div className="flex flex-col gap-3">
      <p className="text-sm text-muted-foreground">
        Este echeq está <strong>pendiente de aceptación</strong>: como beneficiario podés aceptarlo
        (entra en circulación) o repudiarlo (queda terminal, con motivo).
      </p>
      {!repudiando ? (
        <div className="flex flex-wrap gap-2">
          <Button disabled={aceptar.isPending} onClick={alAceptar}>
            {aceptar.isPending ? 'Registrando…' : 'Aceptar'}
          </Button>
          <Button
            variant="destructive"
            disabled={aceptar.isPending}
            onClick={() => setRepudiando(true)}
          >
            Repudiar
          </Button>
        </div>
      ) : (
        <div className="flex flex-col gap-2">
          <Label htmlFor="motivo-repudio">Motivo del repudio *</Label>
          <div className="flex flex-wrap gap-2">
            <Input
              id="motivo-repudio"
              placeholder="No reconozco la operación"
              value={motivo}
              onChange={(evento) => setMotivo(evento.target.value)}
              aria-invalid={errorMotivo !== null}
              className="max-w-md"
            />
            <Button
              variant="destructive"
              disabled={aceptar.isPending}
              onClick={alRepudiar}
            >
              {aceptar.isPending ? 'Registrando…' : 'Confirmar repudio'}
            </Button>
            <Button
              type="button"
              variant="ghost"
              disabled={aceptar.isPending}
              onClick={() => {
                setRepudiando(false);
                setMotivo('');
                setErrorMotivo(null);
              }}
            >
              Cancelar
            </Button>
          </div>
          {errorMotivo && (
            <p role="alert" className="text-sm text-destructive">
              {errorMotivo}
            </p>
          )}
        </div>
      )}
    </div>
  );
}
