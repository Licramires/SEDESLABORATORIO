# Contexto completo del proyecto SI_Lab

Este archivo sirve como punto de continuidad para integrantes del equipo, nuevas sesiones de Copilot y otros agentes. Describe el estado conocido del proyecto, su arquitectura, las reglas que deben respetarse, lo que ya está implementado y los siguientes pasos recomendados.

**Última actualización:** 2026-09-28  
**Repositorio:** `Licramires/SEDESLABORATORIO`  
**Ruta local habitual:** `C:\Users\Usuario\OneDrive\Documentos\GitHub\SEDESLABORATORIO`  
**Rama integrada:** `develop`  
**Commit integrado actual:** `4a4a85c merge: integrar modulo publico en develop`  
**Estado conocido al generar este documento:** `develop` estaba sincronizada con `origin/develop`; no se debe publicar este archivo sin autorización explícita del usuario.

## 1. Objetivo del sistema

SI_Lab es un sistema para la gestión de habilitación de laboratorios del SEDES. El Sprint 1 se organiza por módulos verticales, de modo que cada integrante trabaja en un módulo completo con sus controladores, modelos/ViewModels y vistas.

Casos de uso incluidos en el alcance actual:

- **CU01-CU04:** landing pública, directorio/mapa de laboratorios, detalle y cómo llegar.
- **CU05:** inicio de sesión por rol.
- **CU06-CU07:** creación de una solicitud de habilitación y carga de documentos PDF.
- **CU08-CU09:** consulta, detalle y envío de solicitudes para revisión.

## 2. Stack y convenciones técnicas

- ASP.NET Core MVC sobre .NET 8.
- Entity Framework Core 8.0.20.
- SQLite con la base local `si_lab.db`.
- Proyecto web: `SEDESLABORATORIO.csproj`.
- Nullable reference types e implicit usings habilitados.
- Bootstrap y CSS propio en `wwwroot/css/site.css`.
- Leaflet 1.9.4 y OpenStreetMap para el mapa público.
- Google Maps se usa para generar la navegación desde la ubicación del usuario.
- Organización vertical por módulo:

```text
auth/
  Controllers/
  Models/
  Services/
  Views/
publico/
  Controllers/
  Models/
  Views/
solicitud-registro/
  Controllers/
  Models/
  Views/
solicitud-seguimiento/
  Controllers/
  Models/
  Views/
```

Las entidades compartidas están en `Data/Entities`. El contexto de Entity Framework está en `Data/ApplicationDbContext.cs`. Las rutas están centralizadas en `Infrastructure/Routing/RouteConfig.cs`.

## 3. Estado funcional actual

### 3.1 Módulo `publico/`

Implementa CU01-CU04 y no requiere autenticación.

- Landing/directorio público.
- Listado ordenado por nombre.
- Búsqueda por nombre, tipo o servicios.
- Filtro por tipo.
- Filtro por estado: abierto o cerrado.
- Tarjetas con información resumida.
- Mapa Leaflet con marcadores.
- Marcadores verdes para laboratorios abiertos y rojos para cerrados.
- Vista de detalle.
- Vista de ruta usando geolocalización del navegador.
- Respuesta `404` para laboratorios inexistentes.
- Laboratorios demo creados en Development cuando no hay registros.

Archivos principales:

- `publico/Controllers/PublicoController.cs`
- `publico/Models/PublicoViewModel.cs`
- `publico/Models/LaboratorioDetalleViewModel.cs`
- `publico/Views/Publico/Index.cshtml`
- `publico/Views/Publico/Detalle.cshtml`
- `publico/Views/Publico/Ruta.cshtml`

### 3.2 Módulo `auth/`

Implementa CU05.

- Login por correo y contraseña.
- Verificación con `PasswordHasher<Usuario>` usando hash PBKDF2.
- Cookie de autenticación `si-lab-auth`.
- Esquema de autenticación `SiLabCookie`.
- Claims de identificador, nombre, correo y rol.
- Dashboard posterior al login.
- Logout.
- Vista de acceso denegado.
- Protección de acciones con `[Authorize]` y `[Authorize(Roles = "...")]`.
- Usuarios demo en Development.

Archivos principales:

- `auth/Controllers/AuthController.cs`
- `auth/Services/AuthService.cs`
- `auth/Services/IAuthService.cs`
- `auth/Models/AuthViewModel.cs`
- `auth/Models/AuthResult.cs`
- `auth/REFERENCIA_CU05.md`

### 3.3 Módulo `solicitud-registro/`

Implementa CU06-CU07 y está restringido al rol `propietario`.

