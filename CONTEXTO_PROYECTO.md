## Estado vigente — cierre de los tres bugs del 26/10/2026

BUG-01 resuelto mediante `RecuperacionOperacionFilter`: las navegaciones HTML de formularios reciben una vista con menú y enlace GET seguro al recibir 400/409. Los clientes HTTP y métricas conservan sus formatos. BUG-02: aprobar de nuevo el mismo PDF ya conforme es idempotente y el botón desaparece; observar sigue permitido. BUG-03: se validan coordenadas persistidas antes de publicar marcadores, calcular el más cercano o generar rutas; las fichas inválidas conservan dirección y aviso sin inventar datos.

389 pruebas .NET y 28 JavaScript aprobadas. Navegador real sobre una copia desechable: validación vacía, conflicto de dos pestañas, regreso al expediente, aprobación documental y ruta inválida verificados. Base original y sus 54 archivos privados idénticos al punto previo `App_Data/checkpoints/20261026-144304-antes-bugs-pendientes/`; sin migración ni producción. Ver `docs/CIERRE_BUGS_2026-10-26.md` y `outputs/bugs-20261026/verificacion.json`.

Los tres bugs están cerrados; los nueve casos manuales/parciales no se certifican por estas correcciones. Los Excel del 23/10 conservan sus resultados históricos. PUB-EXT-09 supera la reejecución dirigida; los errores de E2E-EXT-04 están corregidos, pero no se declara completado todo su inventario de controles.

## Refactorización SOLID del 26/10/2026 — estado anterior

Las decisiones se ejecutan mediante `IDecisionesTramite` e implementaciones de `IAccionTramite`, con registro extensible y permisos declarados por manejador. Contratos de datos y archivos en `Application/Abstractions`; todas las interfaces de datos de una petición comparten la misma unidad de trabajo EF. Calendario, historial, avisos, alertas, reservas, agenda, consulta de métricas y exportación CSV están separados. Autenticación y administración reciben `IPasswordHasher<Usuario>`. No añadir nuevas decisiones al controlador ni duplicar su selección en el filtro de permisos.

377 pruebas .NET aprobadas: las 366 anteriores y 11 nuevas de extensión, sustitución, permisos de precondición, reloj, hash, transacción y formularios incompletos. Compilación Release con cero advertencias/errores. Base original y 54 archivos privados preservados; cero vistas/estilos/scripts modificados y ninguna migración. Punto previo: `App_Data/checkpoints/20261026-135109-antes-solid/`. Ver `docs/ARQUITECTURA_SOLID.md` y `outputs/solid-20261026/verificacion.json`.

Los Excel de la campaña del 23/10 son registros históricos: 172 exitosos, 2 fallidos, 9 bloqueados y 3 bugs. La refactorización SOLID no ha corregido ni cerrado esos bugs ni completa los pendientes manuales. Producción sigue excluida.

## Pruebas locales del 23/10/2026 — estado anterior

366 pruebas .NET y 28 JavaScript aprobadas. Restauración reconstruida desde el respaldo y arrancada, con sus 360 pruebas originales aprobadas. Ensayos reales locales de concurrencia, cortes de carga, reinicio inesperado, almacenamiento no disponible, caída entre archivo y confirmación y bloqueo de SQLite. Correcciones de recuperación privada de archivos, exclusión de instancia, respuestas 503/409 y espera limitada de actualización JSON. Sin migración ni cambios en la base original o sus 47 archivos privados.

Producción excluida por indicación del usuario. La ampliación de interfaz quedó detenida al no poder verificar la URL desde el control de Windows; GPS físico, otros dispositivos y contenido institucional pendiente no están certificados. Informe vigente: [Pendientes locales](docs/CIERRE_PENDIENTES_LOCALES_2026-10-23.md). Punto previo: `App_Data/checkpoints/20261022-234400-antes-cierre-pendientes/`. Los apartados siguientes conservan las revisiones anteriores.

## Auditoría ampliada — 22/10/2026 (revisión anterior)

360 pruebas .NET y 6 JavaScript aprobadas. Inventario de 69 acciones: 301 consultas por perfil, 603 enlaces, 318 comprobaciones de seguridad POST, 685 transiciones prohibidas, 17 permitidas y sus reenvíos, 228 casos de campos inválidos y los 11 permisos configurables. Navegación real con los seis perfiles y el portal público; correcciones de formularios, calendario, concurrencia de servicios y consistencia de renovaciones. Base original idéntica al respaldo y 47 archivos privados conservados, sin nueva migración. Siguen pendientes propietario/vigencia de tres laboratorios heredados y los contenidos institucionales definitivos.

