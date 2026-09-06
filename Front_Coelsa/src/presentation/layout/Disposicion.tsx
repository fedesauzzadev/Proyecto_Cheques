import { Outlet } from 'react-router-dom';
import { Toaster } from '@/presentation/ui/sonner';
import Encabezado from './Encabezado';

export default function Disposicion() {
  return (
    <div className="flex min-h-screen flex-col bg-background text-foreground">
      <Encabezado />
      <main className="mx-auto w-full max-w-6xl flex-1 px-4 py-6">
        <Outlet />
      </main>
      <footer className="border-t py-4 text-center text-xs text-muted-foreground">
        Simulador COELSA · Consola de instrumentos · uso didáctico
      </footer>
      <Toaster position="top-right" richColors />
    </div>
  );
}