- Formulario para crear una nueva solicitud.
- Validación de nombre, tipo y coordenadas.
- Creación de `Solicitud` asociada al usuario autenticado.
- Estado inicial `en_revision`.
- Fecha de creación en UTC.
- Serialización de los datos del laboratorio propuesto en JSON.
- Carga de documentos PDF.
- Límite de 10 MB por archivo.
- Validación de extensión `.pdf`.
- Almacenamiento bajo `wwwroot/uploads/solicitudes/{id}`.
- Persistencia de documentos dentro de una transacción.
- Pantalla de confirmación después de crear la solicitud.

Archivos principales:

- `solicitud-registro/Controllers/SolicitudRegistroController.cs`
- `solicitud-registro/Models/NuevaSolicitudViewModel.cs`
- `solicitud-registro/Models/SolicitudRegistroViewModel.cs`
- `solicitud-registro/Views/SolicitudRegistro/Nueva.cshtml`
- `solicitud-registro/Views/SolicitudRegistro/Exito.cshtml`

### 3.4 Módulo `solicitud-seguimiento/`

Implementa CU08-CU09 para propietarios autenticados.

- Lista únicamente las solicitudes del propietario actual.
- Muestra estado y fecha de creación.
- Muestra detalle de una solicitud.
- Permite enviar una solicitud que está en `en_revision`.
- Protege el acceso verificando el rol y el `NameIdentifier`.
- Convierte el JSON de `DatosLaboratorioPropuesto` a información legible:
  - nombre,
  - tipo,
  - latitud,
  - longitud.
- No debe volver a mostrar el JSON crudo al usuario.
- Las solicitudes que no pertenecen al propietario actual responden `404`.

Archivos principales:

- `solicitud-seguimiento/Controllers/SolicitudSeguimientoController.cs`
- `solicitud-seguimiento/Models/SolicitudSeguimientoViewModel.cs`
- `solicitud-seguimiento/Views/SolicitudSeguimiento/Index.cshtml`
- `solicitud-seguimiento/Views/SolicitudSeguimiento/Details.cshtml`

## 4. Contrato de datos

La referencia formal está en `CONTRATO_DATOS.md`. No cambiar nombres de campos ni valores enumerados sin avisar al equipo.

### Usuario

- `Id`
- `Nombre`
- `Email`
- `Password`
- `Rol`

Roles válidos:

```text
propietario
coordinador
supervisor
gerente
administrador
```

### Laboratorio

- `Id`
- `Nombre`
- `Tipo`
- `Lat`
- `Lng`
- `Estado`
- `Servicios`

Estados válidos:

```text
abierto
cerrado
```

### Solicitud

- `Id`
- `PropietarioId`, FK a `Usuario.Id`
- `Propietario`
- `DatosLaboratorioPropuesto`, actualmente JSON almacenado como texto
- `Estado`
- `FechaCreacion`
- colección `Documentos`

Estados actualmente implementados:

```text
en_revision
aprobado
rechazado
```

### Documento

- `Id`
- `SolicitudId`, FK a `Solicitud.Id`
- `Solicitud`
- `RequisitoId`
- `ArchivoPdf`

Las entidades base no deben contener lógica de negocio. La lógica de cada caso de uso debe permanecer en su módulo, mediante controladores, ViewModels y servicios propios cuando sea necesario.

## 5. Base de datos y migraciones

Configuración principal:

- `appsettings.json` activa `Auth:SeedDemoUsers`.
- `appsettings.Development.json` contiene `DefaultConnection`:

```text
Data Source=si_lab.db
```

El arranque ejecuta `DatabaseInitializer.InitializeAsync`, que:

1. Aplica migraciones pendientes.
2. En Development y con `Auth:SeedDemoUsers=true`, crea usuarios demo si no existen.
3. En Development crea laboratorios demo si no existen.

Migraciones existentes:

- `Data/Migrations/20260922225808_InitialCreate`
- `Data/Migrations/20260923042352_AddUniqueEmail`

La base local y sus archivos auxiliares están ignorados por Git:

```text
si_lab.db
si_lab.db-shm
si_lab.db-wal
```

No se debe usar una base local como fuente para transportar datos entre integrantes. Los cambios de esquema deben hacerse mediante migraciones.

## 6. Rutas conocidas

Las rutas se configuran en `Infrastructure/Routing/RouteConfig.cs`.

### Públicas

```text
GET /
GET /laboratorios
GET /laboratorios/Detalle/{id}
GET /laboratorios/Ruta?laboratorioId={id}
```

También funciona la ruta por área:

```text
/publico/Publico/Index
```

