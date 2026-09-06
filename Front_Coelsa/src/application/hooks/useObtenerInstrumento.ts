// Hook de detalle individual por identificador de negocio (RF-F02, espejo de RF-04).
import { useQuery } from '@tanstack/react-query';
import type { UseQueryResult } from '@tanstack/react-query';
import type { TipoInstrumentoForm } from '@/domain/estrategias/estrategiaCreacion';
import type { ChequeResponse, EcheqResponse } from '@/domain/tipos';
import type { IPuertoInstrumentos } from '@/application/puertos';
import type { ErrorCoelsa } from '@/infrastructure/clienteHttp';
import { apiInstrumentos } from '@/infrastructure/apiCoelsa';
import { claveDetalle } from '@/application/clavesConsulta';

export function useObtenerInstrumento(
  tipo: 'ChequeFisico',
  identificador: string,
  puerto?: IPuertoInstrumentos,
): UseQueryResult<ChequeResponse, ErrorCoelsa>;
export function useObtenerInstrumento(
  tipo: 'Echeq',
  identificador: string,
  puerto?: IPuertoInstrumentos,
): UseQueryResult<EcheqResponse, ErrorCoelsa>;
export function useObtenerInstrumento(
  tipo: TipoInstrumentoForm,
  identificador: string,
  puerto?: IPuertoInstrumentos,
): UseQueryResult<ChequeResponse | EcheqResponse, ErrorCoelsa>;
export function useObtenerInstrumento(
  tipo: TipoInstrumentoForm,
  identificador: string,
  puerto: IPuertoInstrumentos = apiInstrumentos,
) {
  return useQuery<ChequeResponse | EcheqResponse, ErrorCoelsa>({
    queryKey: claveDetalle(tipo, identificador),
    queryFn: ({ signal }) =>
      tipo === 'ChequeFisico'
        ? puerto.obtenerCheque(identificador, signal)
        : puerto.obtenerEcheq(identificador, signal),
    enabled: identificador.trim().length > 0,
  });
}
