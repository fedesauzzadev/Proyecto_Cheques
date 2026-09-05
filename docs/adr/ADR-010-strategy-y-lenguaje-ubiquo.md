# ADR-010: Patrón Strategy en creaciones + lenguaje ubicuo en español

## Estado
Accepted

## Contexto
Cheque físico y echeq comparten el flujo de creación (validar → persistir →
invalidar cache de ambos CUITs → seed de idempotencia) pero difieren en
validación e identificación: CMC7 único y bien formado vs CUD hex-64 con
IDECHEQ generado por el simulador (SPEC RF-01). Además, el dominio es
argentino (COELSA, CUIT, CMC7) y el código debía hablar el idioma del negocio
(RNF-09).

## Decisión
Estrategias de creación tras la interfaz común `ICrearInstrumentoStrategy`:
`ChequeFisicoCreationStrategy` y `EcheqCreationStrategy` encapsulan cada
validación; la base compartida (`CreacionInstrumentoStrategyBase`) centraliza
idempotencia e invalidación de cache de ambos CUITs. Todo el código de dominio
(enums, entidades, validadores, máquina de estados) en español como lenguaje
ubicuo.

## Consecuencias
Positivas: agregar un tercer tipo de instrumento no toca los existentes
(Open/Closed); la lógica común (idempotencia + invalidación) vive en un solo
lugar; el código es legible para cualquiera del dominio.
Negativas: indirección extra frente a un `if` en el handler (costo aceptado
por extensibilidad); el español en código puede chocar con convenciones de
librerías en inglés (se resolvió manteniendo inglés solo en infraestructura
externa: EF, Redis, HTTP).
Riesgos: proliferación de estrategias si el dominio crece mucho; mitigado
porque cada estrategia tiene tests unitarios propios (`xUnit`, RNF-10).
