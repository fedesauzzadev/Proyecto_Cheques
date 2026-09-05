// Hook del badge de salud del backend (RF-F08, espejo de RF-07).
// Refresca cada minuto; sin reintentos agresivos (el próximo intervalo re-evalúa).
import { useQuery } from '@tanstack/react-query';
import type { UseQueryResult } from '@tanstack/react-query';
import type { EstadoSalud, IPuertoSalud } from '@/application/puertos';
import type { ErrorCoelsa } from '@/infrastructure/clienteHttp';
import { apiSalud } from '@/infrastructure/apiCoelsa';
import { CLAVE_SALUD } from '@/application/clavesConsulta';

const REFRESCO_SALUD_MS = 60_000;

export function useSaludBackend(
  puerto: IPuertoSalud = apiSalud,
): UseQueryResult<EstadoSalud, ErrorCoelsa> {
  return useQuery({
    queryKey: CLAVE_SALUD,
    queryFn: ({ signal }) => puerto.consultar(signal),
    refetchInterval: REFRESCO_SALUD_MS,
    retry: false,
  });
}
