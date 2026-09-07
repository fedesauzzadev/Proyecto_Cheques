#!/usr/bin/env node
// Servidor MCP (stdio) que expone la carga masiva de cheques/echeqs contra la API COELSA.
//
// Herramientas expuestas:
//   - cargar_cheques / cargar_echeqs: crean N instrumentos NUEVOS (la cantidad es
//     parámetro; semántica incremental sobre progreso.json, sin duplicar CMC7).
//   - progreso: muestra totales acumulados e índices fallidos pendientes de reintento.
//   - sonda_rate_limit: ráfaga de POSTs inválidos (no crea datos) para verificar el límite.
//
// Ejemplo de registro en opencode (opencode.json de la raíz del repo, portable):
//   { "mcp": { "coelsa-carga": {
//       "type": "local", "command": ["node", "server.mjs"],
//       "cwd": "Coelsa/tools/carga-masiva", "enabled": true
//   } } }
// En Claude Desktop / Cursor, apuntar "command"/"args" a la ruta de este archivo
// (<repo>/Coelsa/tools/carga-masiva/server.mjs).
//
// OJO: nada de console.log; stdout queda reservado para el protocolo MCP (solo stderr).
import { z } from 'zod';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { cargarLote, guardarProgreso, leerProgreso, sonda } from './cargador.mjs';

const API_DEFAULT = process.env.COELSA_API_URL ?? 'https://coelsa-api-dev.onrender.com';
const CLAVES = {
  cheques: { total: 'cheques', reintentos: 'reintentosCq' },
  echeqs: { total: 'echeqs', reintentos: 'reintentosEq' },
};

/** Corre una tanda incremental: `cantidad` instrumentos nuevos + reintentos pendientes. */
async function cargarCantidad({ api = API_DEFAULT, tipo, cantidad, desde, concurrencia, maxSegundos, conEstados }) {
  const claves = CLAVES[tipo];
  const progreso = leerProgreso();
  const base = progreso[claves.total];
  const nuevos = Array.from({ length: cantidad }, (_, k) => desde + base + k);
  const pendientes = [...new Set([...progreso[claves.reintentos], ...nuevos])].sort((a, b) => a - b);

  const fin = Date.now() + maxSegundos * 1000;
  const r = await cargarLote({
    api,
    tipo,
    indices: pendientes,
    conEstados,
    conc: concurrencia,
    hoy: new Date(),
    debeParar: () => Date.now() > fin,
  });

  progreso[claves.total] = base + r.ok + r.duplicados;
  progreso[claves.reintentos] = r.reintentos;
  guardarProgreso(progreso);
  return {
    ok: r.ok,
    duplicados: r.duplicados,
    fallos: r.fallos,
    reintentosPendientes: r.reintentos.length,
    totalAcumulado: progreso[claves.total],
    api,
  };
}

const esquemaCarga = {
  cantidad: z
    .number()
    .int()
    .min(1)
    .max(20000)
    .default(100)
    .describe('Instrumentos nuevos a crear en esta corrida (incremental, no un total absoluto)'),
  api: z.string().url().optional().describe(`Base de la API (default: variable COELSA_API_URL o ${API_DEFAULT})`),
  desde: z
    .number()
    .int()
    .min(0)
    .default(0)
    .describe('Desplazamiento del índice base de los CMC7; dejar en 0 salvo que se necesite un rango aparte'),
  concurrencia: z.number().int().min(1).max(50).default(10).describe('Pedidos simultáneos contra la API'),
  maxSegundos: z
    .number()
    .int()
    .min(10)
    .max(600)
    .default(120)
    .describe('Presupuesto de tiempo; si vence, lo creado queda registrado y se continúa en la próxima corrida'),
  conEstados: z
    .boolean()
    .default(true)
    .describe('Avanzar también estados aleatorios (≈20% recibe depositado/compensado/rechazado/anulado; el resto queda en estado inicial)'),
};

function resumen(r, tipo) {
  return JSON.stringify({ tipo, ...r });
}

const server = new McpServer({ name: 'coelsa-carga-masiva', version: '1.1.0' });

server.tool(
  'cargar_cheques',
  'Crea N cheques físicos de prueba contra la API COELSA (CMC7 únicos, reanudable, sin duplicados).',
  esquemaCarga,
  async (p) => {
    try {
      return { content: [{ type: 'text', text: resumen(await cargarCantidad({ ...p, tipo: 'cheques' }), 'cheques') }] };
    } catch (e) {
      return { content: [{ type: 'text', text: `FALLO: ${e.message}` }], isError: true };
    }
  },
);

server.tool(
  'cargar_echeqs',
  'Crea N echeqs de prueba contra la API COELSA (crea cuentas demo + chequeras con capacidad; CMC7 e IDECHEQ autogenerados, reanudable).',
  esquemaCarga,
  async (p) => {
    try {
      return { content: [{ type: 'text', text: resumen(await cargarCantidad({ ...p, tipo: 'echeqs' }), 'echeqs') }] };
    } catch (e) {
      return { content: [{ type: 'text', text: `FALLO: ${e.message}` }], isError: true };
    }
  },
);

server.tool(
  'progreso',
  'Muestra el progreso acumulado de carga (totales por tipo e índices fallidos pendientes de reintento).',
  async () => ({ content: [{ type: 'text', text: JSON.stringify(leerProgreso()) }] }),
);

server.tool(
  'sonda_rate_limit',
  'Ráfaga de POSTs inválidos (no crea datos) para verificar el rate limit de la API.',
  { api: z.string().url().optional().describe(`Base de la API (default: variable COELSA_API_URL o ${API_DEFAULT})`) },
  async ({ api }) => {
    try {
      return { content: [{ type: 'text', text: JSON.stringify(await sonda(api ?? API_DEFAULT)) }] };
    } catch (e) {
      return { content: [{ type: 'text', text: `FALLO: ${e.message}` }], isError: true };
    }
  },
);

await server.connect(new StdioServerTransport());
console.error(`[coelsa-carga] servidor MCP listo (stdio). API: ${API_DEFAULT}`);
