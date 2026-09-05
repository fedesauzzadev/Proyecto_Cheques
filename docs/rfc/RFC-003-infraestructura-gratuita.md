# RFC-003: Infraestructura de despliegue sin costo

**Estado:** Accepted
**Fecha:** 2026-09-05
**Vinculado a:** [PRD sección 2 y riesgo "costo"](../PRD.md), [DEPLOY.md](../../Coelsa/docs/DEPLOY.md)

## Contexto

El proyecto es un entorno de aprendizaje con **presupuesto cero**: no puede
haber costo fijo mensual. A la vez exige realidad operativa: dos ambientes
(dev/prod), CI/CD con deploy automático por rama, base de datos relacional con
migraciones, Redis para el cache (RFC-001) y HTTPS público con health checks.

Restricciones: monorepo (backend .NET 8 en `Coelsa/`, front estático en
`Front_Coelsa/`), Dockerfile multi-stage ya existente, ramas `develop`→dev y
`main`→prod.

## Propuesta

Tres servicios administrados gratuitos, uno por responsabilidad:

| Responsabilidad | Servicio | Modo |
|---|---|---|
| Compute (API .NET + front estático) | **Render** (plan free) | Contenedor Docker desde el blueprint `render.yaml`, dos servicios (dev/prod), health check `/health` |
| Base PostgreSQL | **Neon** (plan free) | Serverless con pooler; un proyecto por ambiente |
| Redis | **Upstash** (plan free) | Serverless regional; una base por ambiente |

El pipeline queda: push a `develop`/`main` → GitHub Actions (build + tests) →
Render redeploy automático del servicio correspondiente. Secretos solo en el
dashboard de Render (`sync: false`).

## Alternativas consideradas

### A. Todo on-premise / máquina local
- Pros: costo cero real, control total.
- Contras: no es operable 24/7, no hay HTTPS público ni CI/CD real; mata el
  objetivo de aprendizaje de operación.

### B. Una sola plataforma PaaS integrada (ej. Railway/Fly.io todo-en-uno)
- Pros: una sola cuenta, UX simple.
- Contras: el free tier de las integradas actuales es trial temporal o con
  restricciones duras (no indefinido); menos control granular por servicio.

### C. Free tiers separados por especialidad (elegida)
- Pros: gratuito **indefinido** en los tres; cada pieza es la mejor de su
  categoría en free tier (Neon: Postgres serverless real con pooler; Upstash:
  Redis serverless con TLS; Render: Docker nativo + blueprint IaC + static
  sites); aísla responsabilidades y enseña cómo se integra de verdad un stack.
- Contras: tres cuentas/dashboards; latencias entre regiones; límites del plan
  (Render duerme tras 15 min, Neon suspende cómputo, Upstash 500k
  comandos/mes); coordinar upgrades futuros entre tres proveedores.

### D. Serverless puro (Lambda/Cloud Run + Aurora Serverless)
- Pros: escala a cero real, sin cold start de contenedor dormido.
- Contras: los free tiers serios requieren tarjeta y riesgo de cobro; más
  complejidad de empaquetado (.NET en funciones); fuera del espíritu didáctico
  de "un contenedor que corre como en prod".

## Consecuencias de la propuesta

- Infra gratuita real con pipeline completo funcionando (verificado con
  stress test 2026-09-05: 0 errores 5xx, rate limit operando).
- Cold starts: ~30-60 s el primer request tras inactividad (Render duerme) y
  ~0,5 s de wake-up de Neon. Aceptable para dev/demo; no lo sería para prod
  real (motivo de upgrade documentado).
- Los servicios compartidos no deben abusarse: el `/health` golpea Neon y
  Upstash en cada llamada ⇒ el monitoreo externo agresivo consume quota de
  Upstash (hallazgo del stress test).
- Riesgo de concentración de IP: sin `X-Forwarded-For` procesado, el rate
  limit keyea por la IP del proxy de Render (hallazgo del stress test; ver
  ADR-006).

## Decisión

**Accepted**: alternativa C. Detalle operativo en
[DEPLOY.md](../../Coelsa/docs/DEPLOY.md); decisión congelada en
[ADR-009](../adr/ADR-009-render-neon-upstash.md).
