// Hook del badge de salud del backend (RF-F08, espejo de RF-07).
// Refresca cada minuto; reintenta transitorios con backoff (cold starts del plan
// gratis, microcortes) y deja el reintento manual al botón de la insignia.
import { useQuery } from '@tanstack/react-query';
import type { UseQueryResult } from '@tanstack/react-query';
import type { EstadoSalud, IPuertoSalud } from '@/application/puertos';
import type { ErrorCoelsa } from '@/infrastructure/clienteHttp';
import { apiSalud } from '@/infrastructure/apiCoelsa';
import { CLAVE_SALUD } from '@/application/clavesConsulta';

const REFRESCO_SALUD_MS = 60_000;
const REINTENTOS_SALUD = 2;

export function useSaludBackend(
  puerto: IPuertoSalud = apiSalud,
): UseQueryResult<EstadoSalud, ErrorCoelsa> {
  return useQuery({
    queryKey: CLAVE_SALUD,
    queryFn: ({ signal }) => puerto.consultar(signal),
    refetchInterval: REFRESCO_SALUD_MS,
    retry: REINTENTOS_SALUD,
  });
}
