import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { Button } from '@/presentation/ui/button';
import { estrategiaChequeFisico } from '@/domain/estrategias';
import type { ErroresCampo } from '@/domain/validacionesInstrumento';
import type { IPuertoInstrumentos } from '@/application/puertos';
import { useCrearInstrumento } from '@/application/hooks/useCrearInstrumento';
import CamposEstrategia from './CamposEstrategia';
import { valoresIniciales } from './valoresIniciales';
import EstadoEnvio from './EstadoEnvio';

export default function FormularioChequeFisico({ puerto }: { puerto?: IPuertoInstrumentos }) {
  const navigate = useNavigate();
  const crear = useCrearInstrumento('ChequeFisico', puerto);

  const [valores, setValores] = useState<Record<string, string>>(() =>
    valoresIniciales(estrategiaChequeFisico.campos.map((campo) => campo.nombre)),
  );
  const [errores, setErrores] = useState<ErroresCampo>({});
  const [errorServidor, setErrorServidor] = useState<string | null>(null);
  const [conflicto, setConflicto] = useState<string | null>(null);
  const [replayHref, setReplayHref] = useState<string | null>(null);
  const [intentoId, setIntentoId] = useState(() => crypto.randomUUID());

  function actualizar(nombre: string, valor: string) {
    setValores((anteriores) => ({ ...anteriores, [nombre]: valor }));
    setErrores((anteriores) => {
      if (!anteriores[nombre]) return anteriores;
      const resto = { ...anteriores };
      delete resto[nombre];
      return resto;
    });
  }

  function alEnviar(evento: FormEvent) {
    evento.preventDefault();
    setErrorServidor(null);
    setConflicto(null);
    setReplayHref(null);

    const request = estrategiaChequeFisico.construir(valores);
    const fallas = estrategiaChequeFisico.validar(request);
    if (Object.keys(fallas).length > 0) {
      setErrores(fallas);
      return;
    }
    setErrores({});

    crear.mutate(
      { request, idempotencyKey: intentoId },
      {
        onSuccess: (resultado) => {
          if (resultado.esReplay) {
            setReplayHref(`/cheques/${resultado.respuesta.identificador}`);
            toast.info('El instrumento ya existía para este intento.');
          } else {
            toast.success('Instrumento creado.');
            navigate(`/cheques/${resultado.respuesta.identificador}`);
          }
        },
        onError: (error) => {
          if (error.estadoHttp === 409) setConflicto(error.message);
          else setErrorServidor(error.message);
        },
      },
    );
  }

  return (
    <form onSubmit={alEnviar} className="flex flex-col gap-4" noValidate>
      <CamposEstrategia
        estrategia={estrategiaChequeFisico}
        valores={valores}
        errores={errores}
        onCambiar={actualizar}
      />
      <EstadoEnvio
        intentoId={intentoId}
        errorServidor={errorServidor}
        conflicto={conflicto}
        replayHref={replayHref}
        onNuevaClave={() => {
          setIntentoId(crypto.randomUUID());
          setConflicto(null);
        }}
      />
      <div>
        <Button type="submit" disabled={crear.isPending}>
          {crear.isPending ? 'Creando…' : 'Crear'}
        </Button>
      </div>
    </form>
  );
}
