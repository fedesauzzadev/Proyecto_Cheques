#!/usr/bin/env node
// Carga masiva de instrumentos contra la API COELSA (operativa puntual).
//
// Uso:
//   node cargador.mjs [--api URL] [--cheques N] [--echeqs M]
//                     [--desde-cq X] [--desde-eq Y]
//                     [--max-segundos S] [--conc N] [--sonda]
//
// - Genera CMC7 únicos y deterministas por índice: reanuda sin duplicar
//   (guarda progreso en progreso.json, mismo directorio).
// - Respeta la regla de negocio: vencimiento > emisión/diferimiento y
//   tenor (vencimiento − emisión) ≤ 365 días.
// - 80% queda en estado inicial (cheque Emitido / echeq Pendiente).
// - Reintenta 429 respetando Retry-After y 5xx/red con backoff.
//
// Núcleo importable por server.mjs (MCP): ver funciones exportadas abajo.
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { randomUUID } from 'node:crypto';
import { fileURLToPath } from 'node:url';

const ARCHIVO_PROGRESO = new URL('./progreso.json', import.meta.url);

const CUITS = ['20123456786', '27876543219', '30511222334', '20334455662'];
const MOTIVOS = [11, 12, 21, 25];

export function leerArgs(argv = process.argv.slice(2)) {
  const args = {};
  for (let i = 0; i < argv.length; i++) {
    if (!argv[i].startsWith('--')) continue;
    const clave = argv[i].slice(2);
    const valor = argv[i + 1] && !argv[i + 1].startsWith('--') ? argv[++i] : true;
    args[clave] = valor === true ? true : Number.isNaN(Number(valor)) ? valor : Number(valor);
  }
  return args;
}

export function dormir(ms) {
  return new Promise((r) => setTimeout(r, ms));
}

export function randInt(min, max) {
  return min + Math.floor(Math.random() * (max - min + 1));
}

export function isoFecha(d) {
  return d.toISOString().slice(0, 10);
}

export function sumarDias(base, n) {
  return new Date(base.getTime() + n * 86400000);
}

/** CMC7 únicos de 30 dígitos por índice (banco 060, CP 1425). */
export function cmc7Cheque(i) {
  return (
    '060' +
    String(i % 10000).padStart(4, '0') +
    '1425' +
    String(i).padStart(8, '0') +
    String(90000000000 + i).padStart(11, '0')
  );
}

/** CMC7 únicos de 30 dígitos por índice (banco 065, CP 2077). */
export function cmc7Echeq(i) {
  return (
    '065' +
    String(i % 10000).padStart(4, '0') +
    '2077' +
    String(i).padStart(8, '0') +
    String(90000000000 + i).padStart(11, '0')
  );
}

/** Fechas variadas respetando: diferimiento ≥ emisión, vencimiento mayor a ambas y tenor ≤ 365. */
export function armarFechas(hoy) {
  const emision = sumarDias(hoy, -randInt(0, 365));
  const dif = Math.random() < 0.5 ? null : sumarDias(emision, randInt(0, 90));
  const base = dif ?? emision;
  const maxTenor = 365 - Math.round((base.getTime() - emision.getTime()) / 86400000);
  const vencimiento = sumarDias(base, randInt(1, maxTenor));
  return {
    fechaEmision: isoFecha(emision),
    fechaDiferimiento: dif ? isoFecha(dif) : null,
    fechaVencimiento: isoFecha(vencimiento),
  };
}

export function armarRequestCheque(i, hoy) {
  return {
    cmc7: cmc7Cheque(i),
    cuitLibrador: CUITS[i % 4],
    cuitBeneficiario: CUITS[(i + 1) % 4],
    monto: Math.round((10000 + Math.random() * 4990000) * 100) / 100,
    moneda: Math.random() < 0.7 ? 'P' : 'D',
    ...armarFechas(hoy),
  };
}

export function armarRequestEcheq(i, hoy) {
  return {
    cmc7: cmc7Echeq(i),
    cuitLibrador: CUITS[i % 4],
    cuitBeneficiario: CUITS[(i + 1) % 4],
    monto: Math.round((10000 + Math.random() * 4990000) * 100) / 100,
    moneda: Math.random() < 0.7 ? 'P' : 'D',
    ...armarFechas(hoy),
  };
}

