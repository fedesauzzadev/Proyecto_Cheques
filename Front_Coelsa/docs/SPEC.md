# SPEC: Front Web COELSA — Consola de Instrumentos

**Versión:** 1.0-draft
**Estado:** Pendiente de validación
**Fecha:** 2026-09-05

---

## 1. Objetivo y alcance

SPA de React que consume al microservicio **Simulador COELSA** (ver
`Coelsa/docs/SPEC.md`) y ofrece la vista de autogestión del "banco nuevo":
consulta de cheques físicos y echeqs por CUIT/CUIL, detalle individual,
creación de instrumentos, avance de estados y baja lógica.

Es el **espejo didáctico del backend**: cada patrón del microservicio tiene
su contraparte en el front (ver sección 6). El negocio y el lenguaje están
definidos por el SPEC del backend; este documento define la experiencia y
la arquitectura del cliente web.

## 2. Glosario

Los términos de negocio (cheque físico, echeq, CMC7, IDECHEQ, CUD, librador,
beneficiario, diferimiento, motivo de rechazo) están definidos en la
sección 2 del SPEC del backend y no se repiten acá.

| Término                        | Definición                                                                                                                                |
| ------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------- |
| **Estado de servidor**         | Datos que viven en la API (instrumentos, totales) y se cachean en el cliente vía TanStack Query.                                          |
| **Puerto (front)**             | Interfaz de TypeScript que declara una capacidad (ej: `IPuertoInstrumentos`); el componente la consume, la infraestructura la implementa. |
| **Invalidación**               | Marcar queries afectadas tras una escritura para que TanStack Query las recargue (espejo del versionado de caché Redis del backend).      |
| **Idempotency-Key de intento** | GUID generado al montar un formulario de creación; se reusa en reintentos del mismo intento.                                              |

## 3. Requisitos funcionales

### RF-F01 — Consulta por CUIT/CUIL, separada por tipo, paginada

- Vista principal con **pestañas Cheques / Echeqs** (nunca mezclados, espejo
  de RF-03 del backend).
- Input de CUIT con **validación módulo 11 client-side** (mismo algoritmo que
  el backend): si es inválido, se informa sin llamar a la API.
- Tabla con columnas: identificador, librador, beneficiario, monto, moneda,
  fecha emisión, diferimiento, vencimiento, estado, motivo de rechazo.
- Paginado sobre el envelope `PagedResponse` (`page`, `pageSize`,
  `totalCount`, `totalPages`), selector de tamaño de página (10/25/50) y
  navegación; al cambiar de página no se pierde el scroll ni la data previa
  (`placeholderData: keepPreviousData`).
- El estado vacío (sin datos para el CUIT) tiene mensaje y call-to-action a
  creación.

### RF-F02 — Detalle individual por identificador de negocio

- Navegación desde el listado y búsqueda directa por **CMC7** (30 dígitos)
  o **IDECHEQ**.
- Para cheques físicos se muestra el **desglose del CMC7** (banco, sucursal,
  código postal, número de cheque, cuenta) parseado en el dominio del front.
- `404` → pantalla/mensaje "instrumento inexistente" con volver al listado.

### RF-F03 — Creación de instrumentos (con Strategy)

- Formularios por tipo, gobernados por una **estrategia de creación** por
  tipo (patrón Strategy, espejo de RF-01): campos, labels, validaciones y
  request builder viven en `estrategiaChequeFisico.ts` /
  `estrategiaEcheq.ts` detrás de una interfaz común.
- Validación client-side espejo del backend: CUIT módulo 11, CMC7 de 30
  dígitos (físicos y echeqs), monto > 0, `fechaEmision ≤ hoy+1`,
  `fechaDiferimiento ≥ fechaEmision` si viene, `fechaVencimiento` obligatoria y
  posterior a emisión (y a diferimiento si viene).
- Los errores del server (400/409) se muestran junto al campo
  correspondiente cuando el detalle lo permite, o como error general.

### RF-F04 — Idempotencia en creación

- Al montar el formulario se genera un **GUID de intento**; viaja como
  header `Idempotency-Key` en el POST.
- Los reintentos automáticos/manuales del mismo intento reusan el GUID.
- Si la respuesta viene con `Idempotent-Replay: true`, se informa
  explícitamente ("instrumento ya existía para este intento").
