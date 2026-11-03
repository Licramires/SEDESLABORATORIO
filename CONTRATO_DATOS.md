## Recuperación de formularios y ubicación pública — 26/10/2026

Repetir la aprobación de un documento actual ya aprobado con versión vigente no modifica la revisión, la versión del expediente, el historial ni las notificaciones. Una versión obsoleta continúa recibiendo 409. La observación posterior con fundamento sigue permitida.

Las navegaciones HTML de formularios de gestión, administración y registro que obtienen un error textual 400/409 ofrecen recuperación mediante una vista y enlace GET, sin cambiar esos códigos ni reenviar automáticamente el POST. Los clientes sin aceptación de HTML y los endpoints de métricas conservan sus formatos anteriores.

Las ubicaciones persistidas también se verifican al consultar: latitud inclusiva −90…90 y longitud −180…180. Los datos inválidos conservan su ficha y dirección con advertencia; se excluyen del mapa/cálculo de cercanía y no generan un destino de Maps. No se asignan coordenadas inventadas para reparar la consulta.

## Recuperación operativa local — 23/10/2026

Sin cambio de esquema. Una instancia del proyecto mantiene la exclusión de su archivo SQLite mediante `<base>.instance.lock`; no se copia ese archivo al respaldar. Al arrancar, se conservan en `Storage:Root/recuperacion/<lote>/` los archivos con nombres generados que no figuren en referencias persistidas, incluidas las plantillas congeladas. No se borran; si una copia histórica es ilegible, se omite esa recuperación. Las peticiones de guardado con almacenamiento no disponible reciben 503 y los bloqueos de SQLite reciben 409. Las peticiones mal formadas conservan sus códigos correspondientes. La actualización JSON de gráficos limita su espera a 15 s y conserva la última consulta ante un error.

La verificación de esta entrega fue solo local. Ver `docs/CIERRE_PENDIENTES_LOCALES_2026-10-23.md` para evidencias y comprobaciones no ejecutadas.

## Validaciones ampliadas — 22/10/2026 (estado vigente)

Los campos obligatorios de operaciones se exigen en el enlace del formulario y se comprueba ModelState antes de guardar. Latitud/longitud usan rangos decimales exactos. Texto sin controles no permitidos, correo/telefono y longitudes se comprueban en servidor; las fechas civiles no llevan zona horaria y las de citación no llevan hora. Copias JSON con elementos nulos, identificadores repetidos o estructura inválida bloquean el avance.

`POST /Gestion/Servicios` exige `id`, revisión `version` (SHA-256 del texto leído) y `servicios` opcional de hasta 2000 caracteres. La actualización compara también el texto anterior en la misma operación SQL; una edición antigua recibe 409. No hay cambio de esquema. Aprobar una renovación exige laboratorio vinculado, propietario coincidente y vigencia extensible un año. La devolución documental puede reutilizar los motivos individuales; el rechazo definitivo conserva fundamento obligatorio.

Ver `docs/AUDITORIA_EXHAUSTIVA_2026-10-22.md` para alcance, evidencias y límites. Las secciones posteriores conservan el historial donde no contradigan este apartado.

## Cambios de flujos — 22/10/2026 (estado actual)

Novena migración `20261022035220_WorkflowValidationAndActNotes`: `Usuario.Version` concurrente, `Inspeccion.NotasAdicionales` separadas del resumen `Acta`, e índice `(SupervisorId, Fecha)`. El servidor deriva Acta de incumplimientos y notas; conserva los textos históricos. La lista y plantilla siguen siendo copias por inspección. Intervalos de agenda `[inicio, fin)`: visitas contiguas permitidas, cruces bloqueados para estados pendientes/registrados/validados/devueltos; una reprogramación sustituye solo su reserva pendiente.

Edición de datos de apertura únicamente en borrador propio, sin primer envío ni vínculo a un establecimiento. Observación legal exige IDs distintos de documentos actuales del expediente; solo esos archivos pierden conformidad. Rechazo de campo reprogramable cuenta como renovación activa. Las copias JSON ilegibles bloquean avances y se informan; no se reemplazan silenciosamente por un catálogo actual.

No existe clasificación demo/oficial de registros. La carga inicial usa el mismo contrato. Reparación de rótulos heredados idempotente: nombres/tipos/municipios/servicios y renovaciones vinculadas, sin borrar archivos ni reiniciar avances. Las modificaciones anteriores conservan vigencia como historial de implementación donde no contradigan este apartado.

