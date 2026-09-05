# ADR-002: Exponer identificadores de negocio, no GUIDs internos

## Estado
Accepted

## Contexto
Cada entidad tiene un `Guid Id` como PK interna de base de datos, pero la
pregunta "¿qué expone la URL?" es de diseño de API: nadie consulta a COELSA
por un GUID. Los identificadores reales del dominio son el CMC7 (30 dígitos,
cheque físico) e IDECHEQ (alfanumérico generado, echeq), ambos únicos y con
significado de negocio (SPEC RF-04, sección 6).

## Decisión
La API expone exclusivamente identificadores de negocio en rutas y respuestas
(`GET /api/v1/cheques/{cmc7}`, `GET /api/v1/echeqs/{idecheq}`, campo
`identificador` en los DTOs). El Guid interno nunca sale de la base de datos.

## Consecuencias
Positivas: URLs autocontenidas y debuggeables (un CMC7 dice banco, sucursal,
cuenta); contrato estable aunque cambie el motor de persistencia; unicidad de
negocio expresada donde corresponde (índices únicos en `cmc7`/`id_echeq`/`cud`).
Negativas: hay que validar formato de identifiers en el borde (30 dígitos,
hex-64) en vez de delegar en el parseo de Guid; el desglose del CMC7 se deriva
al armar la respuesta (sin columnas redundantes).
Riesgos: ninguno relevante en un simulador; en la cámara real estos
identificadores provienen del librador, no los genera el sistema.
