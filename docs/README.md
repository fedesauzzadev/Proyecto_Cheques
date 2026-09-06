# Documentación del proyecto

Este directorio contiene la documentación **transversal** del producto y de las
decisiones de diseño. La documentación **por servicio** vive en cada subsistema:

| Directorio | Contenido |
|---|---|
| `docs/` (acá) | PRD, RFCs, ADRs: producto, propuestas de diseño y decisiones |
| `Coelsa/docs/SPEC.md` | Especificación de requisitos del backend (RF/RNF, dominio, contrato) |
| `Coelsa/docs/openapi.yaml` | Contrato formal de la API (fuente de verdad) |
| `Coelsa/docs/DEPLOY.md` | Runbook de despliegue (Render + Neon + Upstash) |
| `Coelsa/tools/carga-masiva/README.md` | Herramienta de carga masiva de prueba (CLI + MCP) |
| `Front_Coelsa/docs/SPEC.md` | Especificación del front (Consola de Instrumentos) |

## El pipeline documental

```
PRD ─────────▶ RFC ─────────▶ ADR ─────────▶ Código + SPEC
(QUÉ y POR QUÉ)  (CÓMO: propuesta   (POR QUÉ se decidió   (implementación y
                  con alternativas   X y no Y, congelado)  requisitos detallados)
                  y revisión)
```

| Doc | Cuándo se escribe | Vigencia | Regla de oro |
|---|---|---|---|
| **PRD** | Antes de empezar, durante el discovery | Vivo: se actualiza cuando cambia el alcance | Describe el problema y el resultado, nunca la solución técnica |
| **RFC** | Antes de implementar algo significativo | Se congela al resolverse (accepted/rejected); no se edita después | Debe mostrar las alternativas descartadas, no solo la elegida |
| **ADR** | En el momento en que se toma la decisión | Inmutable: si cambia el contexto, se escribe un ADR nuevo que lo supersede | Uno por decisión, corto (media página); vale más 10 cortos que 1 largo |

## Índice

### PRD
- [PRD — Simulador COELSA y Consola de Instrumentos](PRD.md)
- [PRD — Banco IrmaRios: banca de cartera para empresas](PRD-banco-irmarios.md)

### RFCs (`docs/rfc/`)
| RFC | Título | Estado |
|---|---|---|
| [RFC-001](rfc/RFC-001-cache-consultas-por-cuit.md) | Estrategia de consultas por CUIT a escala | Accepted |
| [RFC-002](rfc/RFC-002-idempotencia-en-creaciones.md) | Idempotencia en creaciones (POST) | Accepted |
| [RFC-003](rfc/RFC-003-infraestructura-gratuita.md) | Infraestructura de despliegue sin costo | Accepted |

### ADRs (`docs/adr/`)
| ADR | Decisión | Estado |
|---|---|---|
| [ADR-001](adr/ADR-001-arquitectura-hexagonal.md) | Arquitectura hexagonal en 4 proyectos | Accepted |
| [ADR-002](adr/ADR-002-identificadores-de-negocio.md) | Exponer identificadores de negocio, no GUIDs internos | Accepted |
| [ADR-003](adr/ADR-003-idempotencia-en-postgresql.md) | Idempotencia persistida en PostgreSQL | Accepted |
| [ADR-004](adr/ADR-004-cache-versionado-por-cuit.md) | Cache con invalidación por versión de CUIT | Accepted |
| [ADR-005](adr/ADR-005-fail-open-redis.md) | Degradación fail-open ante caída de Redis | Accepted |
| [ADR-006](adr/ADR-006-rate-limiting-token-bucket.md) | Rate limiting con token bucket y políticas separadas | Accepted |
| [ADR-007](adr/ADR-007-soft-delete.md) | Baja lógica (soft delete) con índices parciales | Accepted |
| [ADR-008](adr/ADR-008-indices-parciales-covering.md) | Índices parciales + covering para listados | Accepted |
| [ADR-009](adr/ADR-009-render-neon-upstash.md) | Stack gratuito Render + Neon + Upstash | Accepted |
| [ADR-010](adr/ADR-010-strategy-y-lenguaje-ubiquo.md) | Patrón Strategy en creaciones + lenguaje ubicuo en español | Accepted |
| [ADR-011](adr/ADR-011-mcp-carga-masiva-local.md) | Carga masiva expuesta como MCP local (stdio) | Accepted |

## Plantillas

### Plantilla de RFC
```markdown
# RFC-NNN: Título
**Estado:** draft | accepted | rejected | superseded por RFC-MMM
**Fecha:** YYYY-MM-DD

## Contexto
(Qué problema motiva esta propuesta. Vincular al PRD/SPEC.)

## Propuesta
(La solución recomendada, con suficiente detalle para poder criticarla.)

## Alternativas consideradas
### Alternativa A — nombre
Pros / Contras
### Alternativa B — nombre
Pros / Contras

## Consecuencias de la propuesta
(Qué gana y qué cuesta: complejidad, deuda, riesgos, límites.)

## Decisión
(Veredicto y racional. Si fue rejected, por qué.)
```

### Plantilla de ADR (formato Nygard)
```markdown
# ADR-NNN: Título de la decisión

## Estado
Accepted | Superseded por ADR-MMM | Deprecated

## Contexto
(El forcing function: qué situación nos obligó a decidir HOY.)

## Decisión
(La decisión en 1-3 frases, en voz activa: "Usamos X para Y".)

## Consecuencias
Positivas: ...
Negativas: ...
Riesgos mitigados / asumidos: ...
```
