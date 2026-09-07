# SPEC: Microservicio Simulador COELSA — Cheques y Echeqs

**Versión:** 1.1-draft
**Estado:** Pendiente de validación
**Fecha:** 2026-09-05

---

## 1. Objetivo y alcance

Construir un microservicio que **simula** el rol de COELSA como cámara electrónica
de compensación, limitado a la gestión de **cheques físicos** y **echeqs**
(cheques electrónicos).

Es el primer servicio de un ecosistema mayor: un "banco nuevo" que ofrecerá
autogestión web para el **descuento de cheques/echeqs**. Este microservicio será
la fuente de verdad simulada de los instrumentos negociables.

## 2. Glosario

| Término | Definición |
|---|---|
| **Cheque físico** | Cheque de papel identificado por su CMC7. |
| **Echeq** | Cheque electrónico emitido contra una cuenta bancaria, identificado por su IDECHEQ. |
| **CMC7** | Código magnetizable de 30 dígitos de la banda inferior del cheque: banco + sucursal + código postal + número de cheque + número de cuenta. Identificador único del cheque físico y clave de búsqueda individual. |
| **IDECHEQ** | Identificador alfabético de 11 letras mayúsculas asignado al momento de crear el echeq. Clave de búsqueda individual del echeq (los cheques físicos no tienen IDECHEQ). |
| **CUD** | Clave Única Digital del echeq (hash SHA-256, 64 caracteres hexadecimales). |
| **Librador** | Quien emite el cheque (persona/empresa con cuenta en un banco). |
| **Beneficiario** | Quien puede cobrar/depositar el instrumento. |
| **Diferimiento** | Fecha futura de pago (cheques de pago diferido). |
| **Motivo de rechazo** | Código estandarizado del motivo por el cual un banco rechaza el pago. |

## 3. Requisitos funcionales

### RF-01 — Creación de instrumentos (con Strategy)
- `POST /api/v1/cheques` crea un **cheque físico**.
- `POST /api/v1/echeqs` crea un **echeq** (exige `cbuEmisor` con chequera vigente).
- Cada tipo es creado por su **estrategia de creación** (patrón Strategy):
  - `ChequeFisicoCreationStrategy`: valida CMC7 único y bien formado (30 dígitos).
  - `EcheqCreationStrategy`: verifica cuenta (existe y activa), titularidad
    (librador = titular) y moneda; reserva el número de la primera chequera
    vigente con lugar y deriva IDECHEQ (11 letras) + CMC7 (30 dígitos).
- Ambas estrategias implementan una interfaz común `ICrearInstrumentoStrategy`.

### RF-01b — Cuentas y e-chequeras (Fase B)
- `POST /api/v1/cuentas` crea una **cuenta corriente** emisora (CBU 22 dígitos
  con verificadores, CUIT titular, moneda). Idempotente.
- `GET /api/v1/cuentas?cuit=` lista las cuentas activas del titular.
- `GET /api/v1/cuentas/{cbu}` obtiene una cuenta.
- `POST /api/v1/cuentas/{cbu}/chequeras` solicita una **e-chequera** nueva
  (50 números, vigente de inmediato — el simulador acelera las 24h reales;
  reintentar con la misma key no duplica). Idempotente.
- `GET /api/v1/cuentas/{cbu}/chequeras` lista las chequeras con sus disponibles.
- `GET /api/v1/cuentas/titulares?tipoDoc=&numero=` es el padrón simulado
  ("lupa"): valida el documento y devuelve el nombre si el titular tiene
  cuenta (`bancarizado`). Divergencia documentada: el simulador **no bloquea**
  beneficiarios no bancarizados (el banco real lo exige); el nombre igual es
  obligatorio al emitir.
- Sin chequera con lugar al emitir → `409` ("solicite una chequera nueva").

### RF-02 — Idempotencia en creación
- Header **obligatorio** `Idempotency-Key` (GUID) en ambos POST.
- Sin header → `400 Bad Request`.
- La key se **persiste** en PostgreSQL junto al id creado (sobrevive reinicios).
- Misma key + mismo body → se devuelve el recurso original con
  `200 OK` + header `Idempotent-Replay: true`.
