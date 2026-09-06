# RFC-004: Arquitectura de Banco IrmaRios — microservicios por contexto delimitado

**Estado:** accepted
**Fecha:** 2026-09-06

## Contexto

El PRD de IrmaRios está aceptado (dinero consistente, descuento con
contragarantía, cartera read-heavy, free tier) y declara como **objetivo de
aprendizaje** operar microservicios: REST y SOLID entre servicios, HTTP
síncrono y colas asíncronas, con observabilidad distribuida (HU-10) y visión
futura de descuento conversacional por IA/MCP. El ecosistema ya es
parcialmente distribuido: Coelsa es un servicio separado con idempotencia,
rate limit y health checks verificados en producción.

Restricciones duras: presupuesto cero (Render 750 hs-instancia/mes compartidas,
Neon y Upstash free), un solo equipo autodidacta, y un dominio donde el
descuento toca tres contextos a la vez (línea, desembolso, endoso).

## Propuesta

### 1. Cuatro microservicios, cortados por contexto delimitado (no por entidad)

| Servicio | Dueño de sus datos | Razón de ser separado |
|---|---|---|
| **Identidad** | personas, empresas, vínculos, roles | Lo consumen todos; ciclo de vida y lectura propias |
| **Cartera** | instrumentos espejo, depósitos, endosos, aceptaciones | Read-heavy, cache propio, única puerta a COELSA |
| **Financiación** | líneas, tasas, operaciones de descuento | Reglas crediticias; orquesta la saga del descuento |
| **Cuentas** | cuentas, ledger inmutable, saldos | Consistencia de dinero; el servicio más protegido |

Cada servicio replica internamente el estándar Coelsa: hexagonal en 4
proyectos (Api/Application/Domain/Infrastructure), idempotencia en creaciones,
ProblemDetails en español, health check, rate limit, migraciones propias.

**Anti-regla explícita:** no hay servicio de "usuarios", ni de "empresas", ni
de "tasas". Las entidades viven dentro de su contexto; cortar por entidad
produce joins distribuidos y el monolito distribuido.

### 2. Datos: base privada por servicio

Neon: un proyecto por ambiente, **una base de datos por servicio** dentro del
proyecto. Ningún servicio se conecta a la base de otro, ni en lectura
("compartir base es compartir schema es compartir mierda"). Las necesidades
de datos ajenos se resuelven con contratos de lectura o réplica de eventos.

### 3. Comunicación

**Síncrona (REST), para lo que necesita respuesta ya:**
- Contratos gruesos orientados a use cases (`POST /desembolsos` en Cuentas,
  no `POST /movimientos`).
- `Idempotency-Key` obligatorio en toda escritura entre servicios (la
  entrega es at-least-once por naturaleza).
- Propagación de `traceparent` (W3C) + `X-Correlation-ID`.
- Timeout + retry con backoff y jitter solo en idempotentes; circuit breaker
  simple en el cliente HTTP compartido.

**Asíncrona (cola Upstash), para lo que el negocio es asíncrono:**
- Recomendado: **QStash** — push HTTP at-least-once con reintentos nativos;
  el consumidor es un endpoint normal del servicio, sin procesos worker
  (Render free no los tiene).
- Alternativa seria: **Redis Streams** de Upstash con `BackgroundService`
  (patrón ya probado en Coelsa con `DepositadorCustodiasWorker`); gana si
  necesitamos consumer groups.
- Eventos de dominio mínimos v1: `InstrumentoCompensado`,
  `InstrumentoRechazado` (Cartera → Financiación),
  `OperacionConcertada/Cobrada/Rechazada` (Financiación → quien escuche).
- **Outbox por servicio:** el evento se persiste en la misma transacción del
  negocio y un relay lo publica después. Sin outbox, el primer timeout pierde
  dinero.

### 4. Saga del descuento (orquestada por Financiación)