### Autenticación

```text
/auth/Auth/Index
/auth/Auth/Login
/auth/Auth/Dashboard
/auth/Auth/AccessDenied
```

El logout es `POST` y usa antiforgery token.

### Solicitud de registro

```text
/solicitud-registro/SolicitudRegistro/Index
/solicitud-registro/SolicitudRegistro/Nueva
/solicitud-registro/SolicitudRegistro/Exito
```

El acceso requiere el rol `propietario`.

### Seguimiento

```text
/solicitud-seguimiento/SolicitudSeguimiento/Index
/solicitud-seguimiento/SolicitudSeguimiento/Details/{id}
```

El envío de una solicitud se realiza mediante `POST` a la acción `Enviar`.

## 7. Usuarios demo para Development

Las cuentas se crean únicamente si la base de usuarios está vacía y la configuración de seed está activa.

Contraseña temporal para todas:

```text
Demo123!
```

| Rol | Correo |
|---|---|
| propietario | `propietario@si-lab.local` |
| coordinador | `coordinador@si-lab.local` |
| supervisor | `supervisor@si-lab.local` |
| gerente | `gerente@si-lab.local` |
| administrador | `administrador@si-lab.local` |

Estas credenciales son solo para desarrollo. Deben reemplazarse antes de cualquier despliegue real.

## 8. Flujo de ramas y convivencia

Rama base:

```text
develop
```

Ramas de trabajo:

```text
feature/auth
feature/publico
feature/solicitud-registro
feature/solicitud-seguimiento
```

Reglas:

1. Cada rama de funcionalidad debe partir de `develop`.
2. La integración normal se realiza mediante Pull Request hacia `develop`.
3. Nadie cambia nombres o valores del contrato de datos sin avisar al grupo.
4. Solo el Integrante 1 modifica archivos compartidos sensibles:
   - `Views/Shared/_Layout.cshtml`
   - `Infrastructure/Routing/RouteConfig.cs`
   - `appsettings.json`
   - `Data/ApplicationDbContext.cs`
5. Los demás integrantes agregan código dentro de su propia carpeta y leen los archivos compartidos.
6. Se debe hacer una integración conjunta a mitad del sprint para detectar conflictos temprano.
7. No ejecutar `git push` salvo que el usuario lo solicite explícitamente.
8. No incluir en commits `bin/`, `obj/`, `.vs/`, archivos `.user`, la base SQLite ni sus archivos auxiliares.

Asignación actual documentada en `TAREAS_SPRINT1.md`:

| Integrante | Módulo | Casos de uso | Dependencia |
|---|---|---|---|
| Werner | `auth/` | CU05 - Login por rol | — |
| Leo | `publico/` | CU01-CU04 - Landing, mapa, detalle, ruta | — |
| Alejandro | `solicitud-registro/` | CU06-CU07 - Nueva solicitud y documentos | Modelo Usuario |
| Lucas | `solicitud-seguimiento/` | CU08-CU09 - Enviar y ver estado de trámite | Modelo Solicitud |

Si cambia la asignación del equipo, actualizar `TAREAS_SPRINT1.md` y este documento juntos.

## 8.1 Planificación completa de casos de uso

La planificación original entregada formalmente al proyecto cubría el Sprint 1. Después de contrastarla con el video y la transcripción de la reunión, se documenta aquí el alcance previsto de los sprints siguientes. Los casos CU30, CU31, CU32 y CU-P4 se agregan para cubrir comportamientos mencionados explícitamente en la reunión.

### Sprint 1 — Implementado

```text
CU01: Ver la landing page pública con los tipos de laboratorio
CU02: Buscar y filtrar laboratorios en el mapa (tipo y estado)
CU03: Ver el detalle de un laboratorio
CU04: Trazar ruta hacia el laboratorio más cercano
CU05: Iniciar sesión según el rol
CU06: Registrar nueva solicitud de apertura
CU07: Cargar documentación en PDF según los requisitos
CU08: Enviar la solicitud (queda "en revisión")
CU09: Consultar el estado de los trámites propios
```

Estado: integrado en `develop`. Los módulos actuales cubren CU01-CU09.

### Sprint 2 — Flujo operativo de revisión e inspección

