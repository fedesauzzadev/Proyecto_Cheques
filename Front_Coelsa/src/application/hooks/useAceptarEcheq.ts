// Hook de aceptación/repudio de echeqs pendientes (RF-F10 + D2: repudio con motivo).
import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { UseMutationResult } from '@tanstack/react-query';
import type { EcheqResponse } from '@/domain/tipos';
import type { IPuertoAceptacion } from '@/application/puertos';
import type { ErrorCoelsa } from '@/infrastructure/clienteHttp';
import { apiAceptacion } from '@/infrastructure/apiCoelsa';
import { prefijoTipo } from '@/application/clavesConsulta';

export interface VariablesAceptacion {
  idecheq: string;
  aceptada: boolean;
  /** Obligatorio al repudiar (hasta 280); prohibido al aceptar. */
  motivo?: string | null;
}

export function useAceptarEcheq(
  puerto: IPuertoAceptacion = apiAceptacion,
): UseMutationResult<EcheqResponse, ErrorCoelsa, VariablesAceptacion> {
  const cliente = useQueryClient();

  return useMutation({
    mutationFn: (variables: VariablesAceptacion) =>
      puerto.aceptar(variables.idecheq, variables.aceptada, variables.motivo),
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: prefijoTipo('Echeq') });
    },
  });
}
