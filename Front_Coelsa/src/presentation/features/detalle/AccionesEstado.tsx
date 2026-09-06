// Acciones de cambio de estado (RF-F05): solo se ofrecen los destinos válidos
// según la máquina de estados; Rechazado exige motivo (códigos del SPEC 5.4).
import { useState } from 'react';
import { toast } from 'sonner';
import { Button } from '@/presentation/ui/button';
import { Label } from '@/presentation/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/presentation/ui/select';
import { destinosDesde } from '@/domain/transiciones';
import { ETIQUETAS_MOTIVO_RECHAZO } from '@/domain/tipos';
import type { EstadoInstrumento, Instrumento, MotivoRechazo } from '@/domain/tipos';
import type { TipoInstrumentoForm } from '@/domain/estrategias/estrategiaCreacion';
import type { IPuertoInstrumentos } from '@/application/puertos';
import { useCambiarEstado } from '@/application/hooks/useCambiarEstado';

const MOTIVOS = Object.entries(ETIQUETAS_MOTIVO_RECHAZO).map(([codigo, etiqueta]) => ({
  codigo: Number(codigo) as MotivoRechazo,
  etiqueta: `${codigo} · ${etiqueta}`,
}));

interface Props {
  tipo: TipoInstrumentoForm;
  instrumento: Instrumento;
  puerto?: IPuertoInstrumentos;
}

export default function AccionesEstado({ tipo, instrumento, puerto }: Props) {
  const cambio = useCambiarEstado(tipo, puerto);
  const [rechazando, setRechazando] = useState(false);
  const [motivo, setMotivo] = useState<string>('');

  const destinos = destinosDesde(instrumento.estado);

  function aplicar(nuevo: EstadoInstrumento, motivoRechazo?: MotivoRechazo) {
    cambio.mutate(
      { identificador: instrumento.identificador, request: { estado: nuevo, motivoRechazo } },
      {
        onSuccess: () => {
          toast.success(`Estado cambiado a ${nuevo}.`);
          setRechazando(false);
          setMotivo('');
        },
        onError: (error) => {
          // 422 por carrera con otro usuario: el detalle viene del server y la
          // invalidación del hook ya refrescó el recurso.
          toast.error('No se pudo cambiar el estado.', { description: error.message });
        },
      },
    );
  }

  if (destinos.length === 0) {
    return <p className="text-sm text-muted-foreground">Estado terminal: sin acciones posibles.</p>;
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap gap-2">
        {destinos
          .filter((destino) => destino !== 'Rechazado')
          .map((destino) => (
            <Button
              key={destino}
              variant={destino === 'Anulado' ? 'outline' : 'default'}
              disabled={cambio.isPending}
              onClick={() => aplicar(destino)}
            >
              {destino}
            </Button>
          ))}
        {destinos.includes('Rechazado') && !rechazando && (
          <Button
            variant="destructive"
            disabled={cambio.isPending}
            onClick={() => setRechazando(true)}
          >
            Rechazar
          </Button>
        )}
      </div>

      {rechazando && (
        <div className="flex flex-col gap-2 rounded-md border border-destructive/30 p-3">
          <Label htmlFor="motivo-rechazo">Motivo de rechazo (obligatorio)</Label>
          <div className="flex flex-wrap gap-2">
            <Select value={motivo} onValueChange={setMotivo}>
              <SelectTrigger id="motivo-rechazo" className="w-64">
                <SelectValue placeholder="Seleccioná un motivo" />
              </SelectTrigger>
              <SelectContent>
                {MOTIVOS.map((m) => (
                  <SelectItem key={m.codigo} value={String(m.codigo)}>
                    {m.etiqueta}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Button
              variant="destructive"
              disabled={cambio.isPending || motivo === ''}
              onClick={() => aplicar('Rechazado', Number(motivo) as MotivoRechazo)}
            >
              Confirmar rechazo
            </Button>
            <Button
              variant="ghost"
              disabled={cambio.isPending}
              onClick={() => setRechazando(false)}
            >
              Cancelar
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