- Misma key + body distinto → `409 Conflict`.
- Unicidad de negocio (CMC7 o IDECHEQ duplicados con key distinta) → `409 Conflict`.

### RF-03 — Consulta por CUIT/CUIL, separada por tipo, paginada
- `GET /api/v1/cheques?cuit={cuit}&page=1&pageSize=10`
- `GET /api/v1/echeqs?cuit={cuit}&page=1&pageSize=10` más filtros opcionales
  (Fase B6): `cbu`, `estado`, `desdeEmision`, `hastaEmision`,
  `desdeVencimiento`, `hastaVencimiento`, `numeroCheque`. Los rangos por eje no
  pueden superar 360 días.
- El `cuit` filtra por librador **o** beneficiario (ambos extremos consultan).
- `page` es 1-based; `pageSize` default 10, máximo 100.
- Jamás se devuelven ambos tipos en un mismo endpoint.
- Respuesta envolvente: `items`, `page`, `pageSize`, `totalCount`, `totalPages`.

### RF-04 — Consulta individual (por identificador de negocio)
- `GET /api/v1/cheques/{cmc7}` → búsqueda por CMC7 (30 dígitos) → `200` o `404`.
- `GET /api/v1/echeqs/{idecheq}` → búsqueda por IDECHEQ → `200` o `404`.
- El id interno (Guid) es solo PK de base de datos; **la API expone identificadores de negocio** (nadie consulta a COELSA por un GUID).

### RF-05 — Actualización de estado
- `PATCH /api/v1/cheques/{cmc7}/estado` y `PATCH /api/v1/echeqs/{idecheq}/estado`, con body `{ "estado": "...", "motivoRechazo": "..." }`.
- Transiciones válidas (máquina de estados simplificada):

```
Emitido ──► Depositado ──► Compensado ──► Pagado
   │            │
   │            └──► Rechazado
   └──► Anulado
```
- Transición inválida → `422 Unprocessable Entity` con detalle.
- `Rechazado` exige `motivoRechazo` (código de rechazo); los demás estados lo prohíben.

### RF-06 — Baja lógica (soft delete)
- `DELETE /api/v1/cheques/{cmc7}` y `DELETE /api/v1/echeqs/{idecheq}`.
- Marca `FechaBaja` + `Activo = false`; el registro no se expone más en consultas.
- Respuesta `204 No Content`; identificador inexistente → `404`.

### RF-07 — Health check
- `GET /health` verifica API, PostgreSQL y Redis (liveness + readiness).

### RF-08 — Rate limiting (protección de flujo)
- Middleware nativo de .NET 8 (token bucket) en dos políticas:
  - **Consultas** (`GET /api/v1/cheques` y `GET /api/v1/echeqs`): más restrictiva.
  - **Creaciones** (`POST`): límite propio.
- Al exceder → `429 Too Many Requests` con header `Retry-After`.
- Configurable por `appsettings` (por defecto: 100 req/min por IP en consultas).

### RF-09 — Aceptación y repudio del echeq (solo echeqs)
- El echeq nace en `Pendiente`; el beneficiario lo acepta o repudia:
  `POST /api/v1/echeqs/{idecheq}/aceptacion` con `{ "aceptada": true/false, "motivo?" }`.
- Aceptar → `Emitido` (entra en circulación, sin motivo); repudiar → `Repudiado`
  (terminal, **exige motivo** de hasta 280 caracteres, se guarda en el echeq).
- Desde `Pendiente` también se puede `Anular` (el librador lo anula si no fue aceptado).
- Aceptar/repudiar algo no pendiente → `422`; inexistente → `404`.

### RF-10 — Endosos del echeq hasta 100 (solo echeqs)
- `POST /api/v1/echeqs/{idecheq}/endosos` con `{ "cuitEndosatario" }` propone un
  endoso nominativo (el endosante es siempre el tenedor actual). Solo en `Emitido`.
- `POST /api/v1/echeqs/{idecheq}/endosos/{orden}/admision` con
  `{ "admitido": true/false, "cuit" }`: solo el endosatario resuelve. Al admitir,
  la tenencia pasa al endosatario y se refresca `CantidadEndosos`.
