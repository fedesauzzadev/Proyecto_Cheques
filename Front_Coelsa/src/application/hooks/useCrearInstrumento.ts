// Hook de creación idempotente (RF-F04, espejo de RF-02).
// Recibe { request, idempotencyKey }: el GUID lo genera el formulario al montar
// y lo reusa en reintentos del mismo intento. Tras crear, invalida el tipo.
// El genérico T selecciona el tipo exacto de respuesta según el instrumento.
import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { UseMutationResult } from '@tanstack/react-query';
import type { TipoInstrumentoForm } from '@/domain/estrategias/estrategiaCreacion';
import type {
  ChequeResponse,
  CrearChequeFisicoRequest,
  CrearEcheqRequest,
  EcheqResponse,
} from '@/domain/tipos';
import type { IPuertoInstrumentos, ResultadoCreacion } from '@/application/puertos';
import type { ErrorCoelsa } from '@/infrastructure/clienteHttp';
import { apiInstrumentos } from '@/infrastructure/apiCoelsa';
import { prefijoTipo } from '@/application/clavesConsulta';

export interface VariablesCreacionCheque {
  request: CrearChequeFisicoRequest;
  idempotencyKey: string;
}

export interface VariablesCreacionEcheq {
  request: CrearEcheqRequest;
  idempotencyKey: string;
}

export function useCrearInstrumento<T extends TipoInstrumentoForm>(
  tipo: T,
  puerto: IPuertoInstrumentos = apiInstrumentos,
): T extends 'ChequeFisico'
  ? UseMutationResult<ResultadoCreacion<ChequeResponse>, ErrorCoelsa, VariablesCreacionCheque>
  : UseMutationResult<ResultadoCreacion<EcheqResponse>, ErrorCoelsa, VariablesCreacionEcheq> {
  const cliente = useQueryClient();

  const mutacion = useMutation<
    ResultadoCreacion<ChequeResponse> | ResultadoCreacion<EcheqResponse>,
    ErrorCoelsa,
    VariablesCreacionCheque | VariablesCreacionEcheq
  >({
    mutationFn: (variables) =>
      tipo === 'ChequeFisico'
        ? puerto.crearCheque(
            variables.request as CrearChequeFisicoRequest,
            variables.idempotencyKey,
          )
        : puerto.crearEcheq(variables.request as CrearEcheqRequest, variables.idempotencyKey),
    onSuccess: () => {
      cliente.invalidateQueries({ queryKey: prefijoTipo(tipo) });
    },
  });

  // La correspondencia tipo → respuesta la garantiza el chequeo `tipo === ...` de arriba.
  return mutacion as T extends 'ChequeFisico'
    ? UseMutationResult<ResultadoCreacion<ChequeResponse>, ErrorCoelsa, VariablesCreacionCheque>
    : UseMutationResult<ResultadoCreacion<EcheqResponse>, ErrorCoelsa, VariablesCreacionEcheq>;
}
