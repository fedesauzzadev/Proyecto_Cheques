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
- `POST /api/v1/echeqs` crea un **echeq**.
- Cada tipo es creado por su **estrategia de creación** (patrón Strategy):
  - `ChequeFisicoCreationStrategy`: valida CMC7 único y bien formado (30 dígitos).
  - `EcheqCreationStrategy`: valida CMC7 bien formado y genera el IDECHEQ.
- Ambas estrategias implementan una interfaz común `ICrearInstrumentoStrategy`.

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
- `GET /api/v1/echeqs?cuit={cuit}&page=1&pageSize=10`
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
| Cmc7 | string(30) | CMC7 completo del echeq (mismo formato que el físico). **Único.** Se expone con su desglose derivado. |
| CuitLibrador / CuitBeneficiario | string(11) | CUIT/CUIL válido. |
| Monto / Moneda / FechaEmision / FechaDiferimiento | — | Igual que cheque físico. |
| Estado / MotivoRechazo / Auditoría | — | Igual que cheque físico. |
| CantidadEndosos | int | Default 0 (informativo en v1). |

### 5.3 Estados (dominio compartido)
`Emitido, Depositado, Compensado, Rechazado, Anulado, Pagado`

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
| echeqs | `cuit_librador, activo` / `cuit_beneficiario, activo` | Parciales `WHERE activo = true`. |
| idempotency_keys | `key` | Único. |

- Los índices parciales solo indexan instrumentos **vigentes** (los dados de baja
  no consumen el índice).
- `COUNT(*)` del paginado se resuelve con cache (ver sección 7), no por query en cada página.

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
  "fechaDiferimiento": "2026-10-05"
}
```

### CrearEcheqRequest
```json
{
  "cmc7": "011000114250000123400001234567",
  "cuitLibrador": "20123456789",
  "cuitBeneficiario": "30712345678",
  "monto": 250000.00,
  "moneda": "D",
  "fechaEmision": "2026-09-05"
}
```

> El IDECHEQ (11 letras mayúsculas) **lo genera el simulador** al crear el echeq; no viene en el request.
> La respuesta incluye el IDECHEQ asignado junto al CMC7 y su desglose.

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
- `seed` de ~30 instrumentos variados para probar paginación y cache.

## 10. Fuera de alcance (v1)

- Firmas digitales reales del echeq (el CUD se acepta como válido si es hex-64).
- Endosos y particiones del echeq (solo contador informativo).
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
