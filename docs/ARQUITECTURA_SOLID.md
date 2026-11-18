# Aplicación de SOLID en SI_Lab

Refactorización del 26/10/2026. Se conserva ASP.NET Core MVC, EF Core, SQLite y las vistas existentes. No requiere una migración de datos. SOLID es un criterio de diseño y mantenimiento; esta documentación describe decisiones y comprobaciones concretas, no una certificación universal de «100 %».

## Responsabilidad única — S

El controlador `GestionController.Decidir` enlaza y valida el formulario, llama a `IDecisionesTramite` y presenta su resultado. Las reglas de envío, revisión documental, asignación, programación, acta, validación del acta y revisión legal están en clases separadas en `Application/Tramites/Acciones`.

`DecisionesTramite` coordina la transacción y el resultado auditable. Los manejadores no confirman cambios por su cuenta. El archivo generado antes de una operación fallida se limpia en el coordinador; el cierre legal conserva su PDF histórico dentro del mismo circuito transaccional.

Se separan calendario laboral, registro del historial, avisos por rol, comprobación de reservas y alertas de vigencia. La consulta de agenda tampoco depende de MVC. Las métricas se calculan en `ConsultaMetricas`; la generación de CSV corresponde a `ExportadorMetricasCsv` y la selección de HTML/JSON/descarga permanece en el controlador.

## Abierto/cerrado — O

`IAccionTramite` declara las acciones que atiende, su permiso y la operación. `RegistroAccionesTramite` construye un registro por clave y rechaza claves vacías, duplicadas o manejadores sin permiso. Ya no hay un selector central de decisiones en el controlador.

Para añadir una acción se implementa el contrato y se registra en `RegistroServicios`. El filtro de permisos consulta la misma declaración; no mantiene otro selector con las acciones del flujo. Si se introduce una capacidad institucional distinta, su definición y administración también deben configurarse expresamente; registrar un manejador no concede un rol nuevo.

Las familias existentes comparten reglas propias: por ejemplo, aprobar, observar o rechazar documentos son variantes de la responsabilidad de revisión documental. Esa agrupación no exige modificar el coordinador de transacciones.

## Sustitución — L

Los consumidores trabajan con contratos: `IDecisionesTramite`, `IAccionTramite`, `IConsultaMetricas`, `IExportadorMetricas`, `IConsultaAgenda`, `IAuthService`, `IPasswordHasher<Usuario>`, `IReloj` y los puertos de datos/archivos.

Un manejador debe validar el actor y la etapa antes de modificar el expediente. Recibe solo acciones declaradas por él, devuelve el texto para el historial y comunica un incumplimiento de negocio mediante `InvalidOperationException`. Si genera un PDF debe anotar su clave en el contexto. No debe confirmar o guardar la transacción: lo hace el coordinador.

Las pruebas sustituyen un manejador, el algoritmo de comprobación de contraseña y el reloj. Verifican tanto el éxito como el rechazo, la renovación del hash, la fecha civil, la confirmación única y la reversión de datos/archivos ante un fallo. Implementar una interfaz sin respetar estas condiciones no basta para cumplir Liskov.

## Segregación de interfaces — I

Los contratos están en `Application/Abstractions`. La consulta pública y las métricas no reciben operaciones de confirmación o transacciones. Autenticación recibe usuarios y confirmación para renovar el hash; el calendario solo necesita el catálogo de feriados. La consulta de agenda y la comprobación de reservas reciben únicamente inspecciones.

Los manejadores reciben los datos que necesitan: requisitos, asignaciones o habilitación. Registro del historial, avisos y alertas tienen contratos distintos. `ILectorArchivos` permite resolver una descarga sin exponer los métodos de guardar o borrar de `IAlmacenPdf`.

El adaptador EF implementa varias interfaces; cada consumidor recibe la suya. Se conserva deliberadamente la API de consultas de EF (`DbSet` y LINQ), por lo que estos puertos no prometen cambiar a cualquier tecnología de persistencia sin adaptar consultas. SOLID no implica por sí solo independencia del ORM ni una arquitectura hexagonal completa.

## Inversión de dependencias — D

Los controladores y servicios refactorizados reciben contratos mediante constructor. `AuthService` y la administración de usuarios reciben `IPasswordHasher<Usuario>`; no crean el algoritmo de contraseña. Las decisiones, las alertas, la agenda y las métricas reciben un reloj reemplazable donde necesitan evaluar el tiempo.

`Infrastructure/RegistroServicios.cs` es el punto de composición. Allí se relacionan interfaces e implementaciones. Todos los puertos de datos de una petición resuelven a la misma instancia scoped de `ApplicationDbContext`, conservando el seguimiento y la atomicidad. El adaptador de transacción está en `Data/ApplicationDbContext.Puertos.cs`.

`TramiteService` permanece como fachada de compatibilidad para consumidores y pruebas anteriores. Delega en los servicios separados; no contiene una segunda copia de las reglas y no está registrado como dependencia del flujo de producción. Las funciones puras de formato, validación y creación de informes permanecen como utilidades; no se crea una interfaz para cada función sin una necesidad de sustitución.

## Comprobaciones y recuperación

- Compilación Release con advertencias tratadas como errores.
- Regresión de los 366 casos de servidor existentes, más 11 comprobaciones nuevas de contratos y formularios de decisión incompletos.
- Evidencia: `outputs/solid-20261026/pruebas/solid-final.trx` y `verificacion.json` en esa carpeta de entrega.
- Punto anterior a la refactorización: `App_Data/checkpoints/20261026-135109-antes-solid/`. Contiene el código, una copia consistente de SQLite, documentos privados e instrucciones de restauración.

Los 183 casos de la campaña del 23/10 conservan sus resultados históricos. Esta refactorización de estructura no certifica los nueve casos manuales pendientes ni cierra los tres bugs funcionales documentados en aquella campaña. Su corrección y reejecución requieren evidencia propia.

Actualización posterior: los tres bugs se corrigieron y verificaron el 26/10/2026 con 389 pruebas de servidor y 28 JavaScript aprobadas. El detalle y la evidencia de navegador están en [CIERRE_BUGS_2026-10-26.md](CIERRE_BUGS_2026-10-26.md). Los pendientes manuales conservan su condición.
