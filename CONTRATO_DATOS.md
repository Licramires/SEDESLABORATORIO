# Contrato de datos SI_Lab

Este contrato es la referencia compartida del Sprint 1. Los nombres de campos y los valores enumerados no deben cambiarse sin avisar al grupo.

## Usuario

| Campo | Tipo | Restricciones |
|---|---|---|
| `id` | entero | Clave primaria |
| `nombre` | texto | Requerido |
| `email` | texto | Requerido |
| `password` | texto | Requerido; debe almacenarse con hash cuando se implemente autenticación |
| `rol` | enum | `propietario`, `coordinador`, `supervisor`, `gerente`, `administrador` |

## Laboratorio

| Campo | Tipo | Restricciones |
|---|---|---|
| `id` | entero | Clave primaria |
| `nombre` | texto | Requerido |
| `tipo` | texto | Requerido |
| `coordenadas.lat` | decimal | Latitud |
| `coordenadas.lng` | decimal | Longitud |
| `estado` | enum | `abierto`, `cerrado` |
| `servicios` | texto | Lista o representación acordada por el equipo |

## Solicitud

| Campo | Tipo | Restricciones |
|---|---|---|
| `id` | entero | Clave primaria |
| `propietario_id` | entero | FK a `Usuario.id` |
| `datos_laboratorio_propuesto` | texto/JSON | Datos del laboratorio propuesto |
| `estado` | enum | `en_revision`, `aprobado`, `rechazado` y estados adicionales acordados |
| `fecha_creacion` | fecha y hora | Requerido |

## Documento

| Campo | Tipo | Restricciones |
|---|---|---|
| `id` | entero | Clave primaria |
| `solicitud_id` | entero | FK a `Solicitud.id` |
| `requisito_id` | entero | Identificador del requisito |
| `archivo_pdf` | texto | Ruta o identificador del archivo PDF |

## Implementación

Las entidades base están en `Data/Entities` y el contexto en `Data/ApplicationDbContext.cs`. La migración inicial se encuentra en `Data/Migrations`. Las entidades no contienen lógica de negocio; cada módulo puede crear sus ViewModels y servicios dentro de su propia carpeta.
