// Hook de cambio de estado (RF-F05, espejo de RF-05).
// La UI solo ofrece destinos válidos (transiciones.ts); tras el cambio invalida el tipo.
import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { UseMutationResult } from '@tanstack/react-query';
import type { TipoInstrumentoForm } from '@/domain/estrategias/estrategiaCreacion';
import type { CambiarEstadoRequest, ChequeResponse, EcheqResponse } from '@/domain/tipos';
import type { IPuertoInstrumentos } from '@/application/puertos';
import type { ErrorCoelsa } from '@/infrastructure/clienteHttp';
import { apiInstrumentos } from '@/infrastructure/apiCoelsa';
import { prefijoTipo } from '@/application/clavesConsulta';

export interface VariablesCambioEstado {
  identificador: string;
  request: CambiarEstadoRequest;
}

export function useCambiarEstado(
  tipo: 'ChequeFisico',
  puerto?: IPuertoInstrumentos,
): UseMutationResult<ChequeResponse, ErrorCoelsa, VariablesCambioEstado>;
export function useCambiarEstado(
  tipo: 'Echeq',
  puerto?: IPuertoInstrumentos,
): UseMutationResult<EcheqResponse, ErrorCoelsa, VariablesCambioEstado>;
export function useCambiarEstado(
  tipo: TipoInstrumentoForm,
  puerto: IPuertoInstrumentos = apiInstrumentos,
) {
  const cliente = useQueryClient();

  return useMutation<ChequeResponse | EcheqResponse, ErrorCoelsa, VariablesCambioEstado>({
    mutationFn: (variables: VariablesCambioEstado) =>
      tipo === 'ChequeFisico'
        ? puerto.cambiarEstadoCheque(variables.identificador, variables.request)
        : puerto.cambiarEstadoEcheq(variables.identificador, variables.request),
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: prefijoTipo(tipo) });
    },
  });
}
