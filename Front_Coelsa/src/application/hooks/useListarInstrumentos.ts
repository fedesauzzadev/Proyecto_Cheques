// Hook de consulta paginada por CUIT (RF-F01, espejo de RF-03 del backend).
// La query no se dispara con CUIT inválido: se informa en el formulario sin llamar a la API.
import { useQuery, keepPreviousData } from '@tanstack/react-query';
import type { UseQueryResult } from '@tanstack/react-query';
import { esCuitValido } from '@/domain/validadorCuit';
import type { TipoInstrumentoForm } from '@/domain/estrategias/estrategiaCreacion';
import type { ChequeResponse, EcheqResponse, PagedResponse } from '@/domain/tipos';
import type { ConsultaPaginada, IPuertoInstrumentos } from '@/application/puertos';
import type { ErrorCoelsa } from '@/infrastructure/clienteHttp';
import { apiInstrumentos } from '@/infrastructure/apiCoelsa';
import { claveListado } from '@/application/clavesConsulta';

export function useListarInstrumentos(
  tipo: 'ChequeFisico',
  filtros: ConsultaPaginada,
  puerto?: IPuertoInstrumentos,
): UseQueryResult<PagedResponse<ChequeResponse>, ErrorCoelsa>;
export function useListarInstrumentos(
  tipo: 'Echeq',
  filtros: ConsultaPaginada,
  puerto?: IPuertoInstrumentos,
): UseQueryResult<PagedResponse<EcheqResponse>, ErrorCoelsa>;
export function useListarInstrumentos(
  tipo: TipoInstrumentoForm,
  filtros: ConsultaPaginada,
  puerto?: IPuertoInstrumentos,
): UseQueryResult<PagedResponse<ChequeResponse> | PagedResponse<EcheqResponse>, ErrorCoelsa>;
export function useListarInstrumentos(
  tipo: TipoInstrumentoForm,
  filtros: ConsultaPaginada,
  puerto: IPuertoInstrumentos = apiInstrumentos,
) {
  return useQuery<PagedResponse<ChequeResponse> | PagedResponse<EcheqResponse>, ErrorCoelsa>({
    queryKey: claveListado(tipo, filtros.cuit, filtros.page, filtros.pageSize),
    queryFn: ({ signal }) =>
      tipo === 'ChequeFisico'
        ? puerto.listarCheques(filtros, signal)
        : puerto.listarEcheqs(filtros, signal),
    enabled: esCuitValido(filtros.cuit),
    placeholderData: keepPreviousData,
  });
}
