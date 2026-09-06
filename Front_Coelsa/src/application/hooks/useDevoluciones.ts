// Hooks de pedidos de devolución de echeqs (RF-F12): listado, solicitud,
// resolución por el tenedor y anulación por el solicitante.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { UseMutationResult, UseQueryResult } from '@tanstack/react-query';
import type { Devolucion } from '@/domain/tipos';
import type { IPuertoDevoluciones } from '@/application/puertos';
import type { ErrorCoelsa } from '@/infrastructure/clienteHttp';
import { apiDevoluciones } from '@/infrastructure/apiCoelsa';
import { claveDevoluciones, prefijoTipo } from '@/application/clavesConsulta';

export interface VariablesSolicitarDevolucion {
  idecheq: string;
  cuitSolicitante: string;
  motivo?: string | null;
}

export interface VariablesResolverDevolucion {
  idecheq: string;
  numero: number;
  aceptada: boolean;
  cuitResolutor: string;
}

export function useDevoluciones(
  idecheq: string,
  puerto: IPuertoDevoluciones = apiDevoluciones,
): UseQueryResult<Devolucion[], ErrorCoelsa> {
  return useQuery({
    queryKey: claveDevoluciones(idecheq),
    queryFn: ({ signal }) => puerto.listar(idecheq, signal),
    enabled: idecheq.trim().length > 0,
  });
}

export function useSolicitarDevolucion(
  puerto: IPuertoDevoluciones = apiDevoluciones,
): UseMutationResult<Devolucion, ErrorCoelsa, VariablesSolicitarDevolucion> {
  const cliente = useQueryClient();

  return useMutation({
    mutationFn: (variables: VariablesSolicitarDevolucion) =>
      puerto.solicitar(variables.idecheq, variables.cuitSolicitante, variables.motivo),
    onSuccess: (_data, variables) => {
      cliente.invalidateQueries({ queryKey: claveDevoluciones(variables.idecheq) });
      cliente.invalidateQueries({ queryKey: prefijoTipo('Echeq') });
    },
  });
}

export function useResolverDevolucion(
  puerto: IPuertoDevoluciones = apiDevoluciones,
): UseMutationResult<Devolucion, ErrorCoelsa, VariablesResolverDevolucion> {
  const cliente = useQueryClient();

  return useMutation({
    mutationFn: (variables: VariablesResolverDevolucion) =>
      puerto.resolver(
        variables.idecheq,
        variables.numero,
        variables.aceptada,
        variables.cuitResolutor,
      ),
    onSuccess: (_data, variables) => {
      cliente.invalidateQueries({ queryKey: claveDevoluciones(variables.idecheq) });
      cliente.invalidateQueries({ queryKey: prefijoTipo('Echeq') });
    },
  });
}

export function useAnularDevolucion(
  puerto: IPuertoDevoluciones = apiDevoluciones,
): UseMutationResult<void, ErrorCoelsa, { idecheq: string; numero: number }> {
  const cliente = useQueryClient();

  return useMutation({
    mutationFn: (variables: { idecheq: string; numero: number }) =>
      puerto.anular(variables.idecheq, variables.numero),
    onSuccess: (_data, variables) => {
      cliente.invalidateQueries({ queryKey: claveDevoluciones(variables.idecheq) });
      cliente.invalidateQueries({ queryKey: prefijoTipo('Echeq') });
    },
  });
}
