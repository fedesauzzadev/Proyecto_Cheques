import { Card, CardContent, CardHeader, CardTitle } from '@/presentation/ui/card';
import type { Instrumento } from '@/domain/tipos';
import EstadoBadge from '../consulta/EstadoBadge';
import {
  describirDiferimiento,
  describirMotivo,
  formatearFecha,
  formatearMonto,
} from '../consulta/formato';

function Fila({ etiqueta, children }: { etiqueta: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-1 py-2 sm:flex-row sm:items-baseline sm:gap-4">
      <dt className="w-40 shrink-0 text-sm text-muted-foreground">{etiqueta}</dt>
      <dd className="text-sm font-medium">{children}</dd>
    </div>
  );
}

export default function TarjetaDetalle({ item }: { item: Instrumento }) {
  const esCheque = item.tipo === 'ChequeFisico';

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex flex-wrap items-center gap-3">
          {esCheque ? 'Cheque físico' : 'Echeq'}
          <EstadoBadge estado={item.estado} />
        </CardTitle>
      </CardHeader>
      <CardContent>
        <dl className="divide-y">
          <Fila etiqueta="Identificador">
            <span className="font-mono text-xs">{item.identificador}</span>
          </Fila>
          <Fila etiqueta="Monto">{formatearMonto(item.monto, item.moneda)}</Fila>
          <Fila etiqueta="CUIT librador">
            <span className="font-mono">{item.cuitLibrador}</span>
            {item.tipo === 'Echeq' && (
              <span className="text-muted-foreground"> · {item.nombreLibrador}</span>
            )}
          </Fila>
          <Fila etiqueta="CUIT beneficiario">
            <span className="font-mono">{item.cuitBeneficiario}</span>
            {item.tipo === 'Echeq' && (
              <span className="text-muted-foreground">
                {' '}
                · {item.tipoDocBeneficiario} · {item.nombreBeneficiario}
              </span>
            )}
          </Fila>
          <Fila etiqueta="Fecha de emisión">{formatearFecha(item.fechaEmision)}</Fila>
          <Fila etiqueta="Diferimiento">{describirDiferimiento(item.fechaDiferimiento)}</Fila>
          <Fila etiqueta="Vencimiento">{formatearFecha(item.fechaVencimiento)}</Fila>
          <Fila etiqueta="Motivo de rechazo">
            {describirMotivo(item.motivoRechazo)}
            {item.tipo === 'Echeq' && item.motivoRepudio && (
              <span className="text-muted-foreground"> (repudio: “{item.motivoRepudio}”)</span>
            )}
          </Fila>
          {esCheque ? (
            <>
              <Fila etiqueta="Banco">{item.desgloseCmc7.banco}</Fila>
              <Fila etiqueta="Sucursal">{item.desgloseCmc7.sucursal}</Fila>
              <Fila etiqueta="Código postal">{item.desgloseCmc7.codigoPostal}</Fila>
              <Fila etiqueta="Número de cheque">{item.desgloseCmc7.numeroCheque}</Fila>
              <Fila etiqueta="Número de cuenta">{item.desgloseCmc7.numeroCuenta}</Fila>
            </>
          ) : (
            <>
              <Fila etiqueta="CBU emisor">
                <span className="font-mono text-xs">{item.cbuEmisor}</span>
              </Fila>
              <Fila etiqueta="Chequera / número">
                <span className="font-mono">
                  N° {item.numeroChequera} · cheque {item.numeroCheque}
                </span>
              </Fila>
              <Fila etiqueta="Carácter">
                {item.caracter === 'AlaOrden' ? 'A la orden (endosable)' : 'No a la orden (solo cesión)'}
              </Fila>
              <Fila etiqueta="Modo">{item.modo}</Fila>
              <Fila etiqueta="Concepto">{item.concepto ?? '—'}</Fila>
              <Fila etiqueta="Motivo">{item.motivo ?? '—'}</Fila>
              <Fila etiqueta="Referencia">{item.referencia ?? '—'}</Fila>
              <Fila etiqueta="Email de aviso">{item.emailNotificacion ?? '—'}</Fila>
              <Fila etiqueta="CMC7">
                <span className="font-mono text-xs">{item.cmc7}</span>
              </Fila>
              <Fila etiqueta="Banco">{item.desgloseCmc7.banco}</Fila>
              <Fila etiqueta="Sucursal">{item.desgloseCmc7.sucursal}</Fila>
              <Fila etiqueta="Código postal">{item.desgloseCmc7.codigoPostal}</Fila>
              <Fila etiqueta="Número de cheque">{item.desgloseCmc7.numeroCheque}</Fila>
              <Fila etiqueta="Número de cuenta">{item.desgloseCmc7.numeroCuenta}</Fila>
              <Fila etiqueta="Endosos">{item.cantidadEndosos}</Fila>
            </>
          )}
        </dl>
      </CardContent>
    </Card>
  );
}
