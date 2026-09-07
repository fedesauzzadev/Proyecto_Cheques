import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { Search } from 'lucide-react';
import { Button } from '@/presentation/ui/button';
import { estrategiaEcheq } from '@/domain/estrategias';
import type { ErroresCampo } from '@/domain/validacionesInstrumento';
import type { IPuertoCuentas, IPuertoInstrumentos } from '@/application/puertos';
import { useCrearInstrumento } from '@/application/hooks/useCrearInstrumento';
import { useTitular } from '@/application/hooks/useCuentas';
import CamposEstrategia from './CamposEstrategia';
import { valoresIniciales } from './valoresIniciales';
import EstadoEnvio from './EstadoEnvio';

export default function FormularioEcheq({
  puerto,
  puertoCuentas,
}: {
  puerto?: IPuertoInstrumentos;
  puertoCuentas?: IPuertoCuentas;
}) {
  const navigate = useNavigate();
  const crear = useCrearInstrumento('Echeq', puerto);

  const [valores, setValores] = useState<Record<string, string>>(() =>
    valoresIniciales(estrategiaEcheq.campos.map((campo) => campo.nombre)),
  );
  const [errores, setErrores] = useState<ErroresCampo>({});
  const [errorServidor, setErrorServidor] = useState<string | null>(null);
  const [conflicto, setConflicto] = useState<string | null>(null);
  const [replayHref, setReplayHref] = useState<string | null>(null);
  const [intentoId, setIntentoId] = useState(() => crypto.randomUUID());

  // Lupa del beneficiario (Fase B3): valida el documento y, si está
  // bancarizado, permite autocompletar el nombre.
  const titular = useTitular(
    valores.tipoDocBeneficiario ?? '',
    valores.cuitBeneficiario ?? '',
    puertoCuentas,
  );

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

    const request = estrategiaEcheq.construir(valores);
    const fallas = estrategiaEcheq.validar(request);
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
            setReplayHref(`/echeqs/${resultado.respuesta.identificador}`);
            toast.info('El instrumento ya existía para este intento.');
          } else {
            toast.success('Instrumento creado.');
            navigate(`/echeqs/${resultado.respuesta.identificador}`);
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
        estrategia={estrategiaEcheq}
        valores={valores}
        errores={errores}
        onCambiar={actualizar}
      />
      <div aria-live="polite">
        {titular.fetchStatus === 'fetching' && (
          <p className="text-xs text-muted-foreground">Validando documento del beneficiario…</p>
        )}
        {titular.isSuccess && titular.data.bancarizado && titular.data.nombre && (
          <div className="flex flex-wrap items-center gap-2 text-sm">
            <span>
              ✓ <span className="font-medium">{titular.data.nombre}</span> (bancarizado)
            </span>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => actualizar('nombreBeneficiario', titular.data.nombre ?? '')}
            >
              <Search className="size-4" aria-hidden />
              Usar nombre
            </Button>
          </div>
        )}
        {titular.isSuccess && !titular.data.bancarizado && (
          <p className="text-xs text-muted-foreground">
            Documento válido pero no bancarizado: informá el nombre manualmente.
          </p>
        )}
        {titular.isError && (
          <p role="alert" className="text-sm text-destructive">
            {titular.error.message}
          </p>
        )}
      </div>
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
