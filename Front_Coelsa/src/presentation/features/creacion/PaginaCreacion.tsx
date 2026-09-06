// Página de creación (RF-F03, espejo de RF-01): delega todo en la estrategia
// del tipo (campos, validaciones y request builder).
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/presentation/ui/card';
import type { TipoInstrumentoForm } from '@/domain/estrategias/estrategiaCreacion';
import { estrategiaChequeFisico, estrategiaEcheq } from '@/domain/estrategias';
import type { IPuertoInstrumentos } from '@/application/puertos';
import FormularioChequeFisico from './FormularioChequeFisico';
import FormularioEcheq from './FormularioEcheq';

interface Props {
  tipo: TipoInstrumentoForm;
  puerto?: IPuertoInstrumentos;
}

export default function PaginaCreacion({ tipo, puerto }: Props) {
  const esCheque = tipo === 'ChequeFisico';
  const estrategia = esCheque ? estrategiaChequeFisico : estrategiaEcheq;

  return (
    <Card className="mx-auto max-w-2xl">
      <CardHeader>
        <CardTitle>{estrategia.titulo}</CardTitle>
        <CardDescription>{estrategia.descripcion}</CardDescription>
        <p className="text-xs text-muted-foreground">
          Identificador: <span className="font-medium">{estrategia.identificadorEtiqueta}</span>
        </p>
      </CardHeader>
      <CardContent>
        {esCheque ? (
          <FormularioChequeFisico puerto={puerto} />
        ) : (
          <FormularioEcheq puerto={puerto} />
        )}
      </CardContent>
    </Card>
  );
}