- `DELETE /api/v1/echeqs/{idecheq}/endosos/{orden}` anula un endoso propuesto.
- `GET /api/v1/echeqs/{idecheq}/endosos` devuelve la cadena ordenada (trazabilidad).
- Ciclo por endoso: `Propuesto → Vigente / Repudiado`; `Propuesto → Anulado`;
  una devolución aceptada deja en `Revertido` los posteriores al solicitante.
- Tope de 100 endosos no anulados → `409`.

### RF-11 — Custodia, rescate y débito automático (solo echeqs)
- `EnCustodia` vía `PATCH /api/v1/echeqs/{idecheq}/estado` (`Emitido ⇄ EnCustodia`, más `Emitido/EnCustodia → Caducado`).
- En custodia no se puede endosar ni pasar a otro estado salvo rescate, depósito o caducidad.
- Un worker (cada 5 minutos) deposita automáticamente los echeqs en custodia
  vencidos dentro de la ventana de presentación (`EnCustodia → Depositado`) y
  caduca los que la superaron (`EnCustodia → Caducado`); nunca tumba el host (errores logueados).
- `DELETE` de la custodia no existe: salir es `Rescatar` (vuelve a `Emitido`).

### RF-12 — Pedidos de devolución del echeq (solo echeqs)
- `POST /api/v1/echeqs/{idecheq}/devoluciones` con `{ "cuitSolicitante", "motivo?" }`:
  cualquier integrante de la cadena distinto del tenedor actual puede pedirla
  (solo en `Emitido`, sin otro pedido pendiente → `409` si lo hay).
- `POST /api/v1/echeqs/{idecheq}/devoluciones/{numero}/resolucion` con
  `{ "aceptada", "cuitResolutor" }`: solo el tenedor actual resuelve. Al aceptar,
  la tenencia vuelve al solicitante y se revierten los endosos vigentes
  posteriores a su posición.
- `DELETE /api/v1/echeqs/{idecheq}/devoluciones/{numero}` anula un pedido solicitado.
- `GET /api/v1/echeqs/{idecheq}/devoluciones` lista los pedidos para trazabilidad.

### RF-13 — Cesiones del echeq "no a la orden" (solo echeqs, Fase D1)
- `POST /api/v1/echeqs/{idecheq}/cesiones` con `{ "cuitCesionario", "domicilioCesionario" }`:
  el tenedor actual cede a un tercero bancarizado (solo en `Emitido` y con
  carácter `NoAlaOrden` → `422` si es `AlaOrden`; sin otra cesión pendiente → `409`).
- `POST /api/v1/echeqs/{idecheq}/cesiones/{numero}/resolucion` con
  `{ "aceptada", "cuitResolutor" }`: solo el cesionario resuelve. Al aceptar,
  la tenencia pasa al cesionario.
- `DELETE /api/v1/echeqs/{idecheq}/cesiones/{numero}` anula una cesión solicitada.
- `GET /api/v1/echeqs/{idecheq}/cesiones` lista las cesiones para trazabilidad.
- Tope de 10 cesiones no anuladas → `409`.

### RF-14 — Certificado para acciones civiles (solo echeqs rechazados, Fase D3)
- `GET /api/v1/echeqs/{idecheq}/certificado` devuelve el CUD (SHA-256
  determinista de los datos), el código de visualización (primeros 12) y los
  datos del rechazo. Solo en `Rechazado` (→ `422` si no); inexistente → `404`.
- No se persiste (se deriva como el desglose). Habilita el reclamo judicial y
  es la base documental de la contragarantía en el descuento.

## 4. Requisitos no funcionales

