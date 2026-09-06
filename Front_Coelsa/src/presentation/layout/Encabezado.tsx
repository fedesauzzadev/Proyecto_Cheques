import { Link, NavLink } from 'react-router-dom';
import { Landmark } from 'lucide-react';
import { cn } from '@/lib/utils';
import InsigniaSalud from './InsigniaSalud';

const ENLACES = [
  { destino: '/', etiqueta: 'Consultar' },
  { destino: '/nuevo/cheque', etiqueta: 'Nuevo cheque' },
  { destino: '/nuevo/echeq', etiqueta: 'Nuevo echeq' },
];

export default function Encabezado() {
  return (
    <header className="sticky top-0 z-10 border-b bg-background/95 backdrop-blur">
      <div className="mx-auto flex w-full max-w-6xl items-center gap-6 px-4 py-3">
        <Link to="/" className="flex items-center gap-2">
          <Landmark className="size-6 text-primary" aria-hidden />
          <h1 className="text-lg font-bold tracking-tight">COELSA — Consola de Instrumentos</h1>
        </Link>
        <nav aria-label="Principal" className="flex items-center gap-1">
          {ENLACES.map((enlace) => (
            <NavLink
              key={enlace.destino}
              to={enlace.destino}
              end={enlace.destino === '/'}
              className={({ isActive }) =>
                cn(
                  'rounded-md px-3 py-2 text-sm font-medium text-muted-foreground hover:text-foreground',
                  isActive && 'bg-muted text-foreground',
                )
              }
            >
              {enlace.etiqueta}
            </NavLink>
          ))}
        </nav>
        <div className="ml-auto">
          <InsigniaSalud />
        </div>
      </div>
    </header>
  );
}