```text
CU10: Revisar solicitud y documentación, aprobar o rechazar (Coordinador)
CU11: Asignar supervisor a un establecimiento (Coordinador)
CU12: Consultar historial de asignaciones (Coordinador)
CU13: Organizar agenda de inspecciones (Supervisor)
CU14: Generar ruta ordenada de inspección (Supervisor)
CU15: Registrar acta de inspección (Supervisor)
CU16: Descargar el formulario de acta y subirlo firmado en PDF (Supervisor)
CU17: Definir resultado: aprobado, con observaciones o rechazado (Supervisor)
CU18: Registrar citación por incumplimiento (Supervisor)
CU19: Revisar el acta, aprobar o rechazar y reasignar si corresponde (Coordinador)
CU20: Marcar vigencia de la habilitación en 1 año (Sistema)
CU21: Notificar al propietario en cada hito (Sistema)
CU30: Reprogramar una inspección rechazada o pendiente (Coordinador)
CU31: Registrar plazo de subsanación para observaciones (Supervisor)
CU32: Registrar solicitud de renovación de habilitación (Propietario)
```

CU30 se deriva de la indicación de que una inspección rechazada debe volver a programarse y puede asignarse al mismo u otro supervisor. CU31 representa los plazos de subsanación, por ejemplo 15 días hábiles, mencionados para corregir incumplimientos. CU32 se agrega porque la reunión distingue trámites de apertura y renovación; debe confirmarse con el equipo si la renovación entra en el alcance de este proyecto o queda para una etapa posterior.

### Sprint 3 — Administración, métricas y cierre

```text
CU22: Gestionar usuarios (Administrador)
CU23: Gestionar roles y permisos (Administrador)
CU24: Configurar requisitos: crear, editar, eliminar, obligatorio/opcional (Administrador)
CU25: Ver panel de métricas por municipio (Gerente)
CU26: Descargar informe de métricas por fechas (Gerente)
CU27: Consultar "Mis establecimientos" y editar datos visuales permitidos (Propietario)
CU28: Recibir alertas de vencimiento de licencia (Propietario)
CU29: Generar informe final de cierre del trámite (Coordinador)
```

En CU27, el propietario no debe poder modificar datos oficiales ya aprobados, como el estado de habilitación, la vigencia, el resultado de inspección o los documentos validados. Esos datos deben modificarse únicamente mediante los flujos autorizados.

### Casos pendientes del abogado

La reunión agregó posteriormente el rol de abogado. Estos casos siguen pendientes de asignación formal a un sprint:

```text
CU-P1: Revisar legalmente el establecimiento (licencia y plano)
CU-P2: Firmar la aprobación legal final y notificar al Coordinador
CU-P3: Notificar al propietario la aprobación final
CU-P4: Cerrar y marcar el trámite como habilitado después de la aprobación legal
```

CU-P4 se agrega para representar explícitamente el cierre del flujo: después de la aprobación legal, el establecimiento queda habilitado y el propietario recibe la confirmación final. Puede fusionarse con CU-P2 o CU-P3 si el equipo decide no mantenerlo como caso independiente.

## 9. Cómo ejecutar el proyecto

Desde la raíz del repositorio:

```powershell
dotnet restore
dotnet build
dotnet run
```

La aplicación puede iniciar en un puerto HTTP/HTTPS indicado por el perfil de ejecución. Usar la URL que muestre la consola, normalmente una de estas:

```text
http://localhost:5095
http://localhost:5250
```

Si solo se usa HTTP y aparece una advertencia de `HttpsRedirectionMiddleware`, no significa necesariamente que la aplicación haya fallado. Para pruebas locales conviene abrir la URL HTTP que informe la consola o configurar explícitamente el perfil HTTPS.

Para detener una ejecución iniciada desde la terminal:

```text
Ctrl+C
```

## 10. Pruebas funcionales mínimas

### Compilación

```powershell
dotnet build --no-restore
```

Resultado esperado: 0 errores y 0 advertencias.

### Público

1. Abrir `/`.
2. Confirmar que aparecen los laboratorios demo.
3. Buscar por nombre, tipo o servicio.
4. Filtrar por tipo.
5. Filtrar por estado abierto/cerrado.
6. Abrir `Ver detalle`.
7. Abrir `Cómo llegar`.
8. Confirmar que el mapa carga con conexión a Internet.
9. Probar un ID inexistente y confirmar `404`.

### Autenticación

1. Abrir `/auth/Auth/Login`.
2. Probar una cuenta demo válida.
3. Confirmar redirección al dashboard.
4. Confirmar que el rol aparece en la sesión.
5. Probar una contraseña inválida y confirmar mensaje de error.
6. Ejecutar logout.

### Registro de solicitud

1. Iniciar sesión como `propietario`.
2. Abrir `/solicitud-registro/SolicitudRegistro/Nueva`.
3. Completar nombre, tipo y coordenadas.
4. Adjuntar uno o más PDF válidos.
5. Probar un archivo no PDF o mayor de 10 MB.
6. Confirmar que la solicitud queda en `en_revision`.
7. Confirmar que los documentos se guardan bajo `wwwroot/uploads/solicitudes`.

