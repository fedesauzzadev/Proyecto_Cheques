// Baja lógica con confirmación (RF-F06, espejo de RF-06).
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { Button } from '@/presentation/ui/button';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/presentation/ui/alert-dialog';
import type { Moneda } from '@/domain/tipos';
import type { TipoInstrumentoForm } from '@/domain/estrategias/estrategiaCreacion';
import type { IPuertoInstrumentos } from '@/application/puertos';
import { useBajaLogica } from '@/application/hooks/useBajaLogica';
import { formatearMonto } from '../consulta/formato';

interface Props {
  tipo: TipoInstrumentoForm;
  identificador: string;
  monto: number;
  moneda: Moneda;
  puerto?: IPuertoInstrumentos;
}

export default function BotonBaja({ tipo, identificador, monto, moneda, puerto }: Props) {
  const navigate = useNavigate();
  const baja = useBajaLogica(tipo, puerto);

  function confirmar() {
    baja.mutate(identificador, {
      onSuccess: () => {
        toast.success('Instrumento dado de baja.');
        navigate('/');
      },
      onError: (error) => {
        toast.error('No se pudo dar de baja.', { description: error.message });
      },
    });
  }

  return (
    <AlertDialog>
      <AlertDialogTrigger asChild>
        <Button variant="outline" className="text-destructive">
          Dar de baja
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>¿Dar de baja el instrumento?</AlertDialogTitle>
          <AlertDialogDescription>
            Se dará de baja <span className="font-mono">{identificador}</span> por{' '}
            {formatearMonto(monto, moneda)}. El registro dejará de listarse en las consultas.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Cancelar</AlertDialogCancel>
          <AlertDialogAction onClick={confirmar} disabled={baja.isPending}>
            {baja.isPending ? 'Dando de baja…' : 'Confirmar baja'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