- `409 Conflict` (CMC7/CUD duplicado con otra key) → mensaje diferenciado.

### RF-F05 — Cambio de estado gobernado por la máquina de estados

- La UI **solo ofrece acciones de transiciones válidas** según el estado
  actual (espejo de RF-05): un cheque `Compensado` no ofrece "Rechazar".
- `Rechazado` exige seleccionar **motivo de rechazo** (códigos 11/12/21/25);
  los demás estados lo ocultan.
- La invalidación tras el cambio refresca listado y detalle.
- `422` (transición inválida por carrera con otro usuario) → mensaje con el
  detalle del server y refresh del recurso.

### RF-F06 — Baja lógica con confirmación

- `DELETE` precedido por modal de confirmación que muestra identificador,
  monto y la aclaración de que el instrumento deja de listarse.
- `204` → toast + invalidación + vuelta al listado; `404` → mensaje.

### RF-F07 — Manejo uniforme de errores y resiliencia

- **Un único parser** de respuestas `application/problem+json` (RFC 7807)
  mapea `title`/`detail` a mensajes en español (espejo de RNF-07).
- `429` → el cliente HTTP **respeta `Retry-After`** y reintenta una vez
  automáticamente (espejo de RF-08); si persiste, informa "demasiadas
  consultas".
- Errores de red/timeout → estado de reintento visible en la vista, sin
  perder el contexto (la query queda en error y se puede reintentar).

### RF-F08 — Indicador de salud del backend

- Badge en el header que consulta `GET /health` (espejo RF-07) con
  refresco periódico.
- Estados: saludable (verde), degradado/erróneo (ámbar/rojo) con tooltip.
- Botón de refresh manual junto al badge y reintentos con backoff ante
  fallas transitorias (los cold starts del plan gratis no deben dejarlo en rojo).

### RF-F09 — Contratos (Swagger embebido)

- Pestaña **Contratos** (`/contratos`) que embebe el Swagger UI de la API en
  un iframe, sin salir de la consola.
- La URL deriva de la misma base que usa el cliente HTTP (`VITE_API_URL`):
  en prod carga el Swagger de la API prod, en dev el de la API dev.

### RF-F10 — Aceptación y repudio (solo echeqs, espejo de RF-09)

- Un echeq `Pendiente` muestra la sección de aceptación en vez de acciones de
  estado: **Aceptar** (pasa a `Emitido`) o **Repudiar** (terminal).

### RF-F11 — Cadena de endosos (solo echeqs, espejo de RF-10)

- Timeline con orden, endosante → endosatario y badge por estado del endoso.
- Formulario para proponer (CUIT validado módulo 11, solo en `Emitido`).
- Cada propuesto se admite/repudia indicando con qué CUIT se actúa (debe ser
  el endosatario) o se anula.

### RF-F12 — Pedidos de devolución (solo echeqs, espejo de RF-12)

- Formulario de solicitud (CUIT de la cadena + motivo opcional, solo en `Emitido`).
- Cada pedido solicitado se acepta/rechaza con el CUIT del tenedor (prellenado)
  o se anula.
- La custodia se opera desde Acciones (gobernada por la máquina de estados);
  en `EnCustodia` se muestra el aviso de débito automático al vencer.

## 4. Requisitos no funcionales

