# ADR-004: Cache con invalidación por versión de CUIT

## Estado
Accepted

## Contexto
La invalidación clásica por borrado de claves (SCAN por prefijo) no es atómica
con la escritura y deja ventana de inconsistencia, además de ser O(n) sobre el
keyspace (RFC-001, alternativa B). Necesitábamos invalidación O(1), atómica y
sin enumerar claves.

## Decisión
Versionado por CUIT: clave
`coelsa:{tipo}:cuit:{cuit}:v{N}:p{page}:{pageSize}` donde `N` es un contador en
Redis (`coelsa:ver:{tipo}:{cuit}`). Toda escritura que toque un CUIT
(librador o beneficiario) hace `INCR` de su versión; las lecturas usan la
nueva versión y las claves viejas expiran solas por TTL (30 min + jitter
±10%). El `totalCount` del paginado viaja en la misma clave. Protección de
stampede con lock breve por clave (`SETNX` + expiry).

## Consecuencias
Positivas: invalidación atómica y O(1); autolimpieza de claves huérfanas;
implementación pequeña (un `INCR` + versión en la clave).
Negativas: las claves de versiones viejas ocupan memoria hasta expirar
(acotado por TTL); la versión vive en Redis: si se pierde, se vuelve a v0 y se
recalienta (aceptable, es cache); todo caso de uso que escriba DEBE
incrementar (regla cubierta por tests de estrategias).
Riesgos mitigados: sin ventana de inconsistencia porque la versión nueva
apunta a claves nuevas. Costo de escritura: un `INCR` por CUIT afectado.