### Seguimiento

1. Mantener sesión como `propietario`.
2. Abrir `/solicitud-seguimiento/SolicitudSeguimiento/Index`.
3. Confirmar que solo se muestran solicitudes del usuario actual.
4. Abrir el detalle.
5. Confirmar que el laboratorio propuesto aparece formateado y no como JSON crudo.
6. Enviar la solicitud.

## 11. Validaciones ya realizadas

La integración del módulo público se probó antes de fusionarla:

```text
PASS public landing
PASS directory route
PASS search filter
PASS detail view
PASS route view
PASS missing laboratory returns 404
RESULT failures=0
```

La compilación integrada más reciente pasó con:

```text
0 errores
0 advertencias
```

El proceso de prueba HTTP pudo finalizar con código distinto de cero al detener la aplicación, aunque las solicitudes ya habían terminado correctamente. Distinguir siempre entre un fallo de una ruta y la finalización intencional del proceso web.

## 12. Archivos compartidos importantes

- `Program.cs`: registra MVC, Razor, EF Core, cookies, autorización y el pipeline HTTP.
- `Data/ApplicationDbContext.cs`: DbSets, conversiones de enums, índices y relaciones.
- `Data/DatabaseInitializer.cs`: migraciones y datos demo.
- `Infrastructure/Routing/RouteConfig.cs`: rutas por área, raíz pública y rutas convencionales.
- `Views/Shared/_Layout.cshtml`: layout y navegación global.
- `wwwroot/css/site.css`: estilos de autenticación, público, registro y seguimiento.
- `CONTRATO_DATOS.md`: contrato formal de entidades.
- `TAREAS_SPRINT1.md`: asignación del sprint.
- `README.md`: resumen de arquitectura, ramas y convivencia.

## 13. Pendientes y evolución recomendada

El Sprint 1 integrado está funcional, pero todavía hay trabajo recomendable antes de considerarlo listo para producción:

1. Revisar visualmente todas las pantallas en navegador y ajustar responsive.
2. Confirmar con el cliente si el proveedor definitivo del mapa será OpenStreetMap/Leaflet, Google Maps u otro.
3. Reemplazar credenciales demo y desactivar el seed en producción.
4. Definir los flujos para coordinador, supervisor, gerente y administrador; por ahora sus roles existen para autenticación, pero sus módulos funcionales no están implementados.
5. Definir estados adicionales de `Solicitud` si el negocio los requiere.
6. Definir `RequisitoId` real y su catálogo; actualmente los documentos creados desde el formulario usan `0`.
7. Añadir validaciones de contenido PDF, nombres seguros, límites globales y estrategia de almacenamiento para producción.
8. Considerar un servicio de almacenamiento separado para documentos en lugar del filesystem local.
9. Añadir pruebas automatizadas de integración y pruebas de autorización.
10. Revisar antiforgery, cookies seguras, HTTPS y secretos antes de desplegar.
11. Confirmar si el botón `Enviar` debe cambiar el estado a un estado adicional distinto de `en_revision`; el contrato actual representa una solicitud enviada como `en_revision`.
12. Confirmar con el cliente el alcance de renovación (CU32) y la ubicación definitiva de los casos del abogado.
13. Definir estados adicionales de `Solicitud` para inspección, observaciones, subsanación, aprobación legal, habilitación y renovación.
14. Evitar cambios directos al contrato compartido sin coordinación del equipo.

## 14. Procedimiento para una nueva sesión o agente

Un agente nuevo debe seguir este orden:

1. Leer `CONTEXTO_PROYECTO.md`.
2. Leer `README.md`, `CONTRATO_DATOS.md` y `TAREAS_SPRINT1.md`.
3. Ejecutar `git status`, `git branch -vv` y `git log --oneline --decorate -10`.
4. Identificar el módulo asignado y trabajar dentro de su carpeta.
5. Leer los archivos compartidos antes de modificar rutas, layout, contexto o configuración.
6. Ejecutar `dotnet build --no-restore` después de cambios de código.
7. Ejecutar pruebas funcionales relacionadas con el módulo.
8. No hacer `git push` sin autorización explícita.
9. Informar cualquier cambio de contrato, migración o conflicto antes de continuar.

Este documento debe actualizarse cuando se agregue un módulo, cambie una ruta, se modifique el contrato, se complete un caso de uso o se altere el flujo de ramas.