| ID      | Requisito                                                                                                                                          |
| ------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| RNF-F01 | React 19 + **TypeScript strict** + Vite; Node 20 LTS.                                                                                              |
| RNF-F02 | Arquitectura hexagonal espejo: `domain`, `application` (puertos + hooks), `infrastructure` (adaptadores HTTP), `presentation`.                     |
| RNF-F03 | **TanStack Query** como único estado de servidor (cache, paginación, invalidación); sin estado global en v1.                                       |
| RNF-F04 | React Router v7 (modo declarativo).                                                                                                                |
| RNF-F05 | Tailwind CSS + shadcn/ui; tokens de diseño; responsivo; accesibilidad AA básica (labels, foco visible, contraste, `aria-` en tablas).              |
| RNF-F06 | Ubiquitous language en **español** (espejo de RNF-09): dominio y casos de uso nombrados como en el backend.                                        |
| RNF-F07 | Vitest + React Testing Library: dominio (CUIT, CMC7, transiciones) con los **mismos casos que los tests del backend**; smoke de componentes clave. |
| RNF-F08 | ESLint + Prettier; CI propio en GitHub Actions (lint + test + build), disparado solo por cambios en `Front_Coelsa/**` (paths filter).              |
| RNF-F09 | Despliegue como **static site en Render** (mismo `render.yaml` del monorepo, `staticPublishPath: dist`).                                           |
| RNF-F10 | Cliente HTTP propio sobre `fetch` con timeout; sin axios ni dependencias de red extra.                                                             |
| RNF-F11 | Configuración por ambiente con `VITE_API_URL` (dev → API dev, prod → API prod); en desarrollo local, proxy de Vite para evitar CORS.               |
| RNF-F12 | Code splitting por ruta (lazy) y assets con hash; presupuesto: bundle inicial < 250 KB gzip.                                                       |

## 5. Modelo de dominio del front

Tipos espejo del contrato OpenAPI (`Coelsa/docs/openapi.yaml` es la fuente
de verdad; ante divergencia prevalece el OpenAPI):

```ts
type EstadoInstrumento = 'Emitido' | 'Depositado' | 'Compensado'
                       | 'Rechazado' | 'Anulado' | 'Pagado';
type MotivoRechazo = 11 | 12 | 21 | 25;   // FaltaDeFondos, CuentaInexistente, DefectoFormal, Adulterado
type Moneda = 'P' | 'D';

interface ChequeResponse  { identificador: string; /* CMC7 */ tipo: 'ChequeFisico'; desgloseCmc7: ...; ... }
interface EcheqResponse   { identificador: string; /* IDECHEQ: 11 letras */ tipo: 'Echeq'; cmc7: string; desgloseCmc7: ...; ... }
interface PagedResponse<T>{ items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number; }
```

Módulos puros de dominio (sin dependencias, testeados con Vitest):

| Módulo             | Responsabilidad                                              | Espejo en el backend                 |
| ------------------ | ------------------------------------------------------------ | ------------------------------------ |
| `validadorCuit.ts` | Validación módulo 11 (mismo algoritmo y casos de test).      | `ValidadorCuit.cs`                   |
| `desgloseCmc7.ts`  | Parse de banco/sucursal/CP/número/cuenta desde 30 dígitos.   | Desglose derivado en `Mapeadores.cs` |
| `idecheq.ts`       | Formato del IDECHEQ: 11 letras mayúsculas.                   | Validación en `Echeq.Crear`          |
| `transiciones.ts`  | Máquina de estados: transiciones válidas y si exigen motivo. | `TransicionesEstado.cs`              |
| `estrategias/*.ts` | Strategy por tipo para formularios y requests.               | `Estrategias/*`                      |

Máquina de estados (idéntica a la del backend):

```
Emitido ──► Depositado ──► Compensado ──► Pagado
   │            │
   │            └──► Rechazado (exige motivo)
   └──► Anulado
```

## 6. Mapa de patrones backend ↔ front

| Patrón del backend (SPEC)                         | Contraparte en el front                                                         | Sección |
| ------------------------------------------------- | ------------------------------------------------------------------------------- | ------- |
| Strategy de creación (RF-01)                      | Estrategia por tipo para formularios y requests                                 | RF-F03  |
| Idempotencia con `Idempotency-Key` (RF-02)        | GUID de intento generado en el form y reusado en reintentos                     | RF-F04  |
| Envelope paginado (RF-03)                         | Tabla paginada con TanStack Query + `keepPreviousData`                          | RF-F01  |
| Máquina de estados (RF-05)                        | Acciones condicionadas por `transiciones.ts`                                    | RF-F05  |
| Cache Redis con invalidación por versión (sec. 7) | Cache de TanStack Query con `invalidateQueries` tras escrituras                 | RNF-F03 |
| ProblemDetails RFC 7807 (RNF-07)                  | Parser único de `application/problem+json` → toasts/campos                      | RF-F07  |
| Rate limit `429` + `Retry-After` (RF-08)          | Cliente HTTP con backoff que respeta `Retry-After`                              | RF-F07  |
| Health check (RF-07)                              | Badge de salud en el header                                                     | RF-F08  |
| Arquitectura hexagonal (RNF-02)                   | Puertos y adaptadores: `application/puertos.ts` → `infrastructure/apiCoelsa.ts` | RNF-F02 |
| Tests de dominio (RNF-10)                         | Vitest con los mismos casos (CUIT, estados, CMC7)                               | RNF-F07 |

