import { Link } from 'react-router-dom';
import { Button } from '@/presentation/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/presentation/ui/card';

export default function PaginaNoEncontrada() {
  return (
    <Card className="mx-auto mt-12 max-w-md text-center">
      <CardHeader>
        <CardTitle>Página no encontrada</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col items-center gap-4">
        <p className="text-sm text-muted-foreground">La ruta solicitada no existe en la consola.</p>
        <Button asChild>
          <Link to="/">Volver a la consulta</Link>
        </Button>
      </CardContent>
    </Card>
  );
}