## Supervisor y métricas visuales — 21/10/2026

No cambia el esquema (ocho migraciones). `AgendaSupervisorModel` y `VisitaSupervisor` son datos de presentación con asignaciones pendientes propias; fechas civiles de Bolivia en `yyyy-MM-ddTHH:mm`, sin conversión según la zona del navegador. Las coordenadas se omiten si los datos del expediente no pasan la validación existente. Rutas solo usa visitas que comienzan en el día elegido, ordenadas por horario. Las visitas nocturnas se muestran en todos los días que ocupan. La consulta de actas/citaciones filtra el supervisor autor; el formulario/resumen usa su inspección más reciente, sin tomar la copia de otro supervisor.

`MetricasVisuales` contiene agregados sin contraseñas, correos ni sellos de sesión. `GET /Metricas/Datos` exige gerente y permiso métricas, con caché deshabilitada. Misma cohorte de primer envío y municipio que Index/CSV; para referencias heredadas usa envío o creación. Serie mensual incluye meses con cero. Total = habilitadas + rechazos definitivos + pendientes; los rechazos de campo reprogramables siguen pendientes. Promedio = suma de días naturales de cierres con fechas coherentes / cantidad de esos cierres; sin cierres es null. Sin decisiones, aprobación es null. Los puntos de atención se refieren al estado actual de esa cohorte y expresan sus umbrales. No se persisten resúmenes ni se alteran registros al consultar gráficos.

## Actualización administrativa — 21/10/2026

Octava migración: `20261021184110_RequirementSectionsAndPermissionEditor`. Agrega `SeccionesRequisitos` (nombre, descripción, orden, activa, eliminada y versión), `Requisitos.SeccionId` opcional con borrado restringido, orden y eliminado; versión concurrente en `PermisosRol` y `CambiosPermisos` con autor, fecha UTC y estado otorgado. Las versiones existentes y permisos anteriores se preservan. La baja del catálogo es lógica y no borra PDF ni referencias de documentos. Secciones con requisitos no eliminados no pueden eliminarse; al desactivar una sección sus requisitos conservan su estado individual.

La publicación exige requisito activo y no eliminado y, si tiene sección, sección activa y no eliminada. El registro, descarga pública de plantilla, borradores y datos demo nuevos usan el mismo filtro y orden. La copia `RequisitoCopia` incluye ID, nombre y descripción de sección al primer envío; JSON de expedientes anteriores sigue siendo válido y se agrupa como requisitos generales. Las modificaciones del catálogo nunca reescriben copias enviadas. El editor de permisos solo acepta la lista completa de capacidades válidas del perfil, comprueba las versiones y guarda cambios e historial en una transacción.

# Contrato de datos SI Lab

Actualización del 18 de octubre de 2026, correspondiente al plan aprobado de correcciones. Los nombres originales se conservan y las ampliaciones se versionan mediante migraciones.

## Usuario

`Id`, `Nombre`, `Email` único sin distinguir mayúsculas, `Password` hash, `Rol`, `Activo`, `SelloSeguridad`. Roles: propietario, coordinador, supervisor, abogado, gerente y administrador. Los cambios administrativos revocan la sesión existente. No se desactiva/reasigna el rol de un supervisor con visitas pendientes.

## Laboratorio

`Id`, `Nombre`, `Tipo`, `Lat`, `Lng`, `Estado` abierto/cerrado, `Servicios`, `Municipio`, `PropietarioId`, `VigenciaHasta`, `Direccion`, `Telefono`, `Codigo`, `DatosVerificados`.

El circuito legal controla habilitación, propiedad y vigencia; el propietario solo edita servicios. Los registros anteriores no reciben propietario ni vigencia inventados. Producción publica únicamente registros verificados, con propietario y vigencia; el directorio excluye vigencias vencidas y la ficha las presenta cerradas. Los registros sin validar se identifican por sus datos pendientes de validación, sin clasificarlos por origen.

## Solicitud

Campos originales: `Id`, `PropietarioId`, `DatosLaboratorioPropuesto` JSON, `Estado`, `FechaCreacion`, `FechaEnvio`, `Version`, `LaboratorioId`, `EsRenovacion`, `Municipio`, `FirmaLegalPdf`.

Ampliaciones: `OrigenObservacion` documental/legal/campo, `MotivoRechazo` documental/legal/campo, `RequiereNuevaInspeccion`, `RequisitosJson`, `RequisitosReconstruidos`, `PrimerEnvio`, `FechaCierre`, `VigenciaOtorgadaDesde`, `VigenciaOtorgadaHasta`, `InformeCierrePdf` binario, `Direccion`, `Telefono`, `Codigo`.

