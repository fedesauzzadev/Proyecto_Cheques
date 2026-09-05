# Despliegue: Desarrollo y Producción

## Arquitectura

```
GitHub                          Render (gratis, sin tarjeta)
------                          --------------------------
rama develop  ──push──▶  coelsa-api-dev.onrender.com    (Desarrollo)
rama main     ──merge──▶  coelsa-api-prod.onrender.com  (Producción)

Postgres: Neon (gratis)     Redis: Upstash (gratis)
```

- Cada push a `develop` re-depliega el ambiente de **desarrollo** automáticamente.
- Cada merge a `main` (vía PR desde `develop`) re-depliega **producción** automáticamente, sin downtime.
- GitHub Actions corre build + tests + imagen Docker en cada push y PR (badge de CI).
- El blueprint `render.yaml` define la infraestructura como código.

## Paso 1: Crear la base PostgreSQL en Neon (2 min)

1. Entrar a https://neon.com → **Sign Up** (gratis, sin tarjeta).
2. **Create project** → nombre `coelsa` → región cualquiera.
3. Copiar la **connection string** del dashboard, algo como:
   ```
   postgresql://neondb_owner:PASSWORD@ep-xxx-xxx.aws.neon.tech/neondb?sslmode=require
   ```

> Sugerido: crear **dos proyectos** (`coelsa-prod` y `coelsa-dev`) para aislar los datos de cada ambiente.

## Paso 2: Crear el Redis en Upstash (2 min)

1. Entrar a https://upstash.com → **Sign in with GitHub** (gratis, sin tarjeta).
2. **Create database** → nombre `coelsa` → tipo **Regional** → región cualquiera.
3. En la pestaña **REST API** / detección, copiar el endpoint y la password. Para esta app conviene el formato StackExchange.Redis:
   ```
   TU-ENDPOINT.upstash.io:6379,password=TU_PASSWORD,ssl=true
   ```
   (También funciona si pegas directo `rediss://default:PASSWORD@TU-ENDPOINT.upstash.io:6379`, la app lo normaliza.)

> Sugerido: crear **dos bases** (`coelsa-prod` y `coelsa-dev`).

## Paso 3: Conectar el repositorio en Render (5 min)

1. Entrar a https://render.com → **Get Started** → **Sign in with GitHub** (gratis, sin tarjeta).
2. **New +** → **Blueprint** → autorizar y elegir el repositorio de este proyecto.
3. Render lee el `render.yaml` y detecta los dos servicios (`coelsa-api-prod` y `coelsa-api-dev`).
4. Pedirá completar los secretos (`sync: false`) — pegar:
   - `ConnectionStrings__Postgres`: la URL de Neon del paso 1.
   - `ConnectionStrings__Redis`: la cadena de Upstash del paso 2.
5. **Apply** → Render buildea el Dockerfile y despliega (~5 min la primera vez).
6. Al terminar, la API queda pública:
   - Producción: `https://coelsa-api-prod.onrender.com` (Swagger: `/swagger`)
   - Desarrollo: `https://coelsa-api-dev.onrender.com` (Swagger: `/swagger`)

Al arrancar, la API aplica las migraciones y siembra datos demo automáticamente (base vacía solamente).

## Flujo de trabajo diario

```bash
git checkout develop
git add . && git commit -m "mi cambio"
git push origin develop          # → CI + redeploy de DESARROLLO
```

Para liberar a producción:

```bash
# En GitHub: abrir Pull Request develop → main, revisar CI en verde, y mergear.
# El merge dispara el redeploy de PRODUCCIÓN.
```

También se puede desde la terminal con GitHub CLI:

```bash
gh pr create --base main --head develop --fill
gh pr merge --merge
```

## Notas del plan gratuito

- Los servicios de Render **duermen tras 15 min sin tráfico**: el primer request tarda ~30-60 s (cold start). Los siguientes son normales.
- Neon suspende el compute sin uso (~0.5 s de activación). Upstash: 500k comandos/mes gratis.
- `render.yaml` + los secretos viven en Render; nunca commitear credenciales al repo.
