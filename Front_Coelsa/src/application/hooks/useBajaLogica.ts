// Hook de baja lógica (RF-F06, espejo de RF-06).
// Tras el DELETE invalida el tipo: el instrumento deja de listarse.
import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { UseMutationResult } from '@tanstack/react-query';
import type { TipoInstrumentoForm } from '@/domain/estrategias/estrategiaCreacion';
import type { IPuertoInstrumentos } from '@/application/puertos';
import type { ErrorCoelsa } from '@/infrastructure/clienteHttp';
import { apiInstrumentos } from '@/infrastructure/apiCoelsa';
import { prefijoTipo } from '@/application/clavesConsulta';

export function useBajaLogica(
  tipo: TipoInstrumentoForm,
  puerto: IPuertoInstrumentos = apiInstrumentos,
): UseMutationResult<void, ErrorCoelsa, string> {
  const cliente = useQueryClient();

  return useMutation({
    mutationFn: (identificador: string) =>
      tipo === 'ChequeFisico'
        ? puerto.eliminarCheque(identificador)
        : puerto.eliminarEcheq(identificador),
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: prefijoTipo(tipo) });
    },
  });
}
