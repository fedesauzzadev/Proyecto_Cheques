# PRD: Banco IrmaRios — banca de cartera para empresas

**Versión:** 1.0
**Estado:** Accepted
**Fecha:** 2026-09-06
**Owner:** Equipo producto
**Docs hijas:** SPEC IrmaRios (a crear) · SPEC Front IrmaRios (a crear) · RFC-004 descomposición (a crear)
**Depende de:** [Simulador COELSA](PRD.md) (fuente de verdad de instrumentos)

> El PRD describe **qué** y **por qué**; el cómo vive en RFCs, ADRs y SPECs.
> Si un detalle técnico aparece acá, está mal ubicado.

---

## 1. Problema

El ecosistema tiene un clearing que sabe todo de los instrumentos, pero no hay
nadie que les dé **servicio bancario** a sus tenedores. Las empresas que cobran
con cheques y echeqs necesitan: registrar su cartera, saber cuánto y cuándo
entran, adelantar ese cobro al banco (descuento), y ver su dinero en una cuenta.

IrmaRios es ese banco: **banco digital para empresas** cuya propuesta central
es la autogestión de la cartera de cheques/echeqs y su **negociación de
adelanto** (descuento) sin mostrador, con tasas y líneas propias por cliente.

## 2. Objetivos

### Objetivo de producto
Un banco que se integra al clearing (COELSA) para gestionar la cartera de sus
clientes empresas: depósitos a cobro, aceptaciones de echeqs, endosos con
conformidad del receptor, descuento de instrumentos según tasa/plazo/línea, y
cuenta con acreditación y extracto observables.

### Objetivos de aprendizaje
- Practicar un dominio con **dinero**: consistencia de saldos, idempotencia en
  operaciones financieras, separación entre decisión crediticia y mayor.
- Evolucionar el monorepo con un segundo producto completo (backend + front).

### No objetivos (v1)
- Autenticación real, firmas digitales, onboarding KYC.
- Otros productos de crédito (préstamos, pases, cauciones, acuerdos).
- Compensación interbancaria propia (eso hace COELSA).
- Notificaciones automáticas (email/push).

## 3. Usuarios (personas)

| Persona | Necesidad | Cómo la resolvemos |
|---|---|---|
| **Persona autorizada de una empresa** (cliente) | "Quiero cargar mis cheques a cobrar, ver mi cartera, adelantar la plata y ver mi saldo" | Autogestión web por empresa: cartera separada cheques/echeqs, negociación de descuento con costo visible antes de confirmar, cuenta y extracto |
| **Operador/oficial del banco** (nosotros) | "Necesito dar de alta empresas, asignar líneas y tasas, y ver la operación" | Alta de empresas con personas autorizadas (roles), tasa estándar al alta con ajuste individual, líneas de crédito por empresa |
| **Equipo de desarrollo** | "Integración al clearing con contratos estables" | Consumo de la API COELSA existente como fuente de verdad de instrumentos |

## 4. Historias de usuario y criterios de aceptación

### HU-01 — Alta de empresa con personas autorizadas
> Como operador quiero registrar una empresa y las personas que pueden operarla.

- Empresa por CUIT único; personas físicas con documento y email.
- Una persona puede estar autorizada en **varias empresas**, con rol
  (apoderado / firmante / consultor) y vigencia.
- Reenvíos del alta no duplican (idempotencia).

### HU-02 — Depositar instrumentos a cobro
> Como cliente quiero registrar cheques físicos y echeqs que me emitieron,
> para que formen mi cartera.

- Depósito por tanda; cada instrumento se valida contra el clearing
  (existencia, beneficiario = mi empresa, estado negociable).
- Cheques físicos y echeqs **nunca mezclados** en consultas ni operaciones.

### HU-03 — Ver mi cartera al día
> Como cliente quiero saber cuánto tengo por cobrar y cuándo.

- Cartera separada por tipo, con estado sincronizado del clearing, filtros por
  vencimiento y librador, y totalizado por moneda.
- Ágil incluso con carteras de miles de instrumentos, sin castigar al clearing
  con consultas repetidas.

### HU-04 — Aceptar o rechazar un echeq recibido
> Como cliente quiero decidir si acepto que me emitan un echeq.

- Desde la bandeja de recibidos: aceptar (queda en cartera) o rechazar con
  motivo; el rechazo se comunica al clearing.

### HU-05 — Endosar con conformidad del receptor
> Como cliente quiero transferir un instrumento a otra empresa.

- El endoso queda **Enviado** hasta que el receptor lo **acepte o rechace**
  con motivo; nadie recibe instrumentos que no quiso.
