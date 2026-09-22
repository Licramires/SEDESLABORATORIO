# SI_Lab

Sistema MVC para la gestión de habilitación de laboratorios del SEDES.

## Stack y estructura

- ASP.NET Core MVC sobre .NET 8.
- Entity Framework Core 8 con SQLite (`si_lab.db`).
- Organización vertical por módulo: cada carpeta contiene sus propios `Controllers`, `Models` y `Views`.
- Entidades y contexto compartidos: `Data/Entities` y `Data/ApplicationDbContext.cs`.
- Rutas centralizadas en `Infrastructure/Routing/RouteConfig.cs`.
- Layout base en `Views/Shared/_Layout.cshtml`.

Módulos del Sprint 1:

| Carpeta | Responsable |
|---|---|
| `auth/` | Integrante 1 |
| `publico/` | Integrante 2 |
| `solicitud-registro/` | Integrante 3 |
| `solicitud-seguimiento/` | Integrante 4 |

## Flujo de ramas

La rama base es `develop`. Todas las ramas de trabajo parten de `develop` y vuelven a integrarse mediante pull request:

- `feature/auth` — Integrante 1
- `feature/publico` — Integrante 2
- `feature/solicitud-registro` — Integrante 3
- `feature/solicitud-seguimiento` — Integrante 4

## Reglas de convivencia

- Nadie cambia los nombres de campos del contrato de datos sin avisar al grupo.
- Solo el Integrante 1 modifica el layout base y el archivo central de rutas.
- Los demás módulos solo agregan código dentro de su propia carpeta.
- Es obligatoria una integración conjunta a mitad de sprint para fusionar todo a `develop` y detectar choques temprano.

## Archivos compartidos

El Integrante 1 es responsable de coordinar cambios en:

- `Views/Shared/_Layout.cshtml`
- `Infrastructure/Routing/RouteConfig.cs`
- `appsettings.json` y `Data/ApplicationDbContext.cs` para la conexión/configuración de base de datos

El contrato completo está en `CONTRATO_DATOS.md` y la asignación en `TAREAS_SPRINT1.md`.
