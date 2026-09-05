# ADR-003: Idempotencia persistida en PostgreSQL

## Estado
Accepted

## Contexto
Reintentar un POST de creación no debe duplicar instrumentos (RFC-002). La
discusión era dónde guardar la `Idempotency-Key`: Redis (efímero) o
PostgreSQL (durable). Nuestra capa de cache tiene semántica fail-open
(ADR-005), es decir, puede faltar: apoyarse en ella para una invariante de
negocio contradice el diseño.

## Decisión
Persistimos `Idempotency-Key` + resultado en la tabla `idempotency_keys` de
PostgreSQL con índice único en `key`. Semántica: key nueva → `201`; misma key
+ mismo body → `200` + `Idempotent-Replay: true`; misma key + body distinto →
`409`; unicidad de negocio violada (CMC7/CUD duplicado) → `409`.

## Consecuencias
Positivas: la garantía de no-duplicación sobrevive reinicios, deploys y caídas
de Redis; el replay (`200` + header) permite al cliente distinguir "ya lo
cargué yo" de "lo cargó otro".
Negativas: una tabla y una escritura extra por creación; comparación de body
(hash del request normalizado) en cada POST con key repetida.
Riesgos mitigados: los tests `EstrategiasCreacionTests` cubren
persiste-e-invalida y replays; la pregunta "¿esta operación ya ocurrió?" tiene
la misma durabilidad que el propio instrumento.