| ID | Requisito |
|---|---|
| RNF-01 | .NET 8 (LTS), C# 12, minimal APIs o controllers (controllers por claridad). |
| RNF-02 | Arquitectura hexagonal: `Domain`, `Application` (puertos), `Infrastructure` y `Api` (adaptadores). |
| RNF-03 | SOLID; inyección de dependencias nativa de .NET. |
| RNF-04 | PostgreSQL vía **EF Core 8** + migraciones. |
| RNF-05 | Cache **Redis** en consultas por CUIT. |
| RNF-06 | Contenedores: `Dockerfile` multi-stage + `docker-compose.yml` (api, postgres, redis). |
| RNF-07 | Errores con **ProblemDetails** (RFC 7807), en español. |
| RNF-08 | Swagger/OpenAPI expuesto en desarrollo. |
| RNF-09 | Nombres de código y dominio en **español** (ubiquitous language). |
| RNF-10 | Tests unitarios xUnit para dominio (CUIT, estados, idempotencia). |
| RNF-11 | Índices parciales y compuestos en PostgreSQL para consultas por CUIT (ver 5.5). |
| RNF-12 | Protección de picos: rate limiting, cache stampede protection y TTL con jitter (ver RF-08 y 7). |
| RNF-13 | Degradación elegante: fail-open a PostgreSQL si Redis no está disponible. |
| RNF-14 | Compresión de respuestas (brotli/gzip) en listados. |

## 5. Modelo de dominio

### 5.1 ChequeFisico
| Campo | Tipo | Reglas |
|---|---|---|
| Id | Guid | PK interna, no expuesta en la API. |
| Cmc7 | string(30) | 30 dígitos: banco(3) + sucursal(4) + código postal(4) + número de cheque(8) + cuenta(11). **Único.** Clave de búsqueda. |
| CuitLibrador | string(11) | CUIT/CUIL válido (algoritmo módulo 11). |
| CuitBeneficiario | string(11) | CUIT/CUIL válido. |
| Monto | decimal(18,2) | > 0. |
| Moneda | string(1) | `P` (pesos) o `D` (dólares). |
| FechaEmision | DateOnly | ≤ hoy+1. |
| FechaDiferimiento | DateOnly? | ≥ FechaEmision; null = a la vista. |
| FechaVencimiento | DateOnly | Obligatoria; > FechaEmision y > FechaDiferimiento si viene. |
| Estado | enum | Ver 5.3. |
| MotivoRechazo | enum? | Requerido solo si `Rechazado`. |
| FechaCreacion / FechaModificacion / FechaBaja / Activo | — | Auditoría y soft delete. |

> El desglose (banco, sucursal, código postal, número de cheque, cuenta) se
> **deriva del CMC7** al armar la respuesta; no se persisten columnas redundantes.

### 5.2 Echeq
| Campo | Tipo | Reglas |
|---|---|---|
| Id | Guid | PK interna, no expuesta en la API. |
| IdEcheq | string(11) | Alfabético **generado por el simulador** al crear (11 letras mayúsculas). **Único.** Clave de búsqueda. |
| CbuEmisor | string(22) | CBU de la cuenta de débito. El librador debe ser su titular y la moneda debe coincidir. |
| Caracter | enum | `AlaOrden` (endosable) / `NoAlaOrden` (solo cesión, Fase D). Todos son cruzados. |
| TipoDocBeneficiario | enum | `CUIT` / `CUIL` / `CDI` (11 dígitos con verificador). |
| NombreLibrador / NombreBeneficiario | string(120) | Razón social o nombre y apellido (validados por la "lupa" en el front). |
| Concepto | string(60)? | Opcional (texto libre; en el banco real es lista desplegable). |
| Motivo | string(280)? | Opcional, razón de la emisión. |
| Referencia | string(60)? | Opcional, referencia libre (ej. `FAC-123`). |
| EmailNotificacion | string(160)? | Opcional, aviso al beneficiario. |
| NumeroChequera / NumeroCheque | int | Chequera (secuencial por cuenta) y número reservado (1-50). Únicos junto al CBU. |
| Cmc7 | string(30) | **Derivado** de CBU + número: banco(3) + sucursal(4) + `2077` + número(8) + últimos 11 de la cuenta. **Único.** Se expone con su desglose derivado. |
| CuitLibrador / CuitBeneficiario | string(11) | CUIT/CUIL válido. |
| Monto / Moneda / FechaEmision / FechaDiferimiento / FechaVencimiento | — | Igual que cheque físico. |
| Estado / MotivoRechazo / Auditoría | — | Igual que cheque físico. |
| CantidadEndosos | int | Default 0 (informativo en v1). |

