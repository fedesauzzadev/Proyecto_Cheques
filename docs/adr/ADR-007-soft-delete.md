# ADR-007: Baja lógica (soft delete) con índices parciales

## Estado
Accepted

## Contexto
Eliminar un instrumento cargado por error no puede borrar historia (auditoría
y confianza del PRD HU-05, SPEC RF-06), pero mantener filas "muertas" en
índices calientes penaliza las consultas por CUIT a volumen.

## Decisión
Baja lógica: `DELETE` marca `FechaBaja` + `Activo = false` y el registro deja
de exponerse (respuesta `204`; consultas filtran `activo`). Los índices de
listado son **parciales** `WHERE activo = true`, de modo que los dados de baja
no consumen índice.

## Consecuencias
Positivas: auditoría intacta sin costo en el hot path; índices más chicos y
estables en el tiempo.
Negativas: la tabla crece con historia (aceptable en un simulador; en la
cámara real correspondería archivado/particionado, fuera de alcance v1); toda
query de negocio debe filtrar `Activo` (regla fácil de olvidar: conviene un
query filter global de EF).
Relación: este ADR es el motivo por el que ADR-008 puede usar índices
parciales.
