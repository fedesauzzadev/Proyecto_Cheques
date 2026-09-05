# ADR-005: Degradación fail-open ante caída de Redis

## Estado
Accepted

## Contexto
Redis es nuestra capa de aceleración (RNF-05), no la fuente de verdad. Si una
caída del cache (o de Upstash en el plan gratuito) tumbara las consultas, la
disponibilidad del servicio quedaría atada a su componente menos confiable
(RNF-13).

## Decisión
`GestorCacheRedis` opera en fail-open: si Redis no responde, la consulta va
directo a PostgreSQL (cubierto por índices, ADR-008) y el error se loguea como
warning. El health check usa `abortConnect=false` para no marcar `/health`
como caído por Redis. Tests `GestorCacheRedisFailOpenTests` verifican: lectura
devuelve cero, carga directo de la fuente e invalidación que no lanza.

## Consecuencias
Positivas: la cache es optimización pura, nunca dependencia dura; degradación
elegante automática sin intervención.
Negativas: ante caída de Redis, la latencia sube (full path a Neon) y la base
absorbe todo el tráfico; un operador puede no notar que el cache está caído si
solo mira disponibilidad (conviene alertar sobre el warning).
Riesgos asumidos: picos sin cache golpean más a Neon; mitigado por el rate
limit (ADR-006).
