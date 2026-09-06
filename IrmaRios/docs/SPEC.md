# SPEC Banco IrmaRios — Slice 1 (Identidad + Cartera)

**Versión:** 0.1 (slice HU-01 → HU-03)
**Fecha:** 2026-09-06
**Padres:** [PRD IrmaRios](../../docs/PRD-banco-irmarios.md) · [RFC-004](../../docs/rfc/RFC-004-microservicios-irmarios.md)

Este SPEC documenta el **primer slice vertical** del banco: dos de los cuatro
microservicios, con las historias HU-01 a HU-03 de punta a punta. Los servicios
faltantes (Financiación, Cuentas) se especificarán al construirse.

## Arquitectura del slice

| Servicio | Puerto local | Base de datos | Rol |
|---|---|---|---|
| **Identidad** | 8081 | `postgres-identidad` (5433) | Empresas, personas, vínculos con rol (HU-01) |
| **Cartera** | 8082 | `postgres-cartera` (5434) + Redis (6380) | Espejo de instrumentos, depósitos, consultas (HU-02/03) |

Base de datos **privada por servicio** (regla RFC-004). Cartera se integra por HTTP:

- `Cartera → Identidad`: valida que la empresa depositante exista (`GET /api/v1/empresas/{cuit}`).
- `Cartera → COELSA` (fuente de verdad): consulta cada instrumento al depositarlo.

Ambas integraciones usan `AddStandardResilienceHandler` (retry + circuit breaker)
y propagan la traza W3C automáticamente (instrumentación OTel de HttpClient).

## Identidad — contrato

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/v1/empresas` | Alta de empresa + personas autorizadas opcionales (idempotente) |
| GET | `/api/v1/empresas/{cuit}` | Empresa por CUIT |
| POST | `/api/v1/empresas/{cuit}/personas` | Crea/reutiliza persona y la autoriza con rol (idempotente) |
| GET | `/api/v1/empresas/{cuit}/personas` | Personas autorizadas vigentes, paginado |
| GET | `/health` | Health check (Postgres) |

Reglas: CUIT con dígito verificador módulo 11 (compartido con COELSA); CUIT
único entre empresas activas (índice único parcial, baja lógica); una persona
se identifica por (tipo, número) de documento y puede operar varias empresas;
vínculo activo único por (persona, empresa, rol). Roles: `Apoderado`,
`Firmante`, `Consultor`.

## Cartera — contrato

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/v1/depositos` | Tanda de instrumentos a cobro, validada contra el clearing (idempotente) |
| GET | `/api/v1/cartera/{cuit}/cheques` | Cartera de cheques físicos, paginada, cacheada |
| GET | `/api/v1/cartera/{cuit}/echeqs` | Cartera de echeqs, paginada, cacheada (nunca mezclados) |
| GET | `/api/v1/cartera/instrumentos/{identificador}` | Detalle por CMC7 o IDECHEQ |
| GET | `/health` | Health check (Postgres + Redis) |

Reglas de ingreso (dominio): identificador bien formado (CMC7 30 dígitos,
IDECHEQ 11 letras); **beneficiario = empresa depositante**; estado del clearing
negociable (`Emitido`/`Pendiente`); no vencido; monto > 0. Aceptación parcial:
la tanda reporta `aceptados` y `rechazados` (con motivo); si nada ingresa → 400.

Cache de cartera: versionado por CUIT+tipo con invalidación en cada depósito,
TTL 30 min con jitter, anti-stampede con lock distribuido y **fail-open** a
PostgreSQL (mismo esquema de ADR-004/ADR-005 del ecosistema, prefijo `irmarios`).

## Estándar transversal (ambos servicios)

- Hexagonal en 4 proyectos (Api/Application/Domain/Infrastructure), como Coelsa.
- `Idempotency-Key` obligatoria en creaciones; replay devuelve la respuesta
  original con header `Idempotent-Replay: true`; key + body distinto → 409.
- ProblemDetails RFC 7807 en español; dominio decide el código (400/404/409/422/502).
- `X-Correlation-ID` en toda respuesta; trazas W3C entre servicios (OTel).
- Observabilidad: OpenTelemetry traces+metrics activo si `OTEL_EXPORTER_OTLP_ENDPOINT`
  está configurado (Grafana Cloud); sin la variable, logging de consola.
- Rate limit global token bucket 60 req/min por IP.
- Esquema: `EnsureCreated` en el slice; migraciones EF cuando el modelo se estabilice.

## Operación

- **Local**: `docker compose up --build` desde `IrmaRios/` (identidad 8081,
  cartera 8082, Postgres 5433/5434, Redis 6380). Cartera apunta al COELSA dev
  de Render: los CMC7 válidos para depositar son los que existen allí (p. ej.
  los 5.000 cheques de la carga masiva).
- **Deploy**: `render.yaml` agrega `irmarios-identidad-dev` e
  `irmarios-cartera-dev` (solo rama develop). Secretos `sync: false` en el
  dashboard; OTLP endpoint/headers se completan al configurar Grafana Cloud.
- **CI**: `.github/workflows/irmarios.yml` (por paths `IrmaRios/**`): build +
  tests de ambas soluciones + build de ambas imágenes Docker.

## Fuera de este slice (próximos pasos del PRD)

Financiación y Cuentas (HU-06/07/08/09), saga del descuento con outbox y QStash
(HU-06/07), endosos con conformidad y aceptación de echeqs (HU-04/05), sincronización
periódica del espejo con el clearing, front `Front_IrmaRios`, migraciones EF,
dashboards de negocio en Grafana, ambiente prod.
