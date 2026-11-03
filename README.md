# SI_Lab — Habilitación de laboratorios SEDES

## Estado vigente — bugs corregidos el 26/10/2026

Corregidos y verificados los tres bugs de la campaña: recuperación de formularios ante 400/409, aprobación documental repetida e invalidación de rutas/marcadores con coordenadas persistidas fuera de rango. Se mantiene la arquitectura SOLID. Compilación Release sin errores ni advertencias, 389 pruebas de servidor y 28 JavaScript aprobadas; comprobación real de las correcciones en navegador y copia local. Base original y 54 archivos privados conservados.

Cierre y alcance: [bugs corregidos](docs/CIERRE_BUGS_2026-10-26.md). Evidencia: `outputs/bugs-20261026/verificacion.json`. Punto previo: `App_Data/checkpoints/20261026-144304-antes-bugs-pendientes/`. Los nueve casos manuales/parciales y el recorrido completo de todos los controles siguen sin certificarse; los Excel del 23/10 mantienen sus resultados históricos.

## Refactorización SOLID del 26/10/2026 — estado anterior

La gestión de decisiones usa manejadores extensibles, contratos por consumidor e inyección de dependencias. Calendario, avisos, historial, reservas, agenda y métricas tienen responsabilidades separadas. El filtro de permisos comparte el registro de acciones. Se conservan las rutas y vistas existentes y no hay migración de esquema. Detalles, contratos de sustitución y límites: [arquitectura SOLID](docs/ARQUITECTURA_SOLID.md).

Compilación Release sin errores ni advertencias; 377 pruebas de servidor aprobadas (366 existentes y 11 de contratos). La base original y sus 54 archivos privados coinciden con el punto previo `App_Data/checkpoints/20261026-135109-antes-solid/`. Evidencia: `outputs/solid-20261026/verificacion.json`. Los tres bugs y nueve casos pendientes de la campaña del 23/10 conservan su estado; esta refactorización no es su cierre funcional.

## Pruebas locales del 23/10/2026 — estado anterior

366 pruebas .NET y 28 JavaScript aprobadas. Restauración reconstruida desde el respaldo y arrancada, con sus 360 pruebas originales aprobadas. Ensayos reales locales de concurrencia, cortes de carga, reinicio inesperado, almacenamiento no disponible, caída entre archivo y confirmación y bloqueo de SQLite. Correcciones de recuperación privada de archivos, exclusión de instancia, respuestas 503/409 y espera limitada de actualización JSON. Sin migración ni cambios en la base original o sus 47 archivos privados.

Producción excluida por indicación del usuario. La ampliación de interfaz quedó detenida al no poder verificar la URL desde el control de Windows; GPS físico, otros dispositivos y contenido institucional pendiente no están certificados. Informe vigente: [Pendientes locales](docs/CIERRE_PENDIENTES_LOCALES_2026-10-23.md). Punto previo: `App_Data/checkpoints/20261022-234400-antes-cierre-pendientes/`. Los apartados siguientes conservan las revisiones anteriores.

## Auditoría ampliada — 22/10/2026 (revisión anterior)

360 pruebas .NET y 6 JavaScript aprobadas. Inventario de 69 acciones: 301 consultas por perfil, 603 enlaces, 318 comprobaciones de seguridad POST, 685 transiciones prohibidas, 17 permitidas y sus reenvíos, 228 casos de campos inválidos y los 11 permisos configurables. Navegación real con los seis perfiles y el portal público; correcciones de formularios, calendario, concurrencia de servicios y consistencia de renovaciones. Base original idéntica al respaldo y 47 archivos privados conservados, sin nueva migración. Siguen pendientes propietario/vigencia de tres laboratorios heredados y los contenidos institucionales definitivos.

Informe y evidencia: [Auditoría ampliada](docs/AUDITORIA_EXHAUSTIVA_2026-10-22.md). Punto previo: `App_Data/checkpoints/20261022-223121-antes-auditoria-exhaustiva/`. Las verificaciones siguientes describen etapas anteriores.

