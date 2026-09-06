// Vista principal de consulta por CUIT (RF-F01, espejo de RF-03).
// Pestañas Cheques/Echeqs (nunca mezclados), validación módulo 11 client-side,
// tabla paginada y estados de carga / vacío / error.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { Banknote, Search, Smartphone } from 'lucide-react';
import { Button } from '@/presentation/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/presentation/ui/card';
import { Input } from '@/presentation/ui/input';
import { Label } from '@/presentation/ui/label';
import { Tabs, TabsList, TabsTrigger } from '@/presentation/ui/tabs';
import { esCuitValido } from '@/domain/validadorCuit';
import type { TipoInstrumentoForm } from '@/domain/estrategias/estrategiaCreacion';
import type { IPuertoInstrumentos } from '@/application/puertos';
import { useListarInstrumentos } from '@/application/hooks/useListarInstrumentos';
import TablaInstrumentos, { EsqueletoTabla } from './TablaInstrumentos';
import PaginacionConsulta from './PaginacionConsulta';

// CUITs con datos sembrados en la API (útiles para probar sin tipear).
const CUITS_DE_PRUEBA = ['20123456786', '27876543219', '30511222334', '20334455662'];

const TITULOS: Record<TipoInstrumentoForm, { pestaña: string; crear: string; rutaNuevo: string }> =
  {
    ChequeFisico: {
      pestaña: 'Cheques físicos',
      crear: 'Crear cheque físico',
      rutaNuevo: '/nuevo/cheque',
    },
    Echeq: { pestaña: 'Echeqs', crear: 'Crear echeq', rutaNuevo: '/nuevo/echeq' },
  };

interface Props {
  puerto?: IPuertoInstrumentos;
}

export default function PaginaConsulta({ puerto }: Props = {}) {
  const [tipo, setTipo] = useState<TipoInstrumentoForm>('ChequeFisico');
  const [cuitTexto, setCuitTexto] = useState('');
  const [errorCuit, setErrorCuit] = useState<string | null>(null);
  const [cuitConsultado, setCuitConsultado] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const consulta = useListarInstrumentos(tipo, { cuit: cuitConsultado, page, pageSize }, puerto);

  function buscarCon(cuit: string) {
    const normalizado = cuit.trim();
    if (!esCuitValido(normalizado)) {
      setErrorCuit('Ingresá un CUIT/CUIL válido de 11 dígitos (con verificador módulo 11).');
      return;
    }
    setErrorCuit(null);
    setCuitConsultado(normalizado);
    setPage(1);
  }

  function alEnviar(evento: FormEvent) {
    evento.preventDefault();
    buscarCon(cuitTexto);
  }

  function alCambiarTipo(nuevo: string) {
    setTipo(nuevo as TipoInstrumentoForm);
    setPage(1);
  }

  const cargandoPrimeraVez = consulta.isPending && cuitConsultado !== '';
  const actualizando = consulta.isFetching && !consulta.isPending;
  const sinResultados = consulta.isSuccess && consulta.data.items.length === 0;

  return (
    <div className="flex flex-col gap-6">
      <Card>
        <CardHeader>
          <CardTitle>Consultar instrumentos por CUIT/CUIL</CardTitle>
          <CardDescription>
            Busca cheques físicos o echeqs donde el CUIT sea librador o beneficiario.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <Tabs value={tipo} onValueChange={alCambiarTipo} aria-label="Tipo de instrumento">
            <TabsList>
              <TabsTrigger
                value="ChequeFisico"
                className="gap-2 data-[state=active]:bg-primary data-[state=active]:font-semibold data-[state=active]:text-primary-foreground data-[state=active]:shadow-sm"
              >
                <Banknote className="size-4" aria-hidden />
                {TITULOS.ChequeFisico.pestaña}
              </TabsTrigger>
              <TabsTrigger
                value="Echeq"
                className="gap-2 data-[state=active]:bg-primary data-[state=active]:font-semibold data-[state=active]:text-primary-foreground data-[state=active]:shadow-sm"
              >
                <Smartphone className="size-4" aria-hidden />
                {TITULOS.Echeq.pestaña}
              </TabsTrigger>
            </TabsList>
          </Tabs>

          <form onSubmit={alEnviar} className="flex flex-col gap-2" noValidate>
            <Label htmlFor="cuit">CUIT/CUIL</Label>
            <div className="flex flex-wrap gap-2">
              <Input
                id="cuit"
                inputMode="numeric"
                placeholder="20123456786"
                value={cuitTexto}
                onChange={(evento) => setCuitTexto(evento.target.value)}
                aria-invalid={errorCuit !== null}
                aria-describedby={errorCuit ? 'error-cuit' : undefined}
                className="max-w-xs font-mono"
              />
              <Button type="submit">
                <Search className="size-4" aria-hidden />
                Buscar
              </Button>
            </div>
            {errorCuit && (
              <p id="error-cuit" role="alert" className="text-sm text-destructive">
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
        </CardContent>
      </Card>

      {cuitConsultado === '' && (
        <Card>
          <CardContent className="py-8 text-center text-sm text-muted-foreground">
            Ingresá un CUIT/CUIL y presioná Buscar para ver sus instrumentos.
          </CardContent>
        </Card>
      )}

      {cargandoPrimeraVez && (
        <Card>
          <CardContent className="pt-6">
            <EsqueletoTabla />
          </CardContent>
        </Card>
      )}

      {consulta.isError && (
        <Card role="alert" className="border-destructive">
          <CardHeader>
            <CardTitle className="text-destructive">No se pudo consultar</CardTitle>
            <CardDescription>{consulta.error.message}</CardDescription>
          </CardHeader>
          <CardContent>
            <Button variant="outline" onClick={() => consulta.refetch()}>
              Reintentar
            </Button>
          </CardContent>
        </Card>
      )}

      {sinResultados && (
        <Card>
          <CardContent className="flex flex-col items-center gap-4 py-8 text-center">
            <p className="text-sm text-muted-foreground">
              No hay {tipo === 'ChequeFisico' ? 'cheques físicos' : 'echeqs'} para el CUIT{' '}
              <span className="font-mono">{cuitConsultado}</span>.
            </p>
            <Button asChild>
              <Link to={TITULOS[tipo].rutaNuevo}>{TITULOS[tipo].crear}</Link>
            </Button>
          </CardContent>
        </Card>
      )}

      {consulta.isSuccess && consulta.data.items.length > 0 && (
        <Card>
          <CardContent className="pt-6">
            <TablaInstrumentos tipo={tipo} pagina={consulta.data} actualizando={actualizando} />
            <PaginacionConsulta
              page={consulta.data.page}
              pageSize={consulta.data.pageSize}
              totalPages={consulta.data.totalPages}
              totalCount={consulta.data.totalCount}
              onPagina={setPage}
              onTamano={(tamano) => {
                setPageSize(tamano);
                setPage(1);
              }}
            />
          </CardContent>
        </Card>
      )}
    </div>
  );
}
