# ADR-001: Arquitectura hexagonal en 4 proyectos

## Estado
Accepted

## Contexto
El backend .NET 8 necesitaba una organización que separe las reglas de negocio
de los detalles técnicos (EF Core, Redis, ASP.NET), para poder testear el
dominio sin infraestructura y reemplazar adaptadores sin tocar los casos de uso
(RNF-02).

## Decisión
Organizamos la solución en 4 proyectos siguiendo arquitectura hexagonal
(puertos y adaptadores): `Domain` (entidades, enums, validación, sin
dependencias) → `Application` (casos de uso, puertos/interfaces, DTOs,
estrategias) → `Infrastructure` (EF Core, repositorios, Redis, idempotencia)
→ `Api` (controllers, middleware, composition root de DI). Los controllers solo
delegan en casos de uso; todo acceso externo cruza un puerto.

## Consecuencias
Positivas: dominio testeable con xUnit sin base de datos (RNF-10); los
adaptadores (ej. `GestorCacheRedis` vs `GestorCacheFake` en tests) son
intercambiables; SOLID e inyección nativa de .NET (RNF-03).
Negativas: más ceremonia que un CRUD directo (4 proyectos, interfaces,
mapeos); curva de aprendizaje para quien sume código.
Riesgos: la disciplina de no "saltear" capas (ej. acceder a EF desde un
controller) solo se sostiene por convención; conviene documentarla en el
`Program.cs`/composition root.