```
Cliente confirma cotización
 └─ Financiación: crea OperacionDescuento (estado Concertando) + outbox
    1. Cuentas:     POST /desembolsos   (neto a la cuenta)     ── si falla → opera Abortada
    2. Cartera:     POST /endosos-al-banco (cobro) ── si falla → compensar (1): débito del neto
    3. Financiación: ocupa línea, congela tasa, estado Concertada
Al vencer (cola, worker de Cartera detecta en COELSA):
    compensado → evento → Financiación: Cobrada, libera línea
    rechazado  → evento → Financiación: Rechazada → Cuentas: débito contragarantía;
                  la línea sigue ocupada hasta regularizar
```

Sin 2PC: cada paso es idempotente, cada fallo tiene compensación, y la
reconciliación (job periódico que compara estados operacion↔cuenta↔cartera)
atrapa lo que se pierda en el medio.

### 5. Observabilidad (HU-10)

- **OpenTelemetry en los 4 servicios**: traces + metrics + logs con un solo
  SDK, propagación automática por HTTP y metadatos de la cola.
- Export OTLP a **Grafana Cloud free** (dashboards incluidos); alternativa a
  evaluar: Axiom (logs/traces) o Seq local en dev.
- Dos familias de tableros: **técnicos** (latencia/errores/saturación por
  servicio, profundidad de cola, reintentos de outbox) y **de negocio**
  (monto descontado por día, tasa promedio pactada, rechazos, uso de líneas).
- Log estructurado con `operationId` en toda operación financiera.

### 6. Despliegue y desarrollo local

- `render.yaml`: +4 servicios **solo en dev** al inicio (prod cuando haya
  producto mostrable). Presupuesto de horas por ambiente, documentado.
- `docker-compose` para desarrollar todo local (4 APIs + front + dependencias).
- CI por paths, como hoy (`ci.yml` para `IrmaRios/**`, workflow propio).

## Alternativas consideradas

### Alternativa A — Monolito modular (4 contextos, schemas separados, un deploy)
Pros: el descuento es **una transacción de base de datos** (sin saga); un solo
deploy y pipeline; debugging trivial; free tier holgado; con límites duros y
verificados en CI, la extracción futura de un contexto es barata.
Contras: no ejercita el objetivo declarado (operar sistemas distribuididos:
colas, outbox, consistencia eventual); la saga se aprendería tarde y con
dinero de verdad arriba; la narrativa de producto (crecer, escalar, MCP)
pide fluidez distribuida desde el día 0.
**Veredicto: descartada por objetivo de aprendizaje y visión, no por
ingeniería.** Es la respuesta correcta si el objetivo fuera solo producir.

### Alternativa B — Microservicios por entidad (usuarios, empresas, tasas…)
Pros: granularidad "pura" que suena bien en un diagrama.
Contras: cada autorización es un join distribuido (persona→empresa→rol);
contratos chatos; el síndrome del monolito distribuido garantizado.
**Veredicto: descartada; el corte correcto es el contexto delimitado.**

### Alternativa C — Dos servicios (Cartera vs. Core bancario)
Pros: corte natural por perfil de carga (read-heavy vs. dinero); mitad de
costos y de superficie distribuida; la saga queda dentro del core.
Contras: el core sigue mezclando tres contextos; no cubre el objetivo de
aprendizaje; el día que Cuentas deba separarse, se re-hace la saga.
**Veredicto: descartada; punto de retorno razonable si el free tier aprieta.**

## Consecuencias de la propuesta

Gana: aprendizaje real de distributed systems sobre un dominio que lo justifica;
escala y despliega por servicio; APIs agent-ready para el chat MCP futuro;
observabilidad de negocio como sub-producto.
Cuesta: 4 pipelines y 8 posibles instancias (arrancar solo dev, spin-down
siempre); debugging distribuido (mitigado con OTel desde el día 1); la saga
del descuento es complejidad real y permanente (curriculum); CI más lenta;
disciplina de contratos para no derivar en acoplamiento.

## Decisión

Aceptada (2026-09-06): 4 microservicios por contexto delimitado. Cola:
**QStash** (Redis Streams queda documentado como alternativa de retorno si
se necesitan consumer groups). Observabilidad: **OpenTelemetry exportando a
Grafana Cloud free**. Arranque solo en dev; prod cuando haya producto.
