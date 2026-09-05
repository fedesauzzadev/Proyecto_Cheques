# ADR-008: Índices parciales + covering para listados

## Estado
Accepted

## Contexto
Sin índices, cada consulta por CUIT es un full table scan; el problema real de
una cámara son las consultas sobre volúmenes masivos (SPEC 5.5). Además, el
`COUNT(*)` del paginado por página es una fuente clásica de presión sobre la
base (RNF-11).

## Decisión
Índices compuestos parciales `WHERE activo = true` sobre
`(cuit_librador)` y `(cuit_beneficiario)` en ambas tablas, con las columnas
del listado como INCLUDE (covering: la página se resuelve sin tocar la heap),
más índices únicos en `cmc7`, `id_echeq`, `cud` e `idempotency_keys.key`.
El `COUNT(*)` no se recalcula por query: viaja cacheado junto con la página
(ADR-004). Migraciones EF Core aplicadas al arranque.

## Consecuencias
Positivas: lecturas por CUIT y por identificador resueltas por índice; costo
de escritura acotado a mantener índices pequeños (solo filas vigentes).
Negativas: los INCLUDE duplican datos en el índice (espacio vs velocidad,
trade-off consciente); el plan de índices debe revisarse si cambian los campos
del listado.
Riesgos mitigados: es la capa que sostiene el fail-open (ADR-005): sin cache,
la base responde con índices en vez de scans.