Informe y evidencia: [Auditoría ampliada](docs/AUDITORIA_EXHAUSTIVA_2026-10-22.md). Punto previo: `App_Data/checkpoints/20261022-223121-antes-auditoria-exhaustiva/`. Las verificaciones siguientes describen etapas anteriores.

Estado actual del 22/10/2026: auditoría transversal de flujos terminada con 91 pruebas de servidor y 4 de interfaz. Agenda bloquea intervalos solapados incluso si ya hay acta; acta React deriva su resumen sin duplicar hallazgos; revisión legal selecciona documentos; propietario corrige datos antes del primer envío; usuario administrativo usa versión. Navegación centrada en tareas, avanzadas agrupadas. Misma base y mismas reglas para datos iniciales y los demás registros, sin clasificación demo/oficial. Rótulos iniciales y renovaciones corregidos; cuatro horarios históricos distribuidos con autorización y anotación en historial. Novena migración. Ver `docs/AUDITORIA_FLUJOS_2026-10-22.md` y `VERIFICACION.md`. Respaldo: `App_Data/checkpoints/20261021-233744-antes-auditoria-flujos`. Los párrafos fechados posteriores describen etapas anteriores.

Actualización supervisor/gerencia del 21/10/2026: React local en agenda/rutas y gráficos de métricas, manteniendo .NET MVC, cookies, versiones y decisiones en servidor. Supervisor usa vista `Inspeccion` con recorrido programación → contenido del acta → PDF firmado → revisión; actas y citaciones se listan por supervisor. Métricas visuales se calculan con los registros guardados y los mismos filtros del CSV. `GET /Metricas/Datos` entrega solo agregados autorizados, sin entidades de usuarios. Sin nuevas migraciones. Ver README para compilar React y `docs/supervisor-react-20261021/` para evidencia. Punto previo: `App_Data/checkpoints/20261021-150038-antes-supervisor-react-metricas`.

Actualización administrativa del 21/10/2026: editor por rol, matriz de funciones y registro de cambios; secciones y gestión completa de requisitos con orden, obligatoriedad, estado y eliminación lógica distinta de desactivación. La consulta pública, el registro y los borradores comparten el catálogo publicado; el expediente enviado conserva su copia con sección y descripción. Ocho migraciones y 53 pruebas. Respaldo previo: `App_Data/checkpoints/20261021-143538-antes-admin-secciones`. Se conserva el diseño oscuro de DESIGN.md. Catálogos y validación de datos conservan su ubicación actual; no se retiraron por la consulta del usuario.

# Contexto actual de SI Lab

Actualización del 21/10/2026: el usuario estableció DESIGN.md como autoridad visual principal. El layout carga `design-theme.css` en lugar del tema Figma anterior. Se aplican fondo casi negro, superficies carbón, acento lavanda, jerarquía tipográfica y controles accesibles sin cambiar los flujos ni la base. Punto previo recuperable: `App_Data/checkpoints/20261021-135546-antes-impeccable`. Las referencias visuales del 18/10 que siguen describen la versión anterior.

Actualizado el 18 de octubre de 2026. El usuario aprobó el plan de `docs/AUDITORIA_Y_PLAN_2026-10-18.md` y se aplicaron las correcciones locales. No se realizó commit, push ni despliegue. El resultado de verificación y los pendientes externos están en `VERIFICACION.md` y `docs/CIERRE_CORRECCIONES_2026-10-18.md`.

## Alcance

La presentación se ajustó a las exportaciones de `Agent-resources/FigmaSedes`: tema común en `wwwroot/css/figma-theme.css`, iconos SVG locales, navegación activa, menú móvil, formulario por tarjetas y resúmenes derivados de los datos disponibles. El punto de control anterior está en `App_Data/checkpoints/20261018-194800-antes-estilo-figma`. No se modificaron los flujos ni los datos locales durante esta actualización visual; se conservaron las 46 pruebas aprobadas. La guía y las capturas están en `ESTILO_VISUAL.md` y `docs/estilo-figma/`.

ASP.NET Core MVC .NET 8, EF Core 8.0.20 y SQLite. Se conservan los módulos originales `auth/`, `publico/`, `solicitud-registro/` y `solicitud-seguimiento/`. La operación integrada usa los controladores de `Controllers/`, vistas compartidas y servicios de `Infrastructure/`.

Hay seis perfiles: propietario, coordinador, supervisor, abogado, gerente y administrador. Las restricciones por rol, pertenencia/asignación y permisos se aplican en el servidor. No existen roles arbitrarios que puedan ampliar atribuciones legales.

