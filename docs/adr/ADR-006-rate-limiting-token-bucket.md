# ADR-006: Rate limiting con token bucket y políticas separadas

## Estado
Accepted

## Contexto
El servicio es público y sin autenticación en v1 (SPEC sección 10), por lo
que necesita protección contra picos y abuso antes de que existan API keys
(RNF-12, RF-08). Consultas y creaciones tienen perfiles distintos: las
primeras son baratas y frecuentes; las segundas escriben en la base y deben
estar más contenidas.

## Decisión
Middleware nativo de .NET 8 con token bucket por IP y `QueueLimit = 0` (sin
cola: lo que excede se rechaza de inmediato), en dos políticas: `consultas`
(GET, 100 req/min) y `creaciones` (POST, 30 req/min), configurables por
`appsettings`, con `429` + header `Retry-After` y body ProblemDetails.

## Consecuencias
Positivas: defensa barata y predecible; validada en producción (stress test
2026-09-05: 1.830 rechazos 429 controlados, 0 errores 5xx, servicio sano al
finalizar).
Negativas / hallazgo operativo: como no hay `UseForwardedHeaders` en
`Program.cs`, detrás del proxy de Render el bucket se keyea por su IP ⇒
**todos los clientes externos comparten un solo bucket** (límite global de
facto). Para multi-usuario real hay que procesar `X-Forwarded-For` (registrar
un ADR supersede cuando se haga).
Riesgos asumidos: un cliente legítimo intensivo puede chocar con el bucket
compartido; mitigado por el fail-open y el cache, que mantienen bajo el costo
de los requests permitidos.
