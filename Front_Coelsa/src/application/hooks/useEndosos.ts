// Hooks de endosos de echeqs (RF-F11): cadena, propuesta, resolución y anulación.
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { UseMutationResult, UseQueryResult } from '@tanstack/react-query';
import type { Endoso } from '@/domain/tipos';
import type { IPuertoEndosos } from '@/application/puertos';
import type { ErrorCoelsa } from '@/infrastructure/clienteHttp';
import { apiEndosos } from '@/infrastructure/apiCoelsa';
import { claveEndosos, prefijoTipo } from '@/application/clavesConsulta';

export interface VariablesProponerEndoso {
  idecheq: string;
  cuitEndosatario: string;
}

export interface VariablesResolverEndoso {
  idecheq: string;
  orden: number;
  admitido: boolean;
  cuit: string;
}

export function useEndosos(
  idecheq: string,
  puerto: IPuertoEndosos = apiEndosos,
): UseQueryResult<Endoso[], ErrorCoelsa> {
  return useQuery({
    queryKey: claveEndosos(idecheq),
    queryFn: ({ signal }) => puerto.listar(idecheq, signal),
    enabled: idecheq.trim().length > 0,
  });
}

export function useProponerEndoso(
  puerto: IPuertoEndosos = apiEndosos,
): UseMutationResult<Endoso, ErrorCoelsa, VariablesProponerEndoso> {
  const cliente = useQueryClient();

  return useMutation({
    mutationFn: (variables: VariablesProponerEndoso) =>
      puerto.proponer(variables.idecheq, variables.cuitEndosatario),
    onSuccess: (_data, variables) => {
      cliente.invalidateQueries({ queryKey: claveEndosos(variables.idecheq) });
      cliente.invalidateQueries({ queryKey: prefijoTipo('Echeq') });
    },
  });
}

export function useResolverEndoso(
  puerto: IPuertoEndosos = apiEndosos,
): UseMutationResult<Endoso, ErrorCoelsa, VariablesResolverEndoso> {
  const cliente = useQueryClient();

  return useMutation({
    mutationFn: (variables: VariablesResolverEndoso) =>
      puerto.resolver(variables.idecheq, variables.orden, variables.admitido, variables.cuit),
    onSuccess: (_data, variables) => {
      cliente.invalidateQueries({ queryKey: claveEndosos(variables.idecheq) });
      cliente.invalidateQueries({ queryKey: prefijoTipo('Echeq') });
    },
  });
}

export function useAnularEndoso(
  puerto: IPuertoEndosos = apiEndosos,
): UseMutationResult<void, ErrorCoelsa, { idecheq: string; orden: number }> {
  const cliente = useQueryClient();

  return useMutation({
    mutationFn: (variables: { idecheq: string; orden: number }) =>
      puerto.anular(variables.idecheq, variables.orden),
    onSuccess: (_data, variables) => {
      cliente.invalidateQueries({ queryKey: claveEndosos(variables.idecheq) });
      cliente.invalidateQueries({ queryKey: prefijoTipo('Echeq') });
    },
  });
}