### 5.2b Cuenta y Chequera (Fase B)
| Entidad | Campo | Tipo | Reglas |
|---|---|---|---|
| Cuenta | Cbu | string(22) | Dígitos con verificadores (entidad 3 + sucursal 4 + cuenta 13). **Único.** Clave de búsqueda. Banco/sucursal/cuenta se derivan, no se persisten. |
| Cuenta | CuitTitular | string(11) | CUIT/CUIL válido. Índice por titular. |
| Cuenta | NombreTitular | string(120) | Razón social del titular (la "lupa" la devuelve). |
| Cuenta | Moneda | string(1) | `P` o `D`. El echeq debe emitirse en la moneda de la cuenta. |
| Chequera | CuentaId | Guid | FK a la cuenta (sin navegación EF, como endosos/devoluciones). |
| Chequera | Numero | int | Secuencial por cuenta (1-based). Único junto a CuentaId. |
| Chequera | ProximoNumero | int | Próximo a reservar (1-51; 51 = agotada). 50 números por chequera. |
| Chequera | Estado | enum | `Vigente, Agotada`. |

> La e-chequera real se habilita a las 24h; el simulador la habilita de
> inmediato y lo documenta en `FechaHabilitacion`.

### 5.3 Estados (dominio compartido)
`Emitido, Depositado, Compensado, Rechazado, Anulado, Pagado, Caducado` más los exclusivos
de echeq (Fase A): `Pendiente` (nace así, espera aceptación), `Repudiado`
(terminal, el beneficiario no lo aceptó) y `EnCustodia` (en poder del banco).

```
Pendiente ──► Emitido ──► Depositado ──► Compensado ──► Pagado
   │  │          │            │
   │  │          │            └──► Rechazado
   │  │          ├──► EnCustodia ⇄ Emitido
   │  │          │         │
   │  │          │         └──► (worker) Depositado al vencer
   │  │          ├──► Caducado (plazo de presentación vencido, 30 días)
   │  └──► Anulado
   └──► Repudiado
```

Reglas de fecha reales (Fase B5): tenor emisión→vencimiento ≤ 360 días;
presentación al cobro dentro de los 30 días del vencimiento (después el
depósito se rechaza con 422 y la custodia pasa a `Caducado` por el worker).

### 5.4 Motivos de rechazo (códigos de rechazo, subconjunto real)
| Código | Motivo |
|---|---|
| 11 | Falta de fondos. |
| 12 | Cuenta inexistente / embargada. |
| 21 | Defecto formal. |
| 25 | Cheque adulterado o falsificado. |

### 5.5 Índices y acceso a datos (diseño para volumen)

> El problema real de COELSA son las consultas sobre volúmenes masivos. Sin estos
> índices, cada consulta por CUIT es un full table scan.

| Tabla | Índice | Tipo |
|---|---|---|
| cheques_fisicos | `cuit_librador, activo` | **Parcial** `WHERE activo = true` + INCLUDE columnas del listado (covering). |
| cheques_fisicos | `cuit_beneficiario, activo` | Parcial `WHERE activo = true`. |
| cheques_fisicos | `cmc7` | Único. |
| echeqs | `id_echeq` | Único. |
| echeqs | `cmc7` | Único. |
| echeqs | `(cbu_emisor, numero_chequera, numero_cheque)` | Único (trazabilidad chequera). |
| cuentas | `cbu` | Único. |
| cuentas | `cuit_titular, activa` | Búsqueda por titular. |
| chequeras | `(cuenta_id, numero)` | Único (secuencial por cuenta). |
| chequeras | `(cuenta_id, estado, proximo_numero)` | Chequera con lugar. |
| echeqs | `cuit_librador, activo` / `cuit_beneficiario, activo` | Parciales `WHERE activo = true`. |
| idempotency_keys | `key` | Único. |
| endosos | `echeq_id, orden` | Único (orden secuencial por echeq). |
| devoluciones | `echeq_id, numero` | Único (numeración secuencial por echeq). |

- Los índices parciales solo indexan instrumentos **vigentes** (los dados de baja
  no consumen el índice).
- `COUNT(*)` del paginado se resuelve con cache (ver sección 7), no por query en cada página.

### 5.6 Endosos, devoluciones y cesiones (Fase A + D1, solo echeqs)

