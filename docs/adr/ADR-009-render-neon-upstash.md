# ADR-009: Stack gratuito Render + Neon + Upstash

## Estado
Accepted

## Contexto
Presupuesto cero e indefinido, pero con exigencias reales: dos ambientes
(dev/prod), CI/CD por rama, Postgres relacional con migraciones, Redis para el
cache y HTTPS público (RFC-003). Ningún free tier integrado serio cumplía
todo sin tarjeta ni trial temporal.

## Decisión
Tres servicios administrados gratuitos, uno por responsabilidad: **Render**
(plan free, Docker desde el blueprint `render.yaml`, dos servicios con
auto-deploy por rama y health check `/health`), **Neon** (Postgres serverless
con pooler, un proyecto por ambiente, cadena en formato Npgsql) y **Upstash**
(Redis serverless regional, una base por ambiente; la app normaliza tanto
`rediss://` como formato `host:port,password=`). Secretos solo en el dashboard
de Render (`sync: false`), nunca en el repo.

## Consecuencias
Positivas: infra gratuita real con pipeline completo verificado (stress test
2026-09-05: 0 errores 5xx; el throughput escaló lineal hasta 69 RPS con c=50
sin degradar p50).
Negativas: cold starts (Render duerme tras 15 min: 30-60 s el primer request;
Neon ~0,5 s de wake-up); latencia dominada por red USA↔AR (~600 ms p50
medidos); límites del plan (Upstash 500k comandos/mes).
Hallazgos operativos del stress test: (1) cada `/health` golpea Neon y
Upstash ⇒ monitoreo externo agresivo consume quota; (2) sin
`X-Forwarded-For`, el rate limit es global (ver ADR-006). Aceptables para
dev/demo; el upgrade a pago queda como evolución documentada.
