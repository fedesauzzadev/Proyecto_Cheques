import { Link } from 'react-router-dom';
import { Hammer } from 'lucide-react';
import { Button } from '@/presentation/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/presentation/ui/card';

// Marcador temporal del paso 5: las páginas de detalle y creación se implementan
// en el paso 6. Las rutas ya existen para no tocar `rutas.tsx` después.
export default function PaginaEnConstruccion({ titulo }: { titulo: string }) {
  return (
    <Card className="mx-auto mt-12 max-w-md text-center">
      <CardHeader>
        <CardTitle className="flex items-center justify-center gap-2">
          <Hammer className="size-5" aria-hidden />
          {titulo}
        </CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col items-center gap-4">
        <p className="text-sm text-muted-foreground">Esta vista se implementa en el paso 6.</p>
        <Button asChild variant="outline">
          <Link to="/">Volver a la consulta</Link>
        </Button>
      </CardContent>
    </Card>
  );
}
