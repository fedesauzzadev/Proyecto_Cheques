import { useState } from 'react';
import { Link, NavLink } from 'react-router-dom';
import { Landmark, Menu, X } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/presentation/ui/button';
import InsigniaSalud from './InsigniaSalud';

const ENLACES = [
  { destino: '/', etiqueta: 'Consultar' },
  { destino: '/nuevo/cheque', etiqueta: 'Nuevo cheque' },
  { destino: '/nuevo/echeq', etiqueta: 'Nuevo echeq' },
  { destino: '/contratos', etiqueta: 'Contratos' },
];

function clasesEnlace(isActive: boolean, movil = false) {
  return cn(
    'rounded-md px-3 py-2 text-sm font-medium text-muted-foreground hover:text-foreground',
    isActive && 'bg-muted text-foreground',
    movil && 'block w-full text-base',
  );
}

export default function Encabezado() {
  const [menuAbierto, setMenuAbierto] = useState(false);

  return (
    <header className="sticky top-0 z-10 border-b bg-background/95 backdrop-blur">
      <div className="mx-auto flex w-full max-w-6xl items-center gap-2 px-4 py-3 sm:gap-4">
        <Link
          to="/"
          className="flex min-w-0 items-center gap-2"
          onClick={() => setMenuAbierto(false)}
        >
          <Landmark className="size-6 shrink-0 text-primary" aria-hidden />
          <h1 className="truncate text-base font-bold tracking-tight sm:text-lg">
            COELSA — Consola de Instrumentos
          </h1>
        </Link>
        <nav aria-label="Principal" className="hidden items-center gap-1 md:flex">
          {ENLACES.map((enlace) => (
            <NavLink
              key={enlace.destino}
              to={enlace.destino}
              end={enlace.destino === '/'}
              className={({ isActive }) => clasesEnlace(isActive)}
            >
              {enlace.etiqueta}
            </NavLink>
          ))}
        </nav>
        <div className="ml-auto flex shrink-0 items-center gap-1">
          <InsigniaSalud />
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="md:hidden"
            aria-expanded={menuAbierto}
            aria-label={menuAbierto ? 'Cerrar menú' : 'Abrir menú'}
            onClick={() => setMenuAbierto((abierto) => !abierto)}
          >
            {menuAbierto ? (
              <X className="size-5" aria-hidden />
            ) : (
              <Menu className="size-5" aria-hidden />
            )}
          </Button>
        </div>
      </div>
      {menuAbierto && (
        <nav aria-label="Principal móvil" className="border-t px-4 py-2 md:hidden">
          <ul className="flex flex-col gap-1">
            {ENLACES.map((enlace) => (
              <li key={enlace.destino}>
                <NavLink
                  to={enlace.destino}
                  end={enlace.destino === '/'}
                  onClick={() => setMenuAbierto(false)}
                  className={({ isActive }) => clasesEnlace(isActive, true)}
                >
                  {enlace.etiqueta}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
      )}
    </header>
  );
}