| Entidad | Campo | Tipo | Reglas |
|---|---|---|---|
| Endoso | EcheqId | Guid | FK al echeq. |
| Endoso | Orden | int | Secuencial por echeq (1-based, sin reuso). Único junto a EcheqId. |
| Endoso | CuitEndosante / CuitEndosatario | string(11) | CUIT/CUIL válido; distintos entre sí. El endosante es siempre el tenedor actual. |
| Endoso | Estado | enum | `Propuesto, Vigente, Repudiado, Anulado, Revertido` (ver RF-10). |
| Devolucion | EcheqId | Guid | FK al echeq. |
| Devolucion | Numero | int | Secuencial por echeq (1-based). Único junto a EcheqId. |
| Devolucion | CuitSolicitante | string(11) | CUIT/CUIL válido; integrante de la cadena distinto del tenedor. |
| Devolucion | Motivo | string(280)? | Opcional. |
| Devolucion | Estado | enum | `Solicitada, Aceptada, Rechazada, Anulada` (ver RF-12). |
| Cesion | EcheqId | Guid | FK al echeq. |
| Cesion | Numero | int | Secuencial por echeq (1-based). Único junto a EcheqId. |
| Cesion | CuitCedente / CuitCesionario | string(11) | CUIT/CUIL válido; distintos entre sí. El cedente es siempre el tenedor actual. |
| Cesion | DomicilioCesionario | string(200) | Obligatorio. |
| Cesion | Estado | enum | `Solicitada, Aceptada, Rechazada, Anulada` (ver RF-13). Tope 10 no anuladas. |

## 6. Contrato de API (DTOs)

> **Contrato formal:** `docs/openapi.yaml` (OpenAPI 3.0.3, design-first) es la
> fuente de verdad de la API. Los ejemplos de esta sección son ilustrativos;
> ante cualquier divergencia prevalece el OpenAPI. Swagger UI servirá este
> contrato en desarrollo.

### CrearChequeFisicoRequest
```json
{
  "cmc7": "060000114250000123400001234567",
  "cuitLibrador": "20123456789",
  "cuitBeneficiario": "30712345678",
  "monto": 1500000.50,
  "moneda": "P",
  "fechaEmision": "2026-09-05",
  "fechaDiferimiento": "2026-10-05",
  "fechaVencimiento": "2026-11-05"
}
```

### CrearEcheqRequest
```json
{
  "cbuEmisor": "0110001300000000000017",
  "caracter": "AlaOrden",
  "tipoDocBeneficiario": "CUIT",
  "nombreLibrador": "Alfa S.R.L.",
  "nombreBeneficiario": "Beta S.A.",
  "concepto": "Pago a proveedores",
  "referencia": "FAC-123",
  "emailNotificacion": "cobros@demo.local",
  "cuitLibrador": "20123456789",
  "cuitBeneficiario": "30712345678",
  "monto": 250000.00,
  "moneda": "D",
  "fechaEmision": "2026-09-05",
  "fechaVencimiento": "2026-10-05"
}
```

> El IDECHEQ (11 letras mayúsculas) y el CMC7 (30 dígitos, derivado del CBU +
> número de chequera) **los genera el simulador** al crear el echeq; no vienen
> en el request. La cuenta debe tener chequera vigente con lugar (si no → 409).
> La respuesta incluye ambos identificadores, el CBU, la chequera/número y el
> desglose del CMC7.

### CrearCuentaRequest
```json
{
  "cbu": "0110001300000000000017",
  "cuitTitular": "20123456789",
  "nombreTitular": "Alfa S.R.L.",
  "moneda": "P"
}
```

### Respuesta (ChequeResponse / EcheqResponse)
```json
{
  "identificador": "060000114250000123400001234567 | ABCDEFGHIJK",
  "tipo": "ChequeFisico | Echeq",
  "estado": "Emitido",
  "motivoRechazo": null,
  "...": "campos del instrumento",
  "fechaCreacion": "2026-09-05T10:00:00Z"
}
```

> `identificador` es el **CMC7** para cheques físicos y el **IDECHEQ** para echeqs.
> El Guid interno de base de datos no se expone en ningún endpoint.