- Trazabilidad de la cadena de endosos por instrumento.

### HU-06 — Negociar el descuento de mi cartera
> Como cliente quiero elegir instrumentos a cobrar y adelantar esa plata.

- Selecciono instrumentos → veo **antes de confirmar**: nominal total, días al
  vencimiento por instrumento, tasa aplicada (la de mi empresa), interés,
  comisión y **neto a acreditar**.
- Requiere línea de crédito disponible por el monto nominal.
- Al confirmar: acreditación inmediata del neto en mi cuenta y los
  instrumentos quedan en poder del banco (endoso de cobro) hasta su vencimiento.
- Confirmar dos veces lo mismo no duplica la operación ni el dinero
  (idempotencia).

### HU-07 — Cobro y rechazo al vencimiento
> Como cliente quiero que el sistema resuelva el final de cada descuento.

- Instrumento cobrado en el clearing → la operación pasa a **Cobrada**, se
  libera línea. El dinero cobrado es del banco (ya se adelantó).
- Instrumento rechazado → la operación pasa a **Rechazada**: se debita el
  importe a la cuenta del cliente (contragarantía) y la línea queda ocupada
  hasta regularizar; el cliente lo ve con claridad en extracto y operación.

### HU-08 — Mi cuenta y extracto
> Como cliente quiero ver saldo y movimientos con confianza.

- Saldo por cuenta y moneda; extracto cronológico con concepto y origen de
  cada movimiento (desembolso, cobro, contragarantía, comisión).
- Los movimientos **nunca se borran ni editan**: la historia es inmutable.

### HU-09 — Administrar líneas y tasas
> Como operador quiero asignar a cada empresa su línea y su tasa.

- Tasa estándar al alta de la empresa, reemplazable por tasa individual con
  vigencia (nunca pisa historia: lo pactado queda congelado en cada operación).
- Línea de crédito por empresa con límite y estado; el uso disponible se
  recalcula desde las operaciones vivas.

## 5. Métricas de éxito

### De producto
| Métrica | Meta v1 |
|---|---|
| Cierre contable | Σ movimientos = saldo, en toda cuenta, siempre |
| Operaciones de dinero duplicadas | 0 (100% reenvíos idempotentes) |
| Costo financiero visible antes de confirmar | 100% de descuentos muestran desglose |

### Técnicas (del SPEC, RNF)
| Métrica | Meta |
|---|---|
| Consulta de cartera (empresa con miles de instrumentos) | p50 < 100 ms sin degradar al clearing |
| Errores 5xx | < 0,1% |
| Suite de dominio (descuento, ledger, estados, idempotencia) | Verde obligatoria en CI |

## 6. Alcance v1 (in/out)

**Dentro:** empresas + personas autorizadas con roles; depósito a cobro;
cartera sincronizada separada por tipo; aceptar/rechazar echeqs recibidos;
endosos con conformidad; descuento con tasa por empresa, comisión y línea de
crédito; cobro/rechazo al vencimiento; cuentas con ledger inmutable y extracto;
taller de líneas y tasas para el operador.

**Fuera:** autenticación/seguridad real, KYC, otros productos de crédito,
notificaciones, multi-empresa consolidado en una vista, escalado horizontal
(documentado como evolución).

## 7. Riesgos y mitigaciones

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Dinero inconsistente (descuentos/cobros duplicados o perdidos) | Alto | Idempotencia en toda operación financiera + ledger inmutable (RFC a crear) |
| Estado divergente con el clearing | Alto | Espejo de cartera con sincronización y reconciliación contra COELSA |
| Complejidad del descuento real (tasas, plazos, contragarantía) | Medio | Reglas de negocio acotadas en v1, explícitas en SPEC antes de codificar |
| Cartera pesada (miles de instrumentos por empresa) | Medio | Consultas cacheadas + paginado, sin golpe repetido al clearing (RFC a crear) |
| Costo de infra (dos productos, free tier) | Medio | Un solo despliegue nuevo con schemas separados por contexto (RFC a crear) |

## 8. Trazabilidad PRD ↔ documentación técnica

| Historia | RFCs relevantes | ADRs relevantes |
|---|---|---|
| HU-01 | RFC-004 (a crear) | ADR-001, ADR-002 |
| HU-02/03 | RFC-005 integración clearing (a crear) | ADR-004, ADR-005 |
| HU-04/05 | RFC-005 | ADR-010 |
| HU-06/07 | RFC-006 descuento y crédito (a crear) | ADR-003 |
| HU-08 | RFC-006 | ADR-003, ADR-007 |
| HU-09 | RFC-006 | — |
