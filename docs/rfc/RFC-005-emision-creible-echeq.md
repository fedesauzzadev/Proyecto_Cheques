# RFC-005: Emisión creíble del echeq (paridad con operatoria real)

**Estado:** accepted — Fases B y D completadas (2026-09-07)
**Fecha:** 2026-09-07

## Contexto

La operatoria real (guía BEE Banco Rioja, BCRA, FAQ Banco Ciudad) emite el
echeq desde home banking con: cuenta de débito/CBU, beneficiario validado
(CUIT/CUIL/CDI + razón social), carácter, concepto/referencia, email de aviso
y flujo Generar → Firmar → Enviar con e-chequera previa. El sistema devuelve
CMC7 + ID. Nuestro simulador hoy pide solo CUITs/monto/moneda/fechas, genera
CMC7 + IDECHEQ random, crea en 1 POST y lista solo por CUIT.

## Propuesta (por fases, cada una deployable)

### Fase B — Emisión creíble (prioridad alta)

| ID | Back | Front | Aceptación |
|---|---|---|---|
| B1. Cuenta/CBU + chequera | Entidades `Cuenta` (CBU, banco, sucursal, nro. cuenta, CUIT titular) y `Chequera` (cuenta, nro., cantidad 50, estado, fecha habilitación +24h). `POST /cuentas`, `POST /cuentas/{cbu}/chequeras`. `CrearEcheqRequest` exige `cbuEmisor` de cuenta con chequera vigente; CMC7 derivado de banco/sucursal/cuenta + nro. secuencial de chequera (adiós random puro). | Selector de cuenta/CBU, pantalla de chequeras (pedir, ver habilitación). | Crear echeq sin CBU → 400; sin chequera vigente → 422; CMC7 refleja banco/cuenta reales. |
| B2. Carácter + modo | Enum `Caracter {AlaOrden, NoAlaOrden}` obligatorio; `Modo=Cruzado` fijo. Endoso solo si `AlaOrden` (→ 422 si no). | Campo carácter en el form + bloqueo de "endosar" si no a la orden. | Echeq no a la orden rechaza endoso con 422. |
| B3. Beneficiario real | `tipoDoc {CUIT,CUIL,CDI}` + `nombreLibrador/nombreBeneficiario` + flag validado. Validador acepta CDI (no solo módulo 11 CUIT). | Tipo doc + razón social con "lupa" (endpoint `GET /titulares?doc=` simulado AFIP/bancarizado). | CDI válido aceptado; nombre expuesto en respuesta. |
| B4. Campos de gestión | `concepto, motivo, referencia, emailNotificacion` opcionales (longitudes acotadas). | Campos en el form + aviso "se notificará a …". | Round-trip en respuesta; email inválido → 400. |
| B5. Reglas de fecha reales | Pago diferido máx 360 días desde emisión; presentación 30 días; diferir depósito en custodia máx 30 días. | Sin cambio (muestra errores del back). | Emisión +361 días → 400. |
| B6. Consulta real | `GET /echeqs` suma filtros `cbu, estado, desdeEmision, hastaEmision, desdePago, hastaPago, numeroCheque`. | Filtros CBU (obligatorio) + estado + rangos (tope 360 días). | Consulta sin CBU → 400 como en BEE. |
| B7. Comprobante | `EcheqResponse` suma `cbuEmisor, bancoEmisor, caracter, concepto, referencia, nombres`. | Vista comprobante (exportar/imprimir) con CMC7 + IDECHEQ. | — |

Migración: `CbuEmisor` nullable + backfill con CBU sintético por banco del CMC7
existente; luego NOT NULL. `Caracter` default `AlaOrden` (comportamiento actual).
Breaking change ya asumido: `POST /echeqs` sin `cmc7` (hecho) + ahora exige `cbuEmisor`.

### Fase C — Workflow de firmas (prioridad media)

- C1. Estados `Borrador → PorFirmar → Pendiente` + `POST /echeqs/{id}/firma`,
  `POST /echeqs/{id}/envio`; firmantes por cuenta + token simulado; wizard de 3
  pasos en el front. Emisión masiva (lotes hasta 300) con un solo
  `Idempotency-Key` por lote.
- C2. Esquema de firmas (N de M) mínimo: cuenta con firmantes, firma conjunta.

### Fase D — Post-emisión (prioridad baja / futura)

- D1. **Cesión** para no a la orden (tope 10 + domicilio, `POST …/cesiones`).
- D2. **Avales** (`solicitar/anular aval` + avalista).
- D3. **Negociación bursátil / IMF** (registro para negociación, custodia-registro).
- D4. **CUD + certificado acciones civiles** (hash + código de visualización
  estilo `echeq.com.ar/CAC`).
- D3. **Motivo de repudio** obligatorio al repudiar.
- Revisar tope de 100 endosos: BCRA hoy dice "sin límite".

## Alternativas consideradas

- **Todo junto en una fase:** descartado; B es valor autónomo y C/D tocan la
  máquina de estados.
- **Cuenta como string libre sin entidad:** descartado; sin entidad no hay
  titularidad ni chequera ni CMC7 derivado.
- **Mock externo de AFIP:** descartado; endpoint simulado propio es suficiente.

## Consecuencias

Gana: paridad con la necesidad real, CMC7 con significado (banco/cuenta), base
para descuento (Cartera necesita CBU + carácter). Cuesta: 2 entidades nuevas,
migración con backfill, front con 2 pantallas nuevas + wizard; `POST /echeqs`
rompe de nuevo (nuevo campo obligatorio).
