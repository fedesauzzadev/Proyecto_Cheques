// Campos del formulario gobernados por la estrategia (solo lectura: no llama a
// validar ni construir, así evita el problema de firmas en unión).
import { Input } from '@/presentation/ui/input';
import { Label } from '@/presentation/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/presentation/ui/select';
import type { EstrategiaCreacion } from '@/domain/estrategias/estrategiaCreacion';
import type { ErroresCampo } from '@/domain/validacionesInstrumento';

interface Props<TRequest> {
  estrategia: EstrategiaCreacion<TRequest>;
  valores: Record<string, string>;
  errores: ErroresCampo;
  onCambiar: (nombre: string, valor: string) => void;
}

export default function CamposEstrategia<TRequest>({
  estrategia,
  valores,
  errores,
  onCambiar,
}: Props<TRequest>) {
  return (
    <>
      {estrategia.campos.map((campo) => (
        <div key={campo.nombre} className="flex flex-col gap-1.5">
          <Label htmlFor={`campo-${campo.nombre}`}>
            {campo.etiqueta}
            {campo.obligatorio ? ' *' : ''}
          </Label>
          {campo.tipo === 'moneda' ? (
            <Select
              value={valores[campo.nombre] ?? 'P'}
              onValueChange={(valor) => onCambiar(campo.nombre, valor)}
            >
              <SelectTrigger id={`campo-${campo.nombre}`} className="max-w-xs">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="P">Pesos ($)</SelectItem>
                <SelectItem value="D">Dólares (US$)</SelectItem>
              </SelectContent>
            </Select>
          ) : (
            <Input
              id={`campo-${campo.nombre}`}
              type={campo.tipo === 'numero' ? 'number' : campo.tipo === 'fecha' ? 'date' : 'text'}
              min={campo.tipo === 'numero' ? '0' : undefined}
              step={campo.tipo === 'numero' ? '0.01' : undefined}
              inputMode={
                campo.tipo === 'numero'
                  ? 'decimal'
                  : campo.nombre.includes('cuit') || campo.nombre === 'cmc7'
                    ? 'numeric'
                    : undefined
              }
              placeholder={campo.placeholder}
              value={valores[campo.nombre] ?? ''}
              onChange={(evento) => onCambiar(campo.nombre, evento.target.value)}
              aria-invalid={errores[campo.nombre] !== undefined}
              className="max-w-md font-mono"
            />
          )}
          {campo.ayuda && !errores[campo.nombre] && (
            <p className="text-xs text-muted-foreground">{campo.ayuda}</p>
          )}
          {errores[campo.nombre] && (
            <p role="alert" className="text-sm text-destructive">
              {errores[campo.nombre]}
            </p>
          )}
        </div>
      ))}
    </>
  );
}