El JSON compatible contiene `nombre`, `tipo` y `coordenadas.lat/lng`. La validación oficial exige estructura y tipos correctos, texto no vacío y coordenadas completas dentro de rangos. El lector tolerante usado para mostrar un expediente nunca sustituye esa validación.

Estados persistidos: `borrador`, `en_revision`, `aprobado`, `rechazado`, `inspeccion`, `observado`, `revision_acta`, `revision_legal`, `habilitado`. Solo `habilitado` representa resolución legal positiva. Cada decisión incrementa `Version`, registra historial y avisa al propietario.

`RequisitosJson` guarda al primer envío una lista de `{Id,Nombre,Obligatorio,Version,PlantillaPdf}`. Se conserva en reenvíos del mismo trámite. Los borradores consultan el catálogo activo hasta su primer envío. La referencia reconstruida durante una actualización se identifica con `RequisitosReconstruidos`; no acredita el catálogo histórico original.

El cierre guarda su propia vigencia y PDF, que no cambian por una renovación ni una edición posterior de usuarios/catálogos. Los cierres heredados que no tenían esa información no toman la vigencia actual como sustituto. No se inventa fecha de cierre para calcular métricas.

## Documento y requisito

Documento: `Id`, `SolicitudId`, `RequisitoId`, `ArchivoPdf`, `NombreArchivo`, `NombreRequisito`, `VersionArchivo`, `Actual`, `EstadoRevision`, `Observacion`, `RevisorId`, `FechaRevision`, `FechaCarga` nullable. `NombreArchivo` conserva el nombre visible, sin ruta y hasta 200 caracteres; nunca se usa como clave de almacenamiento. Los anteriores quedan vacíos si no se conoce su nombre original.

Estados normales de revisión: pendiente, aprobado y observado. `aprobacion_global_heredada` identifica que la etapa anterior acreditaba una revisión global sin datos de revisión individual. La migración no le asigna revisor ni fecha falsos.

Reemplazar un requisito crea una versión nueva, conserva las anteriores y reinicia su revisión. Solo la versión actual satisface el requisito. Un documento aprobado requiere una observación antes de reemplazarse. Todos los PDF actuales se revisan individualmente antes de cerrar la revisión documental.

`RequisitoId=0` identifica complementos y no satisface requisitos obligatorios. Se pueden asociar a un requisito conservando el registro original y el archivo. Un complemento pendiente u observado puede retirarse de la presentación actual sin borrar su historia; los obligatorios no se retiran mediante esa acción.

Requisito: `Id`, `Nombre`, `Obligatorio`, `Activo`, `Version`, `PlantillaPdf`. La baja es lógica. La plantilla pública es distinta del PDF privado del propietario. Su edición no cambia el nombre ni la obligatoriedad guardados en expedientes ya enviados.

Los PDF exigen extensión, cabecera, cierre y tamaño máximo de 10 MB. Registro inicial: sin tope de cinco archivos, con 50 MB en conjunto por carga y petición hasta 55 MB. Cada selección del usuario se acumula en una lista donde puede quitar archivos antes de guardar. Puede crear un borrador sin archivos y seguir cargándolos individualmente después, sin límite de cinco en el expediente. Cargas individuales de documento/acta/dictamen/plantilla/evidencia: cuerpo hasta 11 MB. Las peticiones con longitud declarada excesiva responden 413 antes de interpretar el formulario. El servidor conserva sus límites de lectura para cuerpos sin longitud. Las comprobaciones estructurales no son antivirus ni validación criptográfica.

El propietario puede retirar de la lista de envío documentos actuales pendientes u observados, incluso asociados a requisitos, solo mientras el expediente sea editable. Se conserva archivo e historial y se incrementa la versión del trámite; retirar un obligatorio vuelve a dejarlo pendiente y bloquea el envío hasta reponerlo. Los aprobados y expedientes en revisión permanecen protegidos. `RetirarComplementario` mantiene compatibilidad para complementos; `RetirarDocumento` permite ambos casos, con control de propietario, permisos y versión.

## Inspección

`Id`, `SolicitudId`, `SupervisorId`, `Fecha`, `FechaFin`, `Programada`, `ListaVerificacionJson`, `PlantillaPdf`, `PlantillaVersion`, `DiasSubsanacion`, `Estado`, `Resultado`, `Acta`, `ActaPdf`, `Citacion` heredada y `PlazoSubsanacion`.

