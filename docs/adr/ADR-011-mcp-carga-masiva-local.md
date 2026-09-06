# ADR-011: Herramienta de carga masiva como MCP local (stdio)

## Estado
Accepted

## Contexto
La carga masiva de instrumentos de prueba existía como script CLI operativo
(`Coelsa/tools/carga-masiva/cargador.mjs`), invocable solo desde una terminal.
Queríamos dispararla desde asistentes de código (opencode, Claude, Cursor) en
lenguaje natural ("cargá 50 echeqs"). La alternativa remota exigía transporte
Streamable HTTP + auth propia en Render free (cold start ~1 min, endpoint
público a proteger); Neon y Upstash no ejecutan procesos (ver ADR-009).

## Decisión
Exponemos el núcleo del cargador (ya modular, funciones exportadas) como
servidor MCP **stdio local** (`server.mjs`, SDK oficial `@modelcontextprotocol`)
sin duplicar lógica. Las corridas son **incrementales y parametrizables**
(`cantidad`, `concurrencia`, `maxSegundos`, `conEstados`), con CMC7
deterministas por índice para reanudar sin duplicar. El progreso sigue en
`progreso.json` local (ignorado por git) y el registro del server en
`opencode.json` del repo usa `cwd` relativo (portable).

## Consecuencias
Positivas: cero infra extra (no suma servicios en Render), sin superficie
pública ni auth que mantener, misma lógica exacta que el CLI.
Negativas: exige Node ≥ 18 y `npm install` por máquina; el contador de progreso
es local, por lo que dos máquinas con archivos divergentes generan rangos
solapados (mitigado: el 409 por CMC7 duplicado se cuenta como "duplicado" y no
corrompe datos). Si el uso pasa a ser concurrente, mover el contador a
Upstash/Neon es la evolución natural.
