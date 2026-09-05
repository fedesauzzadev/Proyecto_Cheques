# PRD: Simulador COELSA y Consola de Instrumentos

**Versión:** 1.0
**Estado:** Accepted
**Fecha:** 2026-09-05
**Owner:** Equipo producto
**Docs hijos:** [Coelsa SPEC](../Coelsa/docs/SPEC.md) · [Front SPEC](../Front_Coelsa/docs/SPEC.md) · [RFCs](rfc/) · [ADRs](adr/)

> El PRD describe **qué** problema resolvemos y **por qué**; el cómo vive en los
> RFCs, ADRs y SPECs. Si un detalle técnico aparece acá, está mal ubicado.

---

## 1. Problema

Vamos a construir un **banco nuevo** cuya propuesta central es la autogestión
web para el **descuento de cheques y echeqs**: el cliente carga sus instrumentos,
ve su cartera y negocia su adelanto sin pasar por un mostrador.

Para construir eso necesitamos una **fuente de verdad de los instrumentos
negociables** (cheques físicos y echeqs): quién libró cada uno, a nombre de
quién, por cuánto, en qué estado está y si fue rechazado. En la vida real ese
rol lo cumple COELSA, la cámara electrónica de compensación. No podemos
integrarnos a la real, así que **no existe** ese componente en nuestro
ecosistema: es la primera pieza que falta.

## 2. Objetivos

### Objetivo de producto
Construir un **simulador del rol de COELSA** que exponga, vía API, el ciclo de
vida completo de cheques físicos y echeqs, y una **Consola de Instrumentos**
web para operar sobre esa API, validando de punta a punta la experiencia de
consulta y gestión de cartera que tendrá el banco.

### Objetivos de aprendizaje (este proyecto es también collateral de otro)
- Practicar arquitectura hexagonal, patrones (Strategy, Repository, puertos y
  adaptadores) y diseño para volumen sobre un dominio real argentino.
- Practicar operación real en la nube con pipeline de CI/CD y dos ambientes.

### No objetivos (v1)
- No reemplazamos ni imitamos protocolos reales de COELSA/BCRA.
- No hacemos autenticación, firmas digitales, endosos, compensación entre
  bancos, conciliación ni liquidación (ver sección 7).
- No construimos aún el servicio "banco" ni el motor de descuento.

## 3. Usuarios (personas)

| Persona | Necesidad | Cómo la resolvemos |
|---|---|---|
| **Cliente del banco** (pyme o persona con cheques de terceros) | "Quiero ver qué cheques tengo, de quién, por cuánto y cuándo cobro" | Consulta por CUIT: le devuelve lo emitido a su nombre y lo que él libró, separado por tipo, paginado |
| **Operador de caja / back office** | "Necesito registrar instrumentos, actualizar su estado y dar bajas" | Creación con protección contra doble carga (idempotencia), cambio de estado gobernado, baja lógica |
| **Equipo de desarrollo del ecosistema** (servicio "banco" futuro) | "Necesitamos integrarnos por API con contratos estables y errores predecibles" | REST versionada, OpenAPI design-first, ProblemDetails en español, idempotencia en creaciones |
| **Equipo de operación (nosotros)** | "Tiene que estar desplegado, monitoreable y con dos ambientes" | Health checks, CI/CD, despliegue azul/verde simple (dev/prod) |

## 4. Historias de usuario y criterios de aceptación

### HU-01 — Registrar un instrumento
> Como operador quiero dar de alta un cheque físico (por CMC7) o un echeq
> (por CUD) para que forme parte de la cartera consultable.

- El sistema valida CMC7 (30 dígitos, único) y CUD (SHA-256 hex-64, único).
- El IDECHEQ de los echeqs **lo asigna el simulador** (no lo elige el cliente).
- Si reenvío el mismo alta (misma `Idempotency-Key` y mismo body) no se duplica:
  recibo el recurso original.
- Trazabilidad: RF-01/RF-02 del SPEC backend, RF-F03/RF-F04 del SPEC front.

### HU-02 — Ver mi cartera por CUIT
> Como cliente quiero listar los instrumentos donde aparezco como librador **o**
> beneficiario, para conocer mi posición.

- Un solo `cuit` de entrada cubre ambos roles.
- Cheques y echeqs **nunca mezclados**: son consultas separadas.
- Paginado 1-based con envolvente (`totalCount`, `totalPages`).
- Rápido incluso con carteras grandes (índices + cache; ver RFC-001).
- Trazabilidad: RF-03, RF-F01.

