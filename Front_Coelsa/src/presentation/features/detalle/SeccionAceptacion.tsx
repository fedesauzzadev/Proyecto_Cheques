// Aceptación o repudio del beneficiario sobre un echeq pendiente (RF-F10).
import { toast } from 'sonner';
import { Button } from '@/presentation/ui/button';
import type { IPuertoAceptacion } from '@/application/puertos';
import { useAceptarEcheq } from '@/application/hooks/useAceptarEcheq';

interface Props {
  idecheq: string;
  puerto?: IPuertoAceptacion;
}

export default function SeccionAceptacion({ idecheq, puerto }: Props) {
  const aceptar = useAceptarEcheq(puerto);

  function resolver(aceptada: boolean) {
    aceptar.mutate(
      { idecheq, aceptada },
      {
        onSuccess: (echeq) => {
          toast.success(aceptada ? 'Echeq aceptado: entró en circulación.' : 'Echeq repudiado.', {
            description: `IDECHEQ ${echeq.identificador} → ${echeq.estado}.`,
          });
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
        (entra en circulación) o repudiarlo (queda terminal).
      </p>
      <div className="flex flex-wrap gap-2">
        <Button disabled={aceptar.isPending} onClick={() => resolver(true)}>
          {aceptar.isPending ? 'Registrando…' : 'Aceptar'}
        </Button>
        <Button variant="destructive" disabled={aceptar.isPending} onClick={() => resolver(false)}>
          Repudiar
        </Button>
      </div>
    </div>
  );
}