La correspondencia individual de CU01–CU32 y CU-P1–CU-P4 se conserva en `docs/CASOS_DE_USO.md`. No confundir implementación técnica con validación institucional: todavía deben cargarse los catálogos, formularios y fuentes oficiales.

## Circuito del trámite

1. El propietario guarda un borrador con los datos del establecimiento y adjunta PDF. Los archivos iniciales pueden asociarse a requisitos sin volver a subirlos.
2. Al primer envío se guarda una copia de los requisitos y sus versiones. Esa referencia se conserva durante la subsanación; los cambios posteriores del catálogo se aplican a trámites nuevos.
3. El coordinador revisa cada documento actual. Puede aprobarlo u observarlo con fundamento. Solo cierra la documentación como aprobada cuando todos los documentos actuales están aprobados y existen los archivos obligatorios.
4. Una devolución documental permite corregir documentos observados. El rechazo definitivo es una acción diferente y obliga a iniciar otro trámite.
5. El coordinador asigna al supervisor y puede indicar un intervalo o dejar la programación al supervisor. Este organiza inicio/fin de la visita y el propietario recibe aviso. Se comprueban solapamientos contra otras visitas y se conserva cada asignación anterior.
6. A partir del inicio programado, el supervisor prepara el contenido estructurado del acta, descarga el formulario/resumen y adjunta la versión firmada. Los puntos de verificación y la plantilla se conservan por inspección. Producción requiere configuración institucional; desarrollo permite ensayos sin ella y lo indica.
7. El coordinador acepta el resultado o devuelve el acta para otra inspección. Un incumplimiento de campo puede reprogramarse, pero una observación documental o legal no puede saltar su revisión.
8. El abogado aprueba con PDF firmado, devuelve con observaciones o rechaza definitivamente. Una observación legal vuelve a revisión documental y después a legal sin repetir una visita conforme, salvo que el abogado marque que hace falta otra inspección.
9. Solo la aprobación legal habilita el laboratorio. Se validan datos oficiales, documentación e inspección; se guarda la vigencia otorgada y un PDF inmutable del cierre.
10. La renovación utiliza el mismo circuito y conserva el establecimiento. Si la vigencia sigue activa, el año se suma desde su fin; si venció, desde la aprobación. Los duplicados activos se bloquean. La ventana anticipada es configurable y, por defecto, no añade una restricción nueva.

Estados: `borrador`, `en_revision`, `aprobado`, `inspeccion`, `revision_acta`, `observado`, `rechazado`, `revision_legal`, `habilitado`. `aprobado` representa documentación conforme, no habilitación. El origen de observación y el motivo de rechazo distinguen los retornos permitidos.

## Documentos e historial

- Los PDF y las imágenes de citación se almacenan de forma privada, con claves aleatorias, y se descargan previa autorización. `/uploads` permanece bloqueado.
- Cada reemplazo conserva el registro y el archivo anteriores, su decisión, motivo, revisor y fecha. Los aprobados requieren una observación antes de reemplazarse.
- El propietario puede retirar un complemento pendiente u observado de la presentación actual sin borrar su historial. No puede retirar requisitos obligatorios mediante esa acción.
- Las plantillas públicas son archivos del catálogo, distintos de los documentos privados del expediente. Desactivar un requisito impide su publicación actual sin cambiar la copia de los expedientes recibidos.
- Los datos de presentación tolerantes a JSON dañado nunca se usan para autorizar una habilitación.
- Los informes nuevos se guardan al cerrar. Los cierres heredados sin vigencia histórica conservan esa ausencia y se identifican como reconstruidos; no toman la vigencia actual del laboratorio como si fuera la del cierre original.

## Citaciones, plazos y avisos

Las citaciones tienen número interno, inspección, supervisor, emisión, motivo, vencimiento, anticipación, evidencia PDF/JPG/PNG y fecha de atención. Los avisos de plazo próximo/vencido llegan al propietario y supervisor sin duplicarse. Una inspección conforme validada atiende las citaciones anteriores.

El proceso horario comprueba vigencias, subsanaciones y citaciones. Los plazos de subsanación cuentan lunes a viernes y excluyen los feriados configurados por el administrador; no se inventa un calendario oficial. Un vencimiento de plazo no aplica multas ni rechazos automáticos.

Configuración opcional:

