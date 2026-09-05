import { Button } from '@/presentation/ui/button';

export default function App() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-6 bg-background text-foreground">
      <h1 className="text-3xl font-bold tracking-tight">COELSA — Consola de Instrumentos</h1>
      <p className="text-muted-foreground">Esqueleto del front listo (paso 1 del plan).</p>
      <Button variant="secondary">Base operativa</Button>
    </main>
  );
}
