// Hook del certificado para acciones civiles (Fase D3): lectura directa, sin
// cache de escrituras (el CUD es determinista).
import { useQuery } from '@tanstack/react-query';
import type { UseQueryResult } from '@tanstack/react-query';
import type { Certificado } from '@/domain/tipos';
import type { IPuertoCertificado } from '@/application/puertos';
import type { ErrorCoelsa } from '@/infrastructure/clienteHttp';
import { apiCertificado } from '@/infrastructure/apiCoelsa';
import { claveCertificado } from '@/application/clavesConsulta';

export function useCertificado(
  idecheq: string,
  habilitado: boolean,
  puerto: IPuertoCertificado = apiCertificado,
): UseQueryResult<Certificado, ErrorCoelsa> {
  return useQuery({
    queryKey: claveCertificado(idecheq),
    queryFn: ({ signal }) => puerto.obtener(idecheq, signal),
    enabled: habilitado && idecheq.trim().length > 0,
    // La vista solo aplica a echeqs ya rechazados: ni retry ni refetch periódico.
    retry: false,
    staleTime: Infinity,
  });
}