`Fecha` y `FechaFin` usan hora civil de Bolivia. Con `Programada=false` no existe horario confirmado; el valor técnico de `Fecha` no representa una visita. Las visitas nuevas registran inicio/fin, hasta 12 horas. El fin heredado ausente se interpreta como una hora después del inicio, siguiendo la reserva del contrato anterior.

Estados: pendiente, reprogramada, registrada, validada y rechazada. Reprogramar conserva la visita anterior. Los solapamientos se comprueban contra otras solicitudes del mismo supervisor. Solo el asignado registra el acta y nunca antes del inicio de la visita.

La lista guarda `{Id,Nombre,Referencia,Version,Resultado,Observacion}`. Resultado por punto: cumple, observado o no_aplica. El resultado global puede ser aprobado, observado o rechazado; un punto observado impide resultado aprobado. Preparar contenido no equivale a registrar un acta firmada. La firma se acredita mediante adjunto, sin proveedor criptográfico integrado.

En producción se requieren lista y plantilla institucional disponibles para actas nuevas. Su configuración se copia al asignar una visita; la reprogramación del supervisor conserva la referencia e inicia hallazgos nuevos. Los documentos firmados previos se conservan.

## Citación

`Id` como número interno, `SolicitudId`, `InspeccionId`, `SupervisorId`, `FechaEmision`, `Vencimiento`, `AnticipacionDias`, `Motivo`, `EvidenciaPdf`, `FechaAtencion`.

Por compatibilidad el campo de clave se llama `EvidenciaPdf`, pero admite evidencia PDF, JPG/JPEG o PNG. Se comprueban extensión, firmas estructurales y tamaño de hasta 10 MB; se descarga con el tipo correspondiente y autorización del expediente. No se sirve desde una ruta estática.

La citación requiere la última inspección propia con incumplimientos y no permite duplicar una citación pendiente para esa visita. No se crean citaciones sobre un trámite cerrado. Una inspección conforme validada atiende las citaciones previas. El día de vencimiento se incluye completo.

Los avisos próximo/vencido se deduplican por citación/plazo, fecha, fase y destinatario. Se avisa al propietario y al supervisor. No se ejecutan multas ni sanciones automáticas.

## Catálogo institucional

CatalogoItem: `Id`, `Clase`, `Nombre`, `Referencia`, `Activo`, `Version`, `ArchivoPdf`. Clases: tipo, municipio, inspeccion, feriado, formulario_acta y formulario_citacion. Nombre único por clase, sin distinguir mayúsculas. No cambia de clase un registro existente. Hay como máximo una plantilla activa por tipo de formulario desde la administración.

Los feriados usan fecha `yyyy-MM-dd` y se aplican junto a fines de semana al crear un plazo. La institución carga sus valores: no se inventan tipos, artículos ni feriados. Producción exige catálogos de tipo/municipio para nuevas aperturas; en desarrollo se admite texto libre solo mientras no haya catálogo de esa clase. Un catálogo existente siempre valida contra elementos activos.

## Historial, notificación y permisos

HistorialTramite: `Id`, `SolicitudId`, `ActorId`, `Fecha` UTC, `Detalle`. No hay interfaz para alterar sus entradas. Incluye revisiones de versiones, asociaciones, retiradas, programación y decisiones.

Notificacion: `Id`, `UsuarioId`, `Mensaje`, `Fecha` UTC, `Leida`, `Clave` opcional única. Solo su destinatario consulta/confirma lectura. Los avisos son internos, sin correo/SMS.

PermisoRol: clave compuesta Rol/Clave, campo Habilitado. Ausencia equivale al permiso del catálogo activo. Los permisos suspenden funciones de un perfil; no conceden atribuciones de otro. Las nuevas acciones de revisión, preparación de acta, citación y complementos tienen controles de permiso.

## Migración y respaldo

Se conservan las cuatro migraciones anteriores. `AuditCorrections` agrega el esquema y `InspectionFormSnapshot` agrega datos de formulario y reconstruye referencias disponibles después de la reconstrucción de tablas SQLite.

Antes de aplicar el esquema se respalda SQLite con su mecanismo de copia consistente y, si existen archivos, se respalda el almacenamiento privado. Los PDF antiguos de uploads siguen descargándose con autorización. No transportar datos ni copias mediante Git. La prueba de actualización desde Sprint 1 y desde la versión operativa de octubre verifica la conservación de registros/documentos.