- `Alertas:LicenciaDias`: anticipación del aviso de licencia; predeterminado 30, rango efectivo 0–365.
- `Alertas:SubsanacionDias`: anticipación de subsanación; predeterminado 5, rango efectivo 0–30.
- Cada citación fija su propia anticipación de 0–30 días naturales.
- `Tramites:RenovacionDiasAnticipacion`: si se establece, limita la ventana previa de renovación a 0–365 días. Sin valor se conserva la renovación anticipada del contrato original.

La agenda y las fechas de citación usan hora civil de Bolivia. Los plazos de día completo vencen al terminar ese día. Registro, primer envío, cierre y métricas usan UTC y las pantallas lo indican.

## Administración y consulta pública

El administrador gestiona usuarios, permisos, requisitos con plantillas, tipos, municipios, puntos de inspección, feriados y formularios de acta/citación. Las bajas son lógicas, y requisitos/catálogos usan control de versión. La edición de usuarios revoca sesiones y no permite desactivar un supervisor con visitas pendientes.

En producción los catálogos de tipo/municipio y los requisitos deben configurarse antes de recibir nuevas aperturas. Una instalación nueva no crea requisitos de demostración. En desarrollo se admite captura libre mientras esos catálogos estén vacíos, para no inventar sus ocho tipos oficiales.

La landing publica requisitos y sus formularios. Las fichas tienen mapa, dirección, contacto, código y servicios cuando se dispone de esos datos. La ruta ofrece origen manual en Maps si se deniega geolocalización. Leaflet y Maps requieren conexión; no se utiliza la ubicación personal del usuario en las pruebas.

Los establecimientos sin datos validados no se publican en producción ni renuevan automáticamente. Los datos faltantes se señalan como pendientes de validación, sin clasificación por origen. La pantalla de validación de datos enumera inconsistencias; no asigna propietarios ni vigencias por suposición. Los registros heredados deben contrastarse con la institución.

El propietario consulta sus expedientes y versiones desde cada establecimiento. Solo puede editar servicios; los datos oficiales siguen el circuito autorizado.

## Métricas

El período y municipio seleccionan trámites por primer envío, con referencia a fecha de envío/creación únicamente para registros heredados sin ese dato. Los borradores se cuentan aparte por creación. El estado mostrado es el actual, no una reconstrucción del estado al final del período.

Se muestran aperturas/renovaciones, habilitados, rechazos definitivos, pendientes, promedio de días hasta cierre, tasa de aprobación, rendimiento por supervisor y últimos 50 movimientos. La tasa usa habilitados / (habilitados + rechazos definitivos); los rechazos de campo reprogramables continúan pendientes. Los cierres heredados sin fecha no participan del promedio. El CSV contiene el mismo conjunto y protege sus textos contra fórmulas.

## Datos, migraciones y continuidad

Hay siete migraciones: las cuatro anteriores, `AuditCorrections`, `InspectionFormSnapshot` y `DocumentOriginalName`. Agregan información sin borrar registros; distinguen aprobaciones globales heredadas de revisiones individuales y no inventan fechas. La última conserva el nombre visible de los PDF nuevos sin cambiar las claves privadas ni inventar nombres anteriores.

La base local se respaldó antes de actualizar el esquema. Usuarios, laboratorios, requisitos y borrador existentes se conservaron. Los ejemplos de la auditoría usados en navegador estaban en una copia separada. Posteriormente, por solicitud del usuario, se agregó una carga inicial de desarrollo con 13 expedientes DEMO, 39 documentos de ejemplo (con los tres requisitos actuales), dos laboratorios nuevos del propietario, inspecciones, citación, historial y avisos para los seis perfiles. Los tres laboratorios heredados siguen sin propietario/vigencia y pendientes de fuente institucional.

La carga se activa solo en Development con `Auth:SeedDemoUsers=true` y `Auth:SeedDemoData=true`. Completa cuentas faltantes sin restablecer las existentes; conserva catálogos configurados y solo agrega ejemplos en clases vacías. El marcador persistente `demo:expedientes:v1` evita repetir escenarios o reiniciar avances. Las fechas se fijan al primer arranque; los PDF y formularios se identifican sin validez oficial. La base local se respaldó de nuevo en `App_Data/backups/20261018-191657-antes-demo` antes de cargar los escenarios. En instalaciones nuevas, esta carga sustituye los tres laboratorios incompletos del inicializador anterior por dos laboratorios vinculados al propietario y con vigencia de prueba.

Antes de cualquier actualización real, respaldar conjuntamente base y almacenamiento privado. SQLite y el proceso de avisos están previstos para una instancia. No incluir bases, archivos cargados, copias de seguridad, herramientas, bin ni obj en Git. No realizar push ni publicar sin autorización explícita.