Aplicación ASP.NET Core MVC (.NET 8), Entity Framework Core y SQLite. Implementa el directorio público y el circuito de apertura/renovación, revisión documental, inspección, revisión legal y habilitación por un año.

## Verificación anterior — 22/10/2026

Auditoría de los seis roles: 91 pruebas .NET y 4 JavaScript aprobadas. Se corrigieron conflictos de agenda, actas redundantes, devolución legal por documento, edición de borradores, renovaciones y validaciones; se simplificaron los paneles. Los registros iniciales usan los mismos flujos y reglas, sin categorías demo/oficial. Nueve migraciones. Detalle y límites: [auditoría de flujos](docs/AUDITORIA_FLUJOS_2026-10-22.md).

Punto de restauración anterior: `App_Data/checkpoints/20261021-233744-antes-auditoria-flujos` (ZIP, base, documentos privados y SHA-256).

## Ejecutar

Requiere SDK .NET 8. Desde la raíz:

```powershell
dotnet restore SEDESLABORATORIO.sln
dotnet build SEDESLABORATORIO.sln -c Release --no-restore
dotnet run --project SEDESLABORATORIO.csproj -c Release --launch-profile http
```

URL del perfil HTTP: `http://localhost:5250`. Las migraciones se aplican al arrancar. La base `si_lab.db` y los PDF en `App_Data/documentos` son locales y están excluidos de Git.

Se necesita un SDK compatible con .NET 8 disponible en PATH. La verificación actual se ejecutó en Release sin modificar las protecciones de Windows.

## Cuentas de desarrollo

Solo con `ASPNETCORE_ENVIRONMENT=Development` y `Auth:SeedDemoUsers=true`, se crean las cuentas demo que falten, conservando las contraseñas y roles de las existentes. Contraseña inicial: `Demo123!`.

| Perfil | Correo | Funciones |
|---|---|---|
| Propietario | propietario@si-lab.local | Solicitudes, documentos, seguimiento, servicios y renovación |
| Coordinador | coordinador@si-lab.local | Documentación, asignaciones, revisión de actas e informe de cierre |
| Supervisor | supervisor@si-lab.local | Agenda, rutas, actas, citaciones y plazos |
| Abogado | abogado@si-lab.local | Revisión de licencia/plano y aprobación firmada |
| Gerente | gerente@si-lab.local | Métricas por municipio/fechas y exportación CSV |
| Administrador | administrador@si-lab.local | Usuarios, perfiles, permisos y requisitos |

Con `Auth:SeedDemoData=true` (activado en desarrollo), el arranque también agrega una sola vez 13 expedientes identificados como `DEMO`: borrador, revisión documental, aprobado para asignación, inspección sin programar, visita lista para registrar, visita futura, revisión de acta, revisión legal, observación documental, citación de campo, habilitado, renovación y rechazo definitivo. Incluye PDF ficticios, historial, dos laboratorios del propietario con vigencia y una notificación por perfil. El gerente puede probar métricas y el administrador dispone de ejemplos de catálogos y formularios en las clases que aún estén vacías.

Los documentos y formularios indican que no tienen validez oficial. Los catálogos existentes no se sustituyen. La carga funciona también en bases de desarrollo anteriores, sin borrar registros ni volver a crear los ejemplos ya cargados al reiniciar; el marcador `demo:expedientes:v1` evita duplicados aunque se trabajen los expedientes. Para tener solo cuentas sin estos escenarios, configure `Auth:SeedDemoData=false`. En producción no se crean cuentas ni datos demo. Las fechas se calculan al cargarlos; no se actualizan ni se reinician los avances automáticamente.

## Supervisor y métricas visuales

El supervisor entra en **Mi agenda**, con búsqueda, semana completa y selección de día; dispone de rutas por fecha, actas emitidas, citaciones propias y expedientes asignados. Las visitas sin horario y las anteriores pendientes de acta aparecen aparte. El detalle de inspección muestra la siguiente tarea, programación, preparación del contenido, descarga de formulario/resumen y envío del PDF firmado; documentos e historial quedan disponibles para consulta. El acta registrada queda pendiente de revisión del coordinador. El formulario del supervisor usa la copia de su propia inspección, incluso si después intervino otro supervisor.

