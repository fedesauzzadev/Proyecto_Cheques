// Mapa de rutas (RNF-F04, modo declarativo) con code splitting por ruta (RNF-F12).
// Cada página lazy tiene su propio Suspense: el layout (header + badge de salud)
// se muestra al instante y solo el contenido espera la carga.
// Las páginas de detalle y creación se completan en el paso 6; el esqueleto ya
// deja el mapa estable para no tocar este archivo después.
import { lazy, Suspense } from 'react';
import type { ReactNode } from 'react';
import { Route, Routes } from 'react-router-dom';
import Disposicion from './layout/Disposicion';
import CargandoPagina from './layout/CargandoPagina';
import PaginaNoEncontrada from './layout/PaginaNoEncontrada';

const PaginaConsulta = lazy(() => import('./features/consulta/PaginaConsulta'));
const PaginaDetalle = lazy(() => import('./features/detalle/PaginaDetalle'));
const PaginaCreacion = lazy(() => import('./features/creacion/PaginaCreacion'));
const PaginaContratos = lazy(() => import('./features/contratos/PaginaContratos'));

function conCargaDiferida(pagina: ReactNode) {
  return <Suspense fallback={<CargandoPagina />}>{pagina}</Suspense>;
}

export default function Rutas() {
  return (
    <Routes>
      <Route element={<Disposicion />}>
        <Route index element={conCargaDiferida(<PaginaConsulta />)} />
        <Route
          path="cheques/:identificador"
          element={conCargaDiferida(<PaginaDetalle tipo="ChequeFisico" />)}
        />
        <Route
          path="echeqs/:identificador"
          element={conCargaDiferida(<PaginaDetalle tipo="Echeq" />)}
        />
        <Route
          path="nuevo/cheque"
          element={conCargaDiferida(<PaginaCreacion tipo="ChequeFisico" />)}
        />
        <Route path="nuevo/echeq" element={conCargaDiferida(<PaginaCreacion tipo="Echeq" />)} />
        <Route path="contratos" element={conCargaDiferida(<PaginaContratos />)} />
        <Route path="*" element={<PaginaNoEncontrada />} />
      </Route>
    </Routes>
  );
}
