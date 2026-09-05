# RFC-001: Estrategia de consultas por CUIT a escala

**Estado:** Accepted
**Fecha:** 2026-09-05
**Vinculado a:** [PRD HU-02](../PRD.md), [SPEC sección 7](../../Coelsa/docs/SPEC.md)

## Contexto

El endpoint `GET /api/v1/{tipo}?cuit={cuit}` es la consulta estrella del
producto: es lo que la Consola pide en cada login de cliente. En una cámara de
compensación real, la tabla de instrumentos crece sin cota y cada consulta por
CUIT toca un índice + un `COUNT(*)` para el paginado. Sin estrategia, la
experiencia de la cartera degrada con el volumen y el pico de logins (hora de
apertura) golpea la base con el mismo query miles de veces.

Requisitos relevantes: RNF-05 (cache Redis), RNF-11 (índices), RNF-12
(protección de picos), RNF-13 (degradación elegante).

## Propuesta

Ataque en dos capas:

1. **Capa base de datos (siempre):** índices parciales `WHERE activo = true`
   por `cuit_librador` y `cuit_beneficiario`, con columnas del listado como
   INCLUDE (covering index) para resolver la página sin tocar la heap.
   (Detallado en ADR-007/ADR-008.)
2. **Capa cache (aceleración):**
   - Clave: `coelsa:{tipo}:cuit:{cuit}:v{N}:p{page}:{pageSize}`.
   - La versión `N` por CUIT es un contador en Redis; **toda escritura** de un
     instrumento con ese CUIT (librador o beneficiario) incrementa la versión.
   - TTL 30 min con jitter ±10%.
   - El `totalCount` del paginado se cachea junto con la página.
   - Protección de stampede: lock breve por clave (`SETNX` + expiry); un solo
     request recalienta, el resto espera y lee la versión nueva.
   - Fail-open: Redis caído ⇒ consulta directa a PostgreSQL (la cache es
     optimización, no dependencia).
   - Las lecturas individuales por identificador **no se cachean** (van a la
     base, cubiertas por índice único).

## Alternativas consideradas

### A. Solo índices, sin cache
- Pros: cero complejidad nueva, cero problemas de coherencia.
- Contras: no resuelve el pico de logins; el `COUNT(*)` del paginado por página
  golpea la base siempre; en el plan de deploy elegido (ADR-009) la base es
  serverless con cómputo escaso. Queda corta para la meta de p50 < 50 ms.

### B. TTL corto con invalidación por borrado de claves (patrón clásico cache-aside)
- Pros: más simple de razonar; conocido.
- Contras: borrar claves por CUIT exige enumerarlas (`SCAN` por prefijo) o
  mantener un registro de claves; el `SCAN` en Redis es O(n) sobre el keyspace
  y NO es atómico con la escritura: hay ventana de inconsistencia donde una
  lectura recalienta con datos viejos. Además la página tiene `pageSize`
  variable ⇒ el set de claves a invalidar es combinatorio.

### C. Versionado por CUIT (elegida)
- Pros: la invalidación es O(1) (`INCR` de un hash), atómica y sin enumerar
  claves; las claves viejas muere solas por TTL (autolimpieza); sin ventana de
  inconsistencia porque la versión nueva apunta a claves nuevas; implementación
  pequeña (un `INCR` + incluir `v{N}` en la clave).
- Contras: las claves huérfanas (versiones viejas) ocupan memoria hasta expirar
  (mitigado con TTL 30 min); la versión vive en Redis ⇒ si Redis se pierde, se
  vuelve a versión 0 y se recalienta (aceptable: es cache, no verdad).

### D. CQRS con read-store dedicado
- Pros: la solución "seria" a escala de cámara real: proyección optimizada por
  CUIT, desacoplada de escrituras.
- Contras: duplica infraestructura y complejidad operativa; para un simulador
  v1 es sobreingeniería (el SPEC lo lista como evolución natural, no v1).

## Consecuencias de la propuesta

- Las lecturas de cartera calientes se sirven de Redis (~ms) y la base solo
  recalienta una vez por expiración.
- El costo de escritura crece marginalmente (un `INCR` por CUIT afectado).
- Memoria de Redis proporcional a claves vivas por CUIT activo × páginas
  consultadas (acotado por TTL).
- Dependemos de la coherencia de la versión ⇒ todo caso de uso que escriba
  DEBE incrementar (inválido en tests: crear/eliminar invalidan AMBOS cuits
  del instrumento).

## Decisión

**Accepted**: alternativa C (versionado por CUIT) sobre la capa de índices (1),
con fail-open y stampede protection. CQRS queda documentado como evolución
(SPEC sección 10). Las decisiones congeladas derivadas: ADR-004 (versionado),
ADR-005 (fail-open), ADR-008 (índices).