/** Camino de estados extra (null = queda en inicial). */
export function tirarCamino(esEcheq) {
  const r = Math.random();
  if (r < 0.8) return null;
  if (r < 0.86) return [{ estado: 'Depositado' }];
  if (r < 0.9) return [{ estado: 'Depositado' }, { estado: 'Compensado' }];
  if (r < 0.93) return [{ estado: 'Depositado' }, { estado: 'Compensado' }, { estado: 'Pagado' }];
  if (r < 0.97)
    return [
      { estado: 'Depositado' },
      { estado: 'Rechazado', motivoRechazo: MOTIVOS[randInt(0, MOTIVOS.length - 1)] },
    ];
  if (!esEcheq) return [{ estado: 'Anulado' }];
  if (r < 0.99) return [{ estado: 'Anulado' }];
  return [{ estado: 'EnCustodia' }];
}

function parseRetryAfter(valor) {
  if (!valor) return null;
  const seg = Number(valor);
  if (Number.isFinite(seg) && seg >= 0) return seg * 1000;
  const fecha = Date.parse(valor);
  return Number.isNaN(fecha) ? null : Math.max(0, fecha - Date.now());
}

export async function pedir(api, ruta, { metodo = 'GET', cuerpo, cabeceras = {} } = {}, intento = 0) {
  let res;
  try {
    res = await fetch(api + ruta, {
      method: metodo,
      headers: { ...(cuerpo !== undefined ? { 'Content-Type': 'application/json' } : {}), ...cabeceras },
      body: cuerpo !== undefined ? JSON.stringify(cuerpo) : undefined,
      signal: AbortSignal.timeout(60000),
    });
  } catch (e) {
    if (intento >= 3) throw new Error(`red ${metodo} ${ruta}: ${e.message}`);
    await dormir(1000 * 2 ** intento);
    return pedir(api, ruta, { metodo, cuerpo, cabeceras }, intento + 1);
  }

  if (res.status === 429 && intento < 10) {
    await dormir(parseRetryAfter(res.headers.get('retry-after')) ?? 5000);
    return pedir(api, ruta, { metodo, cuerpo, cabeceras }, intento + 1);
  }
  if (res.status >= 500 && intento < 3) {
    await dormir(1000 * 2 ** intento);
    return pedir(api, ruta, { metodo, cuerpo, cabeceras }, intento + 1);
  }
  if (!res.ok) {
    const texto = await res.text().catch(() => '');
    throw new Error(`${metodo} ${ruta} → ${res.status} ${texto.slice(0, 200)}`);
  }
  if (res.status === 204) return null;
  return res.json();
}

async function crearInstrumento(api, tipo, i, hoy, conEstados) {
  const esEcheq = tipo === 'echeqs';
  const request = esEcheq ? armarRequestEcheq(i, hoy) : armarRequestCheque(i, hoy);
  try {
    var creado = await pedir(
      api,
      `/api/v1/${tipo}`,
      { metodo: 'POST', cuerpo: request, cabeceras: { 'Idempotency-Key': randomUUID() } },
    );
  } catch (e) {
    // 409 con CMC7 determinista = la fila ya existe (timeout anterior que sí impactó).
    if (String(e.message).includes('→ 409')) return 'duplicado';
    throw e;
  }
  const id = creado.identificador;

  if (!conEstados) return;

  if (esEcheq) {
    await pedir(api, `/api/v1/echeqs/${id}/aceptacion`, {
      metodo: 'POST',
      cuerpo: { aceptada: true },
    });
  }
  const camino = tirarCamino(esEcheq);
  if (!camino) return;
  const base = esEcheq ? `/api/v1/echeqs/${id}` : `/api/v1/cheques/${id}`;
  for (const paso of camino) {
    await pedir(api, `${base}/estado`, { metodo: 'PATCH', cuerpo: paso });
  }
  return 'ok';
}

/** Carga un lote de índices con concurrencia. Devuelve { ok, duplicados, fallos, reintentos }. */
export async function cargarLote({ api, tipo, indices, conEstados, conc, hoy, debeParar, reportar }) {
  let ok = 0;
  let duplicados = 0;
  let fallos = 0;
  const reintentos = new Set();
  let cursor = 0;
  async function obrero() {
    while (!debeParar()) {
      const k = cursor++;
      if (k >= indices.length) return;
      try {
        const r = await crearInstrumento(api, tipo, indices[k], hoy, conEstados);
        if (r === 'duplicado') duplicados++;
        else ok++;
      } catch (e) {
        fallos++;
        reintentos.add(indices[k]);
        if (fallos <= 5) reportar?.({ tipo: 'error', indice: indices[k], mensaje: String(e).slice(0, 200) });
      }
      if ((ok + duplicados + fallos) % 50 === 0) reportar?.({ tipo: 'progreso', ok, duplicados, fallos });
    }
  }
  await Promise.all(Array.from({ length: conc }, obrero));
  return { ok, duplicados, fallos, reintentos: [...reintentos] };
}