### HU-03 — Ver el detalle de un instrumento
> Como cliente quiero buscar un cheque por su CMC7 o un echeq por su IDECHEQ.

- Identificadores **de negocio** en la URL, no GUIDs internos.
- `200` con el detalle completo (incluye desglose del CMC7) o `404`.
- Trazabilidad: RF-04, RF-F02.

### HU-04 — Actualizar el estado
> Como operador quiero reflejar el circuito real: depositado, compensado,
> pagado, rechazado o anulado.

- Solo transiciones válidas de la máquina de estados; el resto es `422`.
- `Rechazado` exige código de motivo (falta de fondos, defecto formal, etc.).
- Trazabilidad: RF-05, RF-F05.

### HU-05 — Dar de baja sin perder historia
> Como operador quiero eliminar un instrumento cargado por error sin romper
> auditoría.

- La baja es lógica: el registro deja de exponerse pero no se borra.
- Trazabilidad: RF-06, RF-F06.

### HU-06 — Confianza operativa
> Como equipo del ecosistema quiero saber si la API está viva y lista, y que
> un pico de tráfico no la tumbe.

- `/health` verifica API + PostgreSQL + Redis.
- Rate limiting en consultas y creaciones; al exceder, `429` con `Retry-After`.
- Si el cache falla, se responde desde la base (degradación elegante).
- Trazabilidad: RF-07/RF-08, RNF-12/13, RF-F07/RF-F08.

## 5. Métricas de éxito

### De producto
| Métrica | Meta v1 |
|---|---|
| Cobertura del ciclo de vida del instrumento | 100% de estados y transiciones operables desde la Consola |
| Cero duplicación de instrumentos por reintentos | 100% de reenvíos idempotentes resueltos como replay |

### Técnicas (del SPEC, RNF)
| Métrica | Meta |
|---|---|
| Latencia p50 de listados cacheados | < 50 ms de procesamiento en el origen |
| Errores 5xx | < 0,1% de las respuestas |
| Cobertura de tests del dominio (CUIT, estados, idempotencia, fail-open) | Suite verde en CI obligatoria para mergear |

> Validación empírica: el stress test del 2026-09-05 sobre producción arrojó
> 0 errores 5xx en ~3.500 requests y 1.830 rechazos controlados por rate
> limit (429), con el servicio saludable al finalizar.

## 6. Alcance v1 (in/out)

**Dentro:** creación (cheque físico + echeq), consulta por CUIT paginada y
cacheada, consulta individual, cambio de estado con máquina de estados, baja
lógica, health checks, rate limiting, seed de datos demo, Consola web de
consulta/gestión, CI/CD con ambientes dev y prod.

**Fuera (sección 10 del SPEC):** firmas digitales reales, endosos y
particiones, autenticación/autorización, compensación entre bancos,
conciliación/liquidación, escalado horizontal. El escalado queda documentado
como evolución; v1 aplica los patrones que lo preparan (índices, cache,
rate limit).

## 7. Riesgos y mitigaciones

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Consultas por CUIT lentas a volumen (problema real de una cámara) | Alto | RFC-001 (cache versionado) + índices parciales/covering (ADR-008) |
| Doble carga de instrumentos por reintentos de red | Alto (dinero ficticio, confianza real) | Idempotencia persistida (RFC-002/ADR-003) |
| Dependencia dura del cache | Alto | Fail-open a PostgreSQL (ADR-005) |
| Abuso/pico de tráfico en servicio público | Medio | Rate limiting (ADR-006) |
| Costo de infra durante el aprendizaje | Medio (presupuesto cero) | Stack gratuito (RFC-003/ADR-009) |

## 8. Trazabilidad PRD ↔ documentación técnica

| Historia | RFCs relevantes | ADRs relevantes |
|---|---|---|
| HU-01 | RFC-002 | ADR-002, ADR-003, ADR-010 |
| HU-02 | RFC-001 | ADR-004, ADR-005, ADR-007, ADR-008 |
| HU-03 | — | ADR-002 |
| HU-04 | — | ADR-001 (hexagonal: la máquina vive en Domain) |
| HU-05 | — | ADR-007, ADR-008 |
| HU-06 | RFC-003 | ADR-005, ADR-006, ADR-009 |
