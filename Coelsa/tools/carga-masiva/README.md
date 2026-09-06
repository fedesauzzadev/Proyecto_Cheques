# Carga masiva (CLI + MCP)

Herramienta operativa para poblar la API COELSA con instrumentos de prueba:
cheques físicos y echeqs con CMC7 únicos y deterministas por índice, fechas
válidas según reglas de negocio (tenor ≤ 365 días), y avance opcional de
estados (≈80% queda en estado inicial). Requiere Node ≥ 18.

## MCP (uso desde un asistente)

Servidor stdio (`server.mjs`) con cuatro herramientas:

| Herramienta | Qué hace |
|---|---|
| `cargar_cheques` / `cargar_echeqs` | Crean N instrumentos **nuevos** (incremental) + reintentos pendientes |
| `progreso` | Muestra totales acumulados e índices fallidos |
| `sonda_rate_limit` | Ráfaga de POSTs inválidos (no crea datos) para verificar el límite |

Parámetros de carga: `cantidad` (default 100, semántica incremental: sigue
desde el último índice registrado), `concurrencia` (10), `maxSegundos` (120,
si vence se reanuda en la próxima corrida), `conEstados` (true), `api`
(default `COELSA_API_URL` o `https://coelsa-api-dev.onrender.com`).

El progreso vive en `progreso.json` (local por máquina, ignorado por git):
re-ejecutar una carga reintenta los fallidos y jamás duplica CMC7s.

### Registro en clientes

Ya registrado para opencode en `opencode.json` de la raíz del repo (portable,
vía `cwd`). Solo falta `npm install` en esta carpeta y reiniciar el cliente.
Para otros clientes, apuntar el command a `server.mjs` de esta carpeta:

```json
{ "mcpServers": { "coelsa-carga": {
    "command": "node",
    "args": ["<repo>/Coelsa/tools/carga-masiva/server.mjs"],
    "env": { "COELSA_API_URL": "https://coelsa-api-dev.onrender.com" }
} } }
```

## CLI (uso directo)

```bash
node cargador.mjs [--api URL] [--cheques N] [--echeqs M] [--desde-cq X]
                  [--desde-eq Y] [--max-segundos S] [--conc N] [--sonda]
```

A diferencia del MCP (incremental), el CLI usa metas absolutas: carga hasta
alcanzar `--cheques`/`--echeqs` totales. `--sonda` verifica el rate limit sin
crear datos.

## Decisión de diseño

MCP local stdio (no remoto): ver [ADR-011](../../../docs/adr/ADR-011-mcp-carga-masiva-local.md).