export function leerProgreso() {
  try {
    if (existsSync(ARCHIVO_PROGRESO)) {
      const p = JSON.parse(readFileSync(ARCHIVO_PROGRESO, 'utf8'));
      return { cheques: p.cheques ?? 0, echeqs: p.echeqs ?? 0, reintentosCq: p.reintentosCq ?? [], reintentosEq: p.reintentosEq ?? [] };
    }
  } catch {
    /* arranca de cero */
  }
  return { cheques: 0, echeqs: 0, reintentosCq: [], reintentosEq: [] };
}

export function guardarProgreso(p) {
  writeFileSync(ARCHIVO_PROGRESO, JSON.stringify(p));
}

/** Sonda: ráfaga de POSTs inválidos (no crean datos) para verificar el rate limit. */
export async function sonda(api) {
  const intentos = Array.from({ length: 40 }, (_, k) =>
    fetch(api + '/api/v1/cheques', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Idempotency-Key': randomUUID() },
      body: '{}',
      signal: AbortSignal.timeout(15000),
    })
      .then((r) => r.status)
      .catch((e) => 'ERR:' + e.message),
  );
  const estados = await Promise.all(intentos);
  const hist = {};
  for (const s of estados) hist[s] = (hist[s] ?? 0) + 1;
  return hist;
}

async function main() {
  const args = leerArgs();
  const api = args.api ?? 'https://coelsa-api-dev.onrender.com';

  if (args.sonda) {
    console.log('sonda:', JSON.stringify(await sonda(api)));
    console.log('-> todo 400 (o mayormente 400) = límite levantado; muchos 429 = sigue en 30/min');
    return;
  }

  const totalCheques = args.cheques ?? 5000;
  const totalEcheqs = args.echeqs ?? 5000;
  const desdeCq = args['desde-cq'] ?? 0;
  const desdeEq = args['desde-eq'] ?? 0;
  const conc = args.conc ?? 10;
  const fin = Date.now() + (args['max-segundos'] ?? 150) * 1000;
  const debeParar = () => Date.now() > fin;
  const hoy = new Date();

  const progreso = leerProgreso();
  const nuevosCq = Array.from(
    { length: Math.max(0, totalCheques - progreso.cheques) },
    (_, k) => desdeCq + progreso.cheques + k,
  );
  const nuevosEq = Array.from(
    { length: Math.max(0, totalEcheqs - progreso.echeqs) },
    (_, k) => desdeEq + progreso.echeqs + k,
  );
  const pendientesCq = [...new Set([...progreso.reintentosCq, ...nuevosCq])].sort((a, b) => a - b);
  const pendientesEq = [...new Set([...progreso.reintentosEq, ...nuevosEq])].sort((a, b) => a - b);

  const t0 = Date.now();
  const reportar = (e) => {
    if (e.tipo === 'progreso') console.log(`... ok=${e.ok} fallos=${e.fallos}`);
    else console.log('ERROR muestra:', JSON.stringify(e));
  };

  console.log(`cheques pendientes: ${pendientesCq.length}, echeqs pendientes: ${pendientesEq.length}`);
  const r1 = await cargarLote({
    api, tipo: 'cheques', indices: pendientesCq, conEstados: true, conc, hoy, debeParar,
    reportar: (e) => { if (e.tipo === 'error') reportar(e); },
  });
  progreso.cheques += r1.ok + r1.duplicados;
  progreso.reintentosCq = r1.reintentos;
  guardarProgreso(progreso);
  const r2 = await cargarLote({
    api, tipo: 'echeqs', indices: pendientesEq, conEstados: true, conc, hoy, debeParar,
    reportar: (e) => { if (e.tipo === 'error') reportar(e); },
  });
  progreso.echeqs += r2.ok + r2.duplicados;
  progreso.reintentosEq = r2.reintentos;
  guardarProgreso(progreso);

  console.log(
    `FIN cheques=${progreso.cheques}/${totalCheques} echeqs=${progreso.echeqs}/${totalEcheqs} ` +
    `fallos=${r1.fallos + r2.fallos} duplicados=${r1.duplicados + r2.duplicados} ` +
    `reintentos=[${progreso.reintentosCq.length},${progreso.reintentosEq.length}] ` +
    `tiempo=${Math.round((Date.now() - t0) / 1000)}s`,
  );
}

const esEjecucionDirecta =
  process.argv[1] === fileURLToPath(import.meta.url) ||
  process.argv[1]?.endsWith('/cargador.mjs') ||
  process.argv[1]?.endsWith('\\cargador.mjs');
if (esEjecucionDirecta) {
  main().catch((e) => {
    console.error('FALLO:', e.message);
    process.exitCode = 1;
  });
}
