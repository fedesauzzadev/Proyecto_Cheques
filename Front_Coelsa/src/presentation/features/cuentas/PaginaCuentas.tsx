// Cuentas corrientes emisoras y e-chequeras (Fase B, espejo de RF-01b).
// Buscar por CUIT titular, crear cuenta y solicitar chequeras (50 números).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { toast } from 'sonner';
import { Landmark, PlusCircle, Search } from 'lucide-react';
import { Button } from '@/presentation/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/presentation/ui/card';
import { Input } from '@/presentation/ui/input';
import { Label } from '@/presentation/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/presentation/ui/select';
import { esCuitValido } from '@/domain/validadorCuit';
import { esCbuValido } from '@/domain/cbu';
import type { Cuenta, Moneda } from '@/domain/tipos';
import type { IPuertoCuentas } from '@/application/puertos';
import type { ErrorCoelsa } from '@/infrastructure/clienteHttp';
import {
  useChequeras,
  useCrearCuenta,
  useCuentas,
  useSolicitarChequera,
} from '@/application/hooks/useCuentas';

const CUITS_DE_PRUEBA = ['20123456786', '27876543219', '30511222334', '20334455662'];

interface Props {
  puerto?: IPuertoCuentas;
}

function FilaCuenta({ cuenta, puerto }: { cuenta: Cuenta; puerto?: IPuertoCuentas }) {
  const chequeras = useChequeras(cuenta.cbu, puerto);
  const solicitar = useSolicitarChequera(puerto);

  function alSolicitar() {
    solicitar.mutate(
      { cbu: cuenta.cbu, idempotencyKey: crypto.randomUUID() },
      {
        onSuccess: (resultado) => {
          toast.success(
            resultado.esReplay
              ? `Chequera N° ${resultado.respuesta.numero} (ya existía para este intento).`
              : `Chequera N° ${resultado.respuesta.numero} vigente con 50 números.`,
          );
        },
        onError: (error: ErrorCoelsa) => toast.error(error.message),
      },
    );
  }

  return (
    <div className="flex flex-col gap-2 rounded-md border p-3">
      <div className="flex flex-wrap items-center gap-x-4 gap-y-1">
        <span className="font-mono text-sm font-medium">{cuenta.cbu}</span>
        <span className="text-sm font-medium">{cuenta.nombreTitular}</span>
        <span className="text-xs text-muted-foreground">
          Banco {cuenta.banco} · Sucursal {cuenta.sucursal} · Cuenta {cuenta.numeroCuenta} ·{' '}
          {cuenta.moneda === 'P' ? 'Pesos' : 'Dólares'}
        </span>
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="ml-auto gap-1"
          disabled={solicitar.isPending}
          onClick={alSolicitar}
        >
          <PlusCircle className="size-4" aria-hidden />
          {solicitar.isPending ? 'Solicitando…' : 'Solicitar chequera'}
        </Button>
      </div>
      {chequeras.isPending && <p className="text-xs text-muted-foreground">Cargando chequeras…</p>}
      {chequeras.isError && (
        <p role="alert" className="text-xs text-destructive">
          {chequeras.error.message}
        </p>
      )}
      {chequeras.isSuccess && chequeras.data.length === 0 && (
        <p className="text-xs text-muted-foreground">
          Sin chequeras: solicitá una para poder emitir echeqs desde esta cuenta.
        </p>
      )}
      {chequeras.isSuccess && chequeras.data.length > 0 && (
        <ul className="flex flex-wrap gap-2">
          {chequeras.data.map((chequera) => (
            <li
              key={chequera.numero}
              className="rounded-md bg-muted px-2 py-1 font-mono text-xs"
              title={`Solicitada ${chequera.fechaSolicitud}`}
            >
              N° {chequera.numero} · {chequera.estado} · {chequera.disponibles} disp.
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export default function PaginaCuentas({ puerto }: Props = {}) {
  const [cuitTexto, setCuitTexto] = useState('');
  const [errorCuit, setErrorCuit] = useState<string | null>(null);
  const [cuitConsultado, setCuitConsultado] = useState('');
  const cuentas = useCuentas(cuitConsultado, puerto);

  const [cbu, setCbu] = useState('');
  const [cuitTitular, setCuitTitular] = useState('');
  const [nombreTitular, setNombreTitular] = useState('');
  const [moneda, setMoneda] = useState<Moneda>('P');
  const [errorCrear, setErrorCrear] = useState<string | null>(null);
  const crear = useCrearCuenta(puerto);

  function buscarCon(cuit: string) {
    const normalizado = cuit.trim();
    if (!esCuitValido(normalizado)) {
      setErrorCuit('Ingresá un CUIT/CUIL válido de 11 dígitos (con verificador módulo 11).');
      return;
    }
    setErrorCuit(null);
    setCuitConsultado(normalizado);
  }

  function alBuscar(evento: FormEvent) {
    evento.preventDefault();
    buscarCon(cuitTexto);
  }

  function alCrear(evento: FormEvent) {
    evento.preventDefault();
    setErrorCrear(null);

    const cbuNormalizado = cbu.trim();
    const titularNormalizado = cuitTitular.trim();
    const nombreNormalizado = nombreTitular.trim();
    if (!esCbuValido(cbuNormalizado)) {
      setErrorCrear('El CBU debe tener 22 dígitos con verificadores válidos.');
      return;
    }
    if (!esCuitValido(titularNormalizado)) {
      setErrorCrear('El CUIT/CUIL del titular debe ser válido (11 dígitos con verificador).');
      return;
    }
    if (nombreNormalizado === '' || nombreNormalizado.length > 120) {
      setErrorCrear('El nombre del titular es obligatorio (hasta 120 caracteres).');
      return;
    }

    crear.mutate(
      {
        request: {
          cbu: cbuNormalizado,
          cuitTitular: titularNormalizado,
          nombreTitular: nombreNormalizado,
          moneda,
        },
        idempotencyKey: crypto.randomUUID(),
      },
      {
        onSuccess: (resultado) => {
          toast.success(
            resultado.esReplay ? 'La cuenta ya existía para este intento.' : 'Cuenta creada.',
          );
          setCbu('');
          setCuitTitular('');
          setNombreTitular('');
          if (cuitConsultado === titularNormalizado) cuentas.refetch();
        },
        onError: (error: ErrorCoelsa) => setErrorCrear(error.message),
      },
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Landmark className="size-5" aria-hidden />
            Cuentas emisoras por CUIT titular
          </CardTitle>
          <CardDescription>
            Cada cuenta necesita una e-chequera vigente para emitir echeqs (el CMC7 se deriva
            del CBU + número de chequera).
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <form onSubmit={alBuscar} className="flex flex-col gap-2" noValidate>
            <Label htmlFor="cuit-cuentas">CUIT/CUIL titular</Label>
            <div className="flex flex-wrap gap-2">
              <Input
                id="cuit-cuentas"
                inputMode="numeric"
                placeholder="20123456786"
                value={cuitTexto}
                onChange={(evento) => setCuitTexto(evento.target.value)}
                aria-invalid={errorCuit !== null}
                className="max-w-xs font-mono"
              />
              <Button type="submit">
                <Search className="size-4" aria-hidden />
                Buscar
              </Button>
            </div>
            {errorCuit && (
              <p role="alert" className="text-sm text-destructive">
                {errorCuit}
              </p>
            )}
          </form>

          <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
            <span>CUITs de prueba:</span>
            {CUITS_DE_PRUEBA.map((cuit) => (
              <Button
                key={cuit}
                type="button"
                variant="outline"
                size="sm"
                className="font-mono"
                onClick={() => {
                  setCuitTexto(cuit);
                  buscarCon(cuit);
                }}
              >
                {cuit}
              </Button>
            ))}
          </div>

          {cuentas.isPending && cuitConsultado !== '' && (
            <p className="text-sm text-muted-foreground">Cargando cuentas…</p>
          )}
          {cuentas.isError && (
            <p role="alert" className="text-sm text-destructive">
              {cuentas.error.message}
            </p>
          )}
          {cuentas.isSuccess && cuentas.data.length === 0 && (
            <p className="text-sm text-muted-foreground">
              Sin cuentas para el CUIT <span className="font-mono">{cuitConsultado}</span>. Creá
              una abajo.
            </p>
          )}
          {cuentas.isSuccess && cuentas.data.length > 0 && (
            <div className="flex flex-col gap-3">
              {cuentas.data.map((cuenta) => (
                <FilaCuenta key={cuenta.cbu} cuenta={cuenta} puerto={puerto} />
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Crear cuenta emisora</CardTitle>
          <CardDescription>
            Después pedí su chequera y usá el CBU en <Link to="/nuevo/echeq" className="underline">Nuevo echeq</Link>.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={alCrear} className="flex flex-col gap-4" noValidate>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="nueva-cbu">CBU *</Label>
              <Input
                id="nueva-cbu"
                inputMode="numeric"
                placeholder="0110001300000000000017"
                value={cbu}
                onChange={(evento) => setCbu(evento.target.value)}
                className="max-w-md font-mono"
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="nuevo-titular">CUIT/CUIL titular *</Label>
              <Input
                id="nuevo-titular"
                inputMode="numeric"
                placeholder="20123456786"
                value={cuitTitular}
                onChange={(evento) => setCuitTitular(evento.target.value)}
                className="max-w-xs font-mono"
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="nuevo-nombre">Nombre del titular *</Label>
              <Input
                id="nuevo-nombre"
                placeholder="Alfa S.R.L."
                value={nombreTitular}
                onChange={(evento) => setNombreTitular(evento.target.value)}
                className="max-w-md"
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="nueva-moneda">Moneda *</Label>
              <Select value={moneda} onValueChange={(valor) => setMoneda(valor as Moneda)}>
                <SelectTrigger id="nueva-moneda" className="max-w-xs">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="P">Pesos ($)</SelectItem>
                  <SelectItem value="D">Dólares (US$)</SelectItem>
                </SelectContent>
              </Select>
            </div>
            {errorCrear && (
              <p role="alert" className="text-sm text-destructive">
                {errorCrear}
              </p>
            )}
            <div>
              <Button type="submit" disabled={crear.isPending}>
                {crear.isPending ? 'Creando…' : 'Crear cuenta'}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