## 7. Estructura de proyecto

```
Front_Coelsa/
  src/
    domain/                   # Tipos, enums, validadorCuit, desgloseCmc7,
                              # transiciones, estrategias (puro, sin dependencias)
    application/
      puertos.ts              # Interfaces: IPuertoInstrumentos, IPuertoSalud
      hooks/                  # useListarInstrumentos, useObtenerInstrumento,
                              # useCrearInstrumento, useCambiarEstado, useBajaLogica,
                              # useSaludBackend (TanStack Query)
    infrastructure/
      clienteHttp.ts          # fetch + timeout + ProblemDetails + 429/Retry-After
      apiCoelsa.ts            # Implementación de los puertos contra la API real
    presentation/
      rutas.tsx               # Router v7 + code splitting por ruta
      layout/                 # Header, nav, badge de salud, toast provider
      features/
        consulta/             # Pestañas cheques/echeqs + tabla + paginación
        detalle/              # Vista individual + acciones de estado/baja
        creacion/             # Formularios con strategy + idempotencia
      ui/                     # Componentes shadcn/ui + wrappers propios
    lib/                      # Utilidades transversales (cn de clases Tailwind)
    tests/                    # Espejos de los tests del backend + smoke de UI
  docs/
    SPEC.md
  Dockerfile                  # Build estático (stage node) — opcional, ver 9
```

**Flujo hexagonal:** componente → hook (application) → puerto
(`IPuertoInstrumentos`) → adaptador (`apiCoelsa.ts` en infrastructure).
Los componentes jamás importan `fetch` ni URLs directamente.

## 8. Infraestructura y CI/CD

- **Desarrollo local:** `npm run dev` (Vite) con proxy `/api` → API local
  (docker compose del backend), evitando CORS en dev.
- **Producción:** static site en Render (plan gratis), servido por CDN;
  `VITE_API_URL` apunta a la API de prod. Requiere **habilitar CORS en la
  API** con el origen del front (cambio chico en `Program.cs`).
- **CI:** workflow propio con paths filter `Front_Coelsa/**`: lint, test,
  build. El build rompe si el bundle supera el presupuesto de RNF-F12
  (`size-limit` o warning del CI).
- **render.yaml:** nuevo servicio `type: web`, `runtime: static`,
  `staticPublishPath: dist`, siguiendo `develop`/`main` igual que la API.

## 9. Fuera de alcance (v1)

- Login/autenticación (llegará junto con la API Key del servicio "banco").
- Estado global (Zustand/Redux): no hay estado compartido complejo aún.
- SSR/Next.js: es una consola operativa; SPA alcanza.
- Tests E2E (Playwright) y mock de red (MSW): evolución natural.
- i18n multiidioma: solo español.
- Gráficos/reportes de descuento de cheques (el caso de negocio real del
  "banco nuevo"): fase 2 del ecosistema.

## 10. Plan de implementación (checklist)

1. [ ] Esqueleto: Vite + React 19 + TS strict + Tailwind + shadcn/ui + ESLint/Prettier + Vitest.
2. [ ] `domain`: tipos, enums, `validadorCuit`, `desgloseCmc7`, `transiciones`, estrategias (+ tests espejo del backend).
3. [ ] `infrastructure`: `clienteHttp` (timeout, ProblemDetails, 429/Retry-After) + `apiCoelsa`.
4. [ ] `application`: `puertos.ts` + hooks con TanStack Query (listado paginado, detalle, mutaciones con invalidación).
5. [ ] `presentation` layout + feature `consulta` (pestañas, tabla, paginación, estados vacío/error).
6. [ ] `presentation` features `detalle` (acciones de estado con máquina de estados, baja con confirmación) y `creación` (forms con strategy + idempotencia).
7. [ ] CI propio (lint + test + build con paths filter) + static site en `render.yaml` + CORS en la API.
8. [ ] Verificación end-to-end contra la API dev desplegada (seed incluido).
