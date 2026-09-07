// Hooks de cesiones de echeqs "no a la orden" (Fase D1): listado, solicitud
// por el tenedor, resolución por el cesionario y anulación por el cedente.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { UseMutationResult, UseQueryResult } from '@tanstack/react-query';
import type { Cesion } from '@/domain/tipos';
import type { IPuertoCesiones } from '@/application/puertos';
import type { ErrorCoelsa } from '@/infrastructure/clienteHttp';
import { apiCesiones } from '@/infrastructure/apiCoelsa';
import { claveCesiones, prefijoTipo } from '@/application/clavesConsulta';

export interface VariablesSolicitarCesion {
  idecheq: string;
  cuitCesionario: string;
  domicilioCesionario: string;
}

export interface VariablesResolverCesion {
  idecheq: string;
  numero: number;
  aceptada: boolean;
  cuitResolutor: string;
}

export function useCesiones(
  idecheq: string,
  puerto: IPuertoCesiones = apiCesiones,
): UseQueryResult<Cesion[], ErrorCoelsa> {
  return useQuery({
    queryKey: claveCesiones(idecheq),
    queryFn: ({ signal }) => puerto.listar(idecheq, signal),
    enabled: idecheq.trim().length > 0,
  });
}

export function useSolicitarCesion(
  puerto: IPuertoCesiones = apiCesiones,
): UseMutationResult<Cesion, ErrorCoelsa, VariablesSolicitarCesion> {
  const cliente = useQueryClient();

  return useMutation({
    mutationFn: (variables: VariablesSolicitarCesion) =>
      puerto.solicitar(variables.idecheq, variables.cuitCesionario, variables.domicilioCesionario),
    onSuccess: (_data, variables) => {
      cliente.invalidateQueries({ queryKey: claveCesiones(variables.idecheq) });
      cliente.invalidateQueries({ queryKey: prefijoTipo('Echeq') });
    },
  });
}

export function useResolverCesion(
  puerto: IPuertoCesiones = apiCesiones,
): UseMutationResult<Cesion, ErrorCoelsa, VariablesResolverCesion> {
  const cliente = useQueryClient();

  return useMutation({
    mutationFn: (variables: VariablesResolverCesion) =>
      puerto.resolver(
        variables.idecheq,
        variables.numero,
        variables.aceptada,
        variables.cuitResolutor,
      ),
    onSuccess: (_data, variables) => {
      cliente.invalidateQueries({ queryKey: claveCesiones(variables.idecheq) });
      cliente.invalidateQueries({ queryKey: prefijoTipo('Echeq') });
    },
  });
}

export function useAnularCesion(
  puerto: IPuertoCesiones = apiCesiones,
): UseMutationResult<void, ErrorCoelsa, { idecheq: string; numero: number }> {
  const cliente = useQueryClient();

  return useMutation({
    mutationFn: (variables: { idecheq: string; numero: number }) =>
      puerto.anular(variables.idecheq, variables.numero),
    onSuccess: (_data, variables) => {
      cliente.invalidateQueries({ queryKey: claveCesiones(variables.idecheq) });
      cliente.invalidateQueries({ queryKey: prefijoTipo('Echeq') });
    },
  });
}
