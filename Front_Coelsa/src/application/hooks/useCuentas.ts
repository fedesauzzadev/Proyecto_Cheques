// Hooks de cuentas corrientes y e-chequeras (Fase B).
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { UseMutationResult, UseQueryResult } from '@tanstack/react-query';
import { esCuitValido } from '@/domain/validadorCuit';
import { esCbuValido } from '@/domain/cbu';
import { esDocumentoValido, esTipoDocumento } from '@/domain/documento';
import type {
  Chequera,
  CrearCuentaRequest,
  Cuenta,
  TipoDocumento,
  Titular,
} from '@/domain/tipos';
import type { IPuertoCuentas, ResultadoCreacion } from '@/application/puertos';
import type { ErrorCoelsa } from '@/infrastructure/clienteHttp';
import { apiCuentas } from '@/infrastructure/apiCoelsa';
import { claveChequeras, claveCuentas, prefijoCuentas } from '@/application/clavesConsulta';

export function useCuentas(
  cuit: string,
  puerto: IPuertoCuentas = apiCuentas,
): UseQueryResult<Cuenta[], ErrorCoelsa> {
  return useQuery({
    queryKey: claveCuentas(cuit),
    queryFn: ({ signal }) => puerto.listarPorCuit(cuit, signal),
    enabled: esCuitValido(cuit),
  });
}

export function useChequeras(
  cbu: string,
  puerto: IPuertoCuentas = apiCuentas,
): UseQueryResult<Chequera[], ErrorCoelsa> {
  return useQuery({
    queryKey: claveChequeras(cbu),
    queryFn: ({ signal }) => puerto.listarChequeras(cbu, signal),
    enabled: esCbuValido(cbu),
  });
}

export interface VariablesCrearCuenta {
  request: CrearCuentaRequest;
  idempotencyKey: string;
}

export function useCrearCuenta(
  puerto: IPuertoCuentas = apiCuentas,
): UseMutationResult<ResultadoCreacion<Cuenta>, ErrorCoelsa, VariablesCrearCuenta> {
  const cliente = useQueryClient();

  return useMutation({
    mutationFn: (variables: VariablesCrearCuenta) =>
      puerto.crear(variables.request, variables.idempotencyKey),
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: prefijoCuentas() });
    },
  });
}

export interface VariablesSolicitarChequera {
  cbu: string;
  idempotencyKey: string;
}

export function useSolicitarChequera(
  puerto: IPuertoCuentas = apiCuentas,
): UseMutationResult<ResultadoCreacion<Chequera>, ErrorCoelsa, VariablesSolicitarChequera> {
  const cliente = useQueryClient();

  return useMutation({
    mutationFn: (variables: VariablesSolicitarChequera) =>
      puerto.solicitarChequera(variables.cbu, variables.idempotencyKey),
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: prefijoCuentas() });
    },
  });
}

/** Padrón simulado ("lupa", Fase B3): solo dispara con tipo y número válidos. */
export function useTitular(
  tipoDoc: string,
  numero: string,
  puerto: IPuertoCuentas = apiCuentas,
): UseQueryResult<Titular, ErrorCoelsa> {
  const tipo = esTipoDocumento(tipoDoc) ? (tipoDoc as TipoDocumento) : null;
  return useQuery({
    queryKey: ['cuentas', 'titular', tipoDoc, numero],
    queryFn: ({ signal }) => puerto.buscarTitular(tipo as TipoDocumento, numero, signal),
    enabled: tipo !== null && esDocumentoValido(tipo, numero),
  });
}