Gerencia dispone de gráficos de aperturas/renovaciones por mes, estados actuales, municipios, tipo de laboratorio solicitado, actas por supervisor y puntos de atención. Se calculan con la base guardada y respetan período y municipio. La tasa usa decisiones definitivas; los borradores se separan y el promedio se calcula sobre todos los cierres con fechas válidas, sin promediar promedios municipales. Los meses sin envíos se incluyen con cero; sin decisiones o cierres se indica que no hay datos. No se inventan niveles, capacidad de inspectores ni comparaciones anuales sin datos. «Actualizar gráficos» consulta otra vez el servidor; «Consultar período» renueva también tablas, trazabilidad y el contexto del CSV.

React está integrado como componentes dentro de las vistas MVC, con archivos locales en `wwwroot/js/operations.js`; no se cambió la autenticación ni se necesita un servidor Node en producción. La agenda conserva una lista HTML de respaldo y las métricas conservan tablas accesibles. El paquete generado y su licencia se incluyen en el repositorio. Para modificar React, con Node 22 y npm instalados, desde `ClientApp/` ejecute:

```powershell
npm.cmd ci
npm.cmd test
npm.cmd run build
```

Después compile .NET normalmente. Las dependencias están fijadas en `ClientApp/package-lock.json`; CI ejecuta ambas suites y vuelve a generar el paquete. Son 62 casos de servidor y cuatro pruebas JavaScript. Las rutas se ordenan por horario y se dividen en bloques de hasta cuatro visitas para admitir tres puntos intermedios y destino en móvil, conforme a la [documentación de Google Maps](https://developers.google.com/maps/documentation/urls/get-started). La navegación real depende de Maps; no es una optimización automática de distancias.

## Administración de roles y requisitos

En **Roles y permisos** se muestra la matriz de funciones y el número real de usuarios por perfil. **Editar permisos de…** permite marcar las funciones de un perfil y guardarlas juntas; los cambios afectan inmediatamente a sus sesiones y se registran con fecha y autor. Se conservan los seis perfiles y sus límites de propiedad, asignación y revisión. Una edición desactualizada exige recargar, y la administración de roles permanece accesible para recuperar permisos.

En **Requisitos** se pueden crear y editar secciones (título, descripción, orden y estado), añadir requisitos a cada sección, moverlos, ordenarlos, cambiar su obligatoriedad, adjuntar una plantilla PDF y activarlos o desactivarlos. Eliminar retira un requisito del catálogo; desactivar permite volver a publicarlo. Para eliminar una sección hay que mover o eliminar primero sus requisitos. La baja conserva los documentos, plantillas y referencias históricas. La página pública, el registro y los borradores usan solo requisitos activos de secciones activas; los expedientes enviados conservan su copia anterior, incluida la sección.

Los requisitos anteriores permanecen en «Requisitos generales» hasta asignarlos a una sección. No se agregaron requisitos institucionales sin validar. Catálogos, formularios y validación de datos siguen disponibles como herramientas administrativas de la implementación existente, aunque no figuran en el menú de la maqueta.

## Flujo

El menú público y el de usuarios incluyen **Requerimientos**, con el listado activo, obligatoriedad y formularios descargables. El mismo listado aparece al crear una solicitud. En el registro se pueden seleccionar PDF sucesivamente, ver sus nombres/tamaños y quitar cualquiera antes de guardar. Ya no existe el límite de cinco archivos: se conservan 10 MB por PDF y 50 MB por carga inicial. También puede guardar el borrador sin archivos y adjuntarlos uno por uno después; los pendientes u observados se pueden quitar de la lista de envío. Los documentos aprobados permanecen protegidos y los retirados conservan su historial.

1. El propietario guarda un borrador, adjunta los PDF por requisito y lo envía.
2. El coordinador revisa cada PDF y aprueba la documentación o la devuelve para subsanar. El rechazo definitivo se diferencia de una observación. Después asigna supervisor, con horario opcional.
3. El supervisor organiza inicio/fin, prepara el contenido del acta, descarga formulario/resumen y adjunta el acta firmada. Puede registrar citaciones con evidencia PDF/JPG/PNG y plazos de subsanación.
4. El coordinador acepta el resultado o rechaza el acta para reprogramar.
5. El abogado revisa licencia/plano, devuelve observaciones, rechaza definitivamente o adjunta la aprobación firmada. Una subsanación legal no repite una inspección conforme salvo indicación explícita.
6. La aprobación legal valida los datos y habilita el laboratorio por un año. Guarda una copia inmutable del informe y la vigencia de ese cierre. La renovación conserva el establecimiento y no modifica informes anteriores.

Las decisiones y versiones de documentos se conservan en historial. Un proceso horario avisa de licencias, citaciones y subsanaciones sin duplicados. Las agendas y fechas de citación usan hora de Bolivia (UTC−4); el historial y las métricas indican UTC. Los valores configurables están en `CONTEXTO_PROYECTO.md`.

## Pruebas

```powershell
dotnet test tests/SiLab.Tests/SiLab.Tests.csproj -c Release --logger "trx;LogFileName=integration.trx"
```

Las 366 pruebas utilizan SQLite y almacenamiento temporales, cookies reales y tokens antiforgery; no modifican la base local. Incluyen recorridos completos, migraciones con datos anteriores, historial, autorización, citaciones/fotos, métricas, concurrencia, carga demo sin duplicados y gestión de más de cinco PDF con retiro/reposición de obligatorios. El informe está en [VERIFICACION.md](VERIFICACION.md). El workflow `.github/workflows/verify.yml` exige al menos 366 pruebas y se ejecuta al subir cambios o abrir un PR; no se publicó ni ejecutó remotamente en esta sesión.

## Despliegue

- En Production, la primera cuenta se crea con las variables de entorno `Bootstrap__AdminEmail` y `Bootstrap__AdminPassword` (mínimo 12 caracteres). Se exigen únicamente si la base está vacía. Retirar esas variables después del primer arranque.
- Configurar `ConnectionStrings__DefaultConnection`, `Storage__Root` y HTTPS. Las cookies de producción requieren HTTPS.
- Respaldar conjuntamente SQLite y `App_Data/documentos`; el almacenamiento debe ser persistente y privado. No servir `App_Data` ni los antiguos `wwwroot/uploads` desde el servidor frontal.
- Se conserva compatibilidad con rutas antiguas de PDF mediante descarga autorizada. La aplicación bloquea el acceso HTTP directo a `/uploads`.
- Mantener una instancia para SQLite y el proceso de alertas. Las notificaciones no usan correo/SMS y las firmas son documentos adjuntos, sin firma criptográfica integrada.
- El administrador carga tipos, municipios, requisitos, puntos de inspección, plantillas y feriados a partir de fuentes institucionales. Producción no crea requisitos demo y exige los catálogos para nuevas aperturas; las actas reales requieren lista y plantilla.
- Los establecimientos sin datos verificados no se publican en producción. La pantalla de validación los enumera sin asignar propietarios ni vigencias por suposición.

El plan aprobado, las correcciones y los pendientes externos están en [el informe de cierre](docs/CIERRE_CORRECCIONES_2026-10-18.md). La base local tiene respaldo previo a las dos migraciones nuevas; sus registros originales se conservaron.

## Estructura y mantenimiento

Los módulos originales permanecen en `auth/`, `publico/`, `solicitud-registro/` y `solicitud-seguimiento/`. La operación integrada usa `Controllers/GestionController.cs`, `AdministracionController.cs`, `MetricasController.cs`, `NotificacionesController.cs`, sus vistas y servicios en `Infrastructure/`.

Las entidades están en `Data/Entities`; los cambios de esquema se versionan en `Data/Migrations`. No transportar datos mediante la base local ni renombrar campos sin coordinar el contrato.

La rama base histórica es `develop`. Integrar por PR y no publicar cambios sin autorización. La asignación original del equipo se conserva en `TAREAS_SPRINT1.md`; el estado actual está en `CONTEXTO_PROYECTO.md` y el modelo en `CONTRATO_DATOS.md`.