### Listado paginado (PagedResponse<T>)
```json
{
  "items": [ ... ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 42,
  "totalPages": 5
}
```

### Cambio de estado
`PATCH /api/v1/{tipo}/{id}/estado`
```json
{ "estado": "Rechazado", "motivoRechazo": 11 }
```

## 7. Comportamiento del cache (Redis)

| Aspecto | Decisión |
|---|---|
| Clave | `coelsa:{tipo}:cuit:{cuit}:v{N}:p{page}:{pageSize}` |
| TTL | 30 minutos **+ jitter aleatorio de ±10%** (evita expiración sincronizada masiva de claves). |
| Invalidación | **Versionado por CUIT**: hash `coelsa:ver:{tipo}:{cuit}` (int). Toda escritura (create/update/delete) de un instrumento con CUIT X incrementa la versión → las lecturas usan la nueva versión y las claves viejas expiran solas. |
| Stampede | **Lock por clave** (`SETNX` + expiry corto en Redis): al expirar una clave popular, un solo request recalienta desde PostgreSQL; el resto espera brevemente y lee la nueva versión. |
| totalCount | El `COUNT(*)` de la paginación se cachea junto con la página (misma clave/versionado): no se golpea la BD en cada cambio de página. |
| Fail-open | Si Redis no responde → la consulta va directo a PostgreSQL. **La cache es optimización, no dependencia dura** (clave de disponibilidad). |
| Lecturas individuales por id | Sin cache (van directo a PostgreSQL). |
| Serialización | JSON (UTF-8). |

## 8. Arquitectura y estructura

```
src/
  Coelsa.Domain/            # Entidades, enums, validación de dominio (sin dependencias)
  Coelsa.Application/       # Casos de uso, puertos (interfaces), DTOs, estrategias
  Coelsa.Infrastructure/    # EF Core, repositorios, Redis, idempotencia
  Coelsa.Api/               # Controllers, middleware, DI composition root
tests/
  Coelsa.UnitTests/
docs/
  SPEC.md
```

**Flujo hexagonal:** Controller → Caso de uso (Application) → Puerto
(IRepository, ICachePort, IIdempotenciaPort) → Adaptador (Infrastructure).

## 9. Infraestructura

- `docker-compose.yml` con servicios:
  - `api`: build del Dockerfile, puerto 8080.
  - `postgres`: postgres:16, volumen persistente.
  - `redis`: redis:7.
- Migraciones EF Core aplicadas al arranque en desarrollo (incluyen índices de 5.5).
- Rate limiting (RF-08) y compresión de respuesta configurados en el host.
- `seed` de ~30 instrumentos variados para probar paginación y cache, más vitrina
  Fase A (echeq pendiente, repudiado, en custodia, con cadena de endosos y devolución solicitada).

## 10. Fuera de alcance (v1)

- Firmas digitales reales del echeq.
- Débito automático con garantías de liquidación (el worker deposita sin neteo multilateral).
- Autenticación/autorización (se agregará como API Key en el servicio "banco").
- Ciclo completo de compensación entre bancos (futuro microservicio).
- Conciliación y liquidación.
- **Escalado** (horizontal/vertical, réplicas de lectura, particionado por fecha,
  sharding, CQRS con read-store dedicado, colas asíncronas): documentado como
  evolución natural del simulador; lo que v1 sí aplica son los patrones de
  RNF-11 a RNF-14, que mitigan el problema de consultas sin escalar.

## 11. Plan de implementación (checklist)

1. [ ] Esqueleto de solución + 4 proyectos + referencias hexagonales.
2. [ ] Dominio: entidades, enums, validador CUIT, máquina de estados.
3. [ ] Application: DTOs, puertos, casos de uso, estrategias de creación.
4. [ ] Infrastructure: DbContext EF, repositorios, migraciones (con índices de 5.5), idempotencia, Redis (stampede + fail-open).
5. [ ] Api: controllers, ProblemDetails, health, Swagger (sirviendo docs/openapi.yaml), rate limiting, compresión, DI.
6. [ ] Dockerfile + docker-compose + seed.
7. [ ] Tests unitarios (CUIT, estados, idempotencia, paginación, fail-open de cache).
8. [ ] Verificación end-to-end con docker compose up.
