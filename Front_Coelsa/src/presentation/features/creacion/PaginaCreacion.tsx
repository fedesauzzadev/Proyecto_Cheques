import type { TipoInstrumentoForm } from '@/domain/estrategias/estrategiaCreacion';
import PaginaEnConstruccion from '../comun/PaginaEnConstruccion';

// Stub del paso 5: la implementación real llega en el paso 6.
export default function PaginaCreacion({ tipo }: { tipo: TipoInstrumentoForm }) {
  const titulo = tipo === 'ChequeFisico' ? 'Nuevo cheque físico' : 'Nuevo echeq';
  return <PaginaEnConstruccion titulo={titulo} />;
}
