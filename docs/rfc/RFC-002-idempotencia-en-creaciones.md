# RFC-002: Idempotencia en creaciones (POST)

**Estado:** Accepted
**Fecha:** 2026-09-05
**Vinculado a:** [PRD HU-01](../PRD.md), [SPEC RF-02](../../Coelsa/docs/SPEC.md)

## Contexto

Los `POST /api/v1/cheques` y `/POST /api/v1/echeqs` crean instrumentos
financieros. HTTP no es confiable por sí solo: timeouts, reintentos de
proxies, doble click del usuario o reintentos automáticos del front pueden
duplicar una creación. Un cheque duplicado no es un bug cosmético: rompe la
cartera y la confianza en el sistema. Necesitamos que **reintentar sea
seguro** (misma intención ⇒ mismo resultado, sin duplicar).

## Propuesta

Idempotencia estilo Stripe:

- Header **obligatorio** `Idempotency-Key` (GUID) en ambos POST; sin él ⇒ `400`.
- La key se **persiste** junto al identificador creado.
- Semántica:
  - Key nueva + datos válidos ⇒ `201 Created` (primera vez).
  - Key existente + **mismo body** ⇒ `200 OK` con el recurso original + header
    `Idempotent-Replay: true` (replay transparente).
  - Key existente + **body distinto** ⇒ `409 Conflict` (la key se usa para otra
    intención: probablemente un bug del cliente).
  - Key distinta + unicidad de negocio violada (CMC7/CUD ya existente) ⇒
    `409 Conflict`.

## Alternativas consideradas

### A. No hacer nada ("el cliente no reintenta")
- Pros: cero código.
- Contras: falsa premisa; los reintentos ocurren siempre en producción. Es la
  receta para cheques duplicados.

### B. Idempotencia natural por unicidad de negocio (confiar solo en el índice único de CMC7/CUD)
- Pros: la BD ya lo garantiza; sin tabla nueva.
- Contras: el reintento llega con otro CMC7 (ej: regenerado por el emisor) o,
  peor, con el mismo y recibimos `409` en vez del recurso: el cliente no puede
  distinguir "ya lo cargué yo" de "lo cargó otro". El usuario no sabe si su
  operación exitó.

### C. Guardar la key en Redis con TTL (efímera)
- Pros: barata y rápida; patrón común para idempotencia de corta vida.
- Contras: al expirar o evictar, un reintento tardío **duplica**; una cámara
  debe resolver la pregunta para siempre, no por 24 h. Redis además es nuestra
  capa de optimización con fail-open (ADR-005): apoyar una **invariante de
  negocio** en una dependencia descartable contradice el diseño.

### D. Persistir key + resultado en PostgreSQL (elegida)
- Pros: sobrevive reinicios y deploys (la verdad de "¿esta operación ya
  ocurrió?" es tan durable como el propio instrumento); replay correcto con
  `200` + header; índice único en `key`; la comparación de body detecta abuso
  de key.
- Contras: una tabla más y una escritura extra por creación; hay que comparar
  el body contra lo persistido (hash del request normalizado).

## Consecuencias de la propuesta

- El contrato de la API exige a los clientes generar y **reusar** la key por
  intención (documentado en OpenAPI y Consola).
- El caso "mismo CMC7, key distinta" sigue siendo `409` de negocio: la
  idempotencia protege reintentos, no legitima duplicar.
- La tabla `idempotency_keys` crece con las creaciones (aceptable: una fila
  por creación real).

## Decisión

**Accepted**: alternativa D, con la semántica exacta de RFC-002. Decisión
congelada en [ADR-003](../adr/ADR-003-idempotencia-en-postgresql.md). El front
implementa el mismo patrón con persistencia local (RF-F04 del SPEC front).
