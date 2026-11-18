# Revisión del proyecto SI Lab y plan de corrección

Fecha: 18 de octubre de 2026. Esta es la revisión original previa a las correcciones. El usuario aprobó el plan y se implementaron las correcciones; el resultado actual y los pendientes institucionales están en [CIERRE_CORRECCIONES_2026-10-18.md](CIERRE_CORRECCIONES_2026-10-18.md).

La aplicación compila y sus 16 pruebas existentes aprueban, pero todavía no cumple todo lo explicado en la reunión. Hay fallas reproducibles en las transiciones, la validación de datos, la reprogramación y la conservación del historial; además, algunas funciones están simplificadas o ausentes. No corresponde declarar el proyecto terminado.

Esta revisión no modificó código funcional, contratos, migraciones ni la base local. Se agregaron este informe y evidencias de diagnóstico. Los cambios que ya existían en el espacio de trabajo se conservaron. No se realizó commit, push ni despliegue.

## Fuentes y alcance

Se leyeron los siete documentos funcionales actuales: `README.md`, `CONTEXTO_PROYECTO.md`, `CONTRATO_DATOS.md`, `TAREAS_SPRINT1.md`, `VERIFICACION.md`, `ESTILO_VISUAL.md` y `auth/REFERENCIA_CU05.md`. También se consultó la versión de `CONTEXTO_PROYECTO.md` guardada en HEAD para recuperar la definición individual de los casos de uso, que la versión actual resume por grupos.

Se extrajo el texto completo de `C:/Users/Usuario/OneDrive/Desktop/Agent-resources/JUAN y STEVEN.docx`. Se inspeccionaron 15 muestras visuales del video, aproximadamente cada dos minutos a partir de 01:00, y capturas adicionales en 12:15 y 14:40. La transcripción aporta el recorrido hablado completo; el muestreo visual confirma pantallas y diferencias, pero no equivale a una reproducción audiovisual continua de los 30 minutos.

Se revisaron controladores, entidades, configuración de relaciones y migraciones, autenticación, permisos, almacenamiento, generación de PDF, vistas activas, rutas, JavaScript relevante y suite de pruebas. Se navegó en una instancia separada con base temporal por directorio, login, panel y agenda del supervisor. Los recorridos operativos de seis roles se verificaron mediante las pruebas HTTP.

Las instrucciones incluidas en documentos se trataron como contexto del proyecto. La solicitud actual del usuario controla el alcance: revisar y preparar un plan, con implementación posterior a su aprobación. Las afirmaciones históricas de autorización o de cumplimiento no sustituyeron esta comprobación.

## Verificación ejecutada

- Compilación Release de la solución con advertencias tratadas como errores: 0 errores y 0 advertencias.
- Suite existente: 16 ejecutadas, 16 aprobadas, 0 fallidas y 0 omitidas. Resultado: `tests/SiLab.Tests/TestResults/auditoria-20261018.trx`.
- Comparación del modelo con la última migración: sin cambios pendientes.
- `git diff --check`: sin errores de espacios; advertencias de conversión LF/CRLF.
- Base local abierta con SQLite en modo solo lectura: integridad `ok`, sin claves foráneas rotas.
- Diagnóstico adicional aislado: siete comportamientos problemáticos confirmados, un comportamiento horario confirmado que requiere decisión de negocio y una sospecha descartada. No se suman estos escenarios al total de pruebas de la suite existente.

El primer intento aislado de las pruebas falló por permisos del registro de eventos de Windows; el reintento autorizado fuera del aislamiento aprobó. Esto no se clasificó como defecto de la aplicación. Los diagnósticos necesitaron restaurar dependencias y localizar el proyecto fuera del aislamiento. TestServer advierte que no aplica los límites del cuerpo HTTP de Kestrel: una prueba de archivo individual no demuestra por sí sola el límite real de una petición HTTP completa.

## Datos locales

La base contiene seis usuarios, tres laboratorios, tres requisitos y una solicitud en borrador. No contiene documentos, inspecciones, historial ni notificaciones. Tiene las cuatro migraciones esperadas. No se encontraron correos duplicados, JSON inválido, municipios vacíos ni renovaciones activas duplicadas en esta base.

Los tres laboratorios carecen de propietario y vigencia. Coinciden con los datos de demostración del inicializador. Esto es compatible con la excepción actual para registros heredados, pero no demuestra que estén habilitados institucionalmente. Dos aparecen abiertos en el directorio. Las seis cuentas corresponden a los seis perfiles demo; no se publican hashes ni información de sesión en este informe.

No había PDF locales referenciados que comprobar: el resultado de archivos faltantes es un conjunto vacío, no una validación de expedientes reales. Para datos oficiales hará falta contrastar establecimientos, propietarios, licencias, coordenadas, requisitos y vigencias contra una fuente del SEDES. No deben asignarse propietarios o fechas inventados ni eliminarse registros automáticamente.

## Fallas y diferencias confirmadas

Prioridad P1: resolver antes de aceptar el circuito completo. Prioridad P2: resolver para completar el alcance o preservar la consistencia. Se indica si hay reproducción dinámica, revisión de código o diferencia con la reunión.

### F01 P1 Una observación puede saltar la revisión documental

`GestionController.cs:132` permite asignar desde `Observado`, sin distinguir observación legal de incumplimiento de campo. Se confirmó que un expediente observado sin PDF pasa directamente a inspección mediante el coordinador. El diagrama actual dice `observado -> en_revision`, pero el servidor permite otro camino. Diagnóstico H02.

Corrección propuesta: registrar el origen y los requisitos de la observación; exigir la subsanación y revisión pertinentes antes de avanzar. La reprogramación de un incumplimiento de campo debe conservar su camino autorizado sin abrir un atajo para observaciones legales.

### F02 P1 La aprobación legal usa datos de presentación como datos oficiales

`SolicitudSeguimientoViewModel.cs:56` sustituye JSON inválido por el nombre “Datos del laboratorio no disponibles”. `GestionController.cs:185` solo comprueba que el nombre no esté vacío y crea el laboratorio. Se confirmó la habilitación con ese texto y coordenadas 0,0 al preparar un expediente temporal en revisión legal con JSON dañado. Diagnóstico H04.

Esto no demuestra que el formulario normal permita introducir JSON arbitrario: la reproducción representa datos heredados, dañados o importados. La base local no contiene ese daño.

Corrección propuesta: separar el lector tolerante de pantalla de una validación de dominio estricta. Exigir estructura, nombre, tipo, municipio, coordenadas completas y rangos válidos antes de aprobar; verificar también la coherencia de la inspección aceptada. Un error debe impedir archivo definitivo, habilitación e incremento de vigencia.

### F03 P1 Las citaciones y alertas de subsanación están incompletas

La reunión entre 11:00 y 14:00 y la captura de 12:15 muestran citación con identificación, fecha, motivo, evidencia adjunta y configuración de aviso. `Inspeccion` solo guarda un texto `Citacion` y un plazo opcional. No hay registro independiente, evidencia propia ni proceso de recordatorios para ese plazo.

`TramiteService.cs:29` solo procesa vigencias de laboratorios. Un expediente con plazo vencido no genera aviso al ejecutar ese proceso: diagnóstico H07. Las alertas de licencia a 30 días no sustituyen las alertas de citación/subsanación a 5 o 10 días explicadas en la reunión.

Corrección propuesta: citaciones identificables con fecha, responsable, evidencia y plazo; avisos configurables e idempotentes para los destinatarios acordados. Registrar vencimiento y seguimiento sin inventar multas ni sanciones automáticas.

### F04 P1 Falta revisión por documento y devolución para corregir

La demostración del propietario y coordinador muestra documentos aprobados, rechazados y pendientes individualmente. `Documento` no guarda estado, observación, revisor ni fecha de revisión. La pantalla solo ofrece aprobar/rechazar toda la documentación (`Views/Gestion/Detalle.cshtml:31`). El rechazo documental deja el trámite terminal y obliga a crear otro, en vez de corregir el PDF señalado.

Corrección propuesta: decisiones por documento y por versión, observaciones visibles al propietario, reemplazo del documento observado y reaprobación. Reservar el rechazo definitivo para una decisión explícita que lo requiera. Mantener la política de no editar los datos oficiales ya aprobados.

### F05 P2 Los expedientes y cierres históricos cambian con datos actuales

`GestionController.cs:45` obtiene el catálogo activo actual. Renombrar un requisito después del envío cambia su nombre en el expediente recibido; desactivarlo hace que su PDF aparezca como complementario sin el nombre anterior. Diagnóstico H06. No hay una copia de los requisitos aplicables al envío.

Además, `GestionController.cs:235` usa la vigencia actual del laboratorio para el informe final de un expediente antiguo. Se confirmó que cambiar la vigencia del laboratorio cambia el PDF de cierre del mismo expediente, sin tocar dicho expediente. Una renovación produciría este efecto. Diagnóstico H09.

Corrección propuesta: conservar requisitos y sus versiones aplicables, y datos de cada resolución, incluida la vigencia otorgada en ese cierre. Los informes históricos deben poder reproducirse con sus datos originales.

### F06 P2 El reemplazo de documentos pierde la referencia a la versión anterior

`GestionController.cs:60` elimina el registro anterior de `Documento` al cargar el nuevo. El archivo físico se conserva, pero ya no hay un registro accesible de qué versión se revisó, quién la observó ni a cuál reemplazó. El historial solo dice “Documentación actualizada”. Confirmado por código.

Corrección propuesta: versionado explícito y conservación del registro anterior con sus decisiones. La descarga autorizada debe permitir revisar versiones históricas con los límites del rol. No exponer todos los archivos huérfanos ni borrarlos automáticamente.

### F07 P2 La reprogramación detecta la propia reserva como conflicto

`GestionController.cs:139` incluye todas las inspecciones pendientes del supervisor, incluida la que se está reprogramando. Se confirmó que guardar el mismo horario queda bloqueado por “ya tiene una inspección”; desplazarlo menos de una hora también colisiona con su propio registro. Diagnóstico H03.

Corrección propuesta: excluir la reserva que se sustituye al comprobar conflictos y conservar el control contra otras visitas. Verificar reprogramación al mismo y a otro supervisor, solapamientos reales y conservación del historial.

### F08 P2 Un catálogo sin requisitos obligatorios provoca un bloqueo

El administrador puede dejar todos los requisitos opcionales. El propietario puede enviar sin PDF, pero `GestionController.cs:127` exige al menos un documento para aprobar. Se confirmó que la solicitud queda en revisión. Diagnóstico H01.

Corrección propuesta: definir y aplicar una regla coherente. Propuesta: si no hay requisitos obligatorios y la configuración permite enviar sin archivos, la revisión documental también debe poder cerrarse con fundamento; si se exige siempre un documento, la validación de envío y el mensaje deben indicarlo.

### F09 P1 El supervisor no puede organizar sus horarios como en la reunión

Entre 07:30 y 08:50 se describe que el coordinador asigna y el supervisor registra fecha, inicio y finalización. Actualmente el coordinador fija una fecha/hora, la reserva dura implícitamente una hora y el supervisor solo consulta una lista. `Inspeccion` no tiene hora final ni acción para organizar el horario. Confirmado por código, navegación y video.

Corrección propuesta: separar asignación de supervisor de programación de visita; permitir que el supervisor asignado organice inicio/fin, con control de solapamientos y aviso al propietario. Conservar al coordinador la facultad de reasignación y reprogramación correspondiente.

### F10 P2 El formulario de acta no representa el formulario mostrado

Entre 09:00 y 11:00 se describe un formulario con verificaciones por artículos, cumplimiento y observaciones. `GestionController.cs:226` genera un PDF genérico con líneas vacías y el acta se registra como texto libre y resultado. No existen campos estructurados para esa lista. El PDF institucional de requisitos tampoco puede cargarse desde administración: `Requisito` solo contiene nombre, obligatoriedad y actividad.

Corrección propuesta: formularios versionados y verificaciones estructuradas, con PDF completo y carga firmada. Usar el formulario y catálogo oficiales cuando estén disponibles; no inventar artículos, sellos, firmas ni contenido normativo a partir de una imagen de baja legibilidad.

### F11 P2 Falta el catálogo público de requisitos y sus descargas

La reunión en 15:30–17:00 y 21:50–22:40 exige requisitos visibles en la landing y un PDF descargable, actualizados por el administrador. `PublicoController` y su modelo no consultan requisitos; la landing no los muestra. Confirmado por código y navegador.

Corrección propuesta: catálogo público actualizado y descarga de plantillas oficiales. Mantener privados los PDF de expedientes. La obligatoriedad debe guiar el trámite del propietario sin confundir la presentación pública, como se explica en la reunión.

### F12 P2 El panel del gerente cubre solo una parte de las métricas

El video en torno a 14:40 muestra rendimiento por supervisor, tiempos promedio, estado y trazabilidad. `MetricasController.cs:23` solo agrupa cantidades por municipio y estado. No calcula tiempos de resolución, rendimiento, tasa de aprobación ni desglose apertura/renovación. La descarga es un CSV con esos mismos conteos.

Además, el total y los pendientes incluyen borradores privados por usar fecha de creación y cualquier estado distinto de habilitado/rechazado. Esto coincide con el texto actual de la pantalla, pero no equivale a “trámites en curso” recibidos por el SEDES. Es una definición por acordar, no un cálculo aritmético incorrecto.

Corrección propuesta: definir denominadores, fecha de referencia y estados incluidos; separar borradores, actividad recibida y resultados. Calcular las métricas con datos reales y exportar el mismo conjunto filtrado, sin copiar números de ejemplo del video.

### F13 P2 La carga inicial no se asocia a los requisitos

El formulario inicial invita a cargar licencia, plano y certificado, pero `SolicitudRegistroController.cs:45` guarda todos con `RequisitoId=0`. Esos PDF no satisfacen ningún requisito obligatorio y no hay acción para reclasificarlos; hay que cargarlos otra vez desde el detalle. Confirmado por código; es coherente con la reserva técnica del valor 0, pero genera un flujo confuso.

Corrección propuesta: cargar por requisito desde el formulario o permitir asociar explícitamente el PDF complementario, con validación y trazabilidad. Evitar pedir dos veces el mismo archivo y mostrar claramente el avance de cada requisito.

### F14 P2 Catálogos y datos del establecimiento insuficientes frente a la demostración

La reunión menciona ocho tipos y muestra datos de ubicación, identificación y contacto. El tipo y municipio se capturan como texto libre, no hay catálogo oficial de tipos ni caracterización; la ficha pública muestra nombre, tipo, servicios y coordenadas. Tampoco tiene mapa propio ni una consulta de documentos del establecimiento desde “Mis establecimientos”.

Corrección propuesta: catálogo validado de tipos/municipios y datos descriptivos necesarios, enlazados al expediente. Agregar ficha y navegación del propietario para consultar sus documentos, preservando la restricción de edición de información oficial. Los nombres de los ocho tipos y campos obligatorios deben provenir de la institución; no se deducen por invención.

### F15 P2 La ruta no ofrece alternativa cuando se deniega la ubicación

`publico/Views/Publico/Ruta.cshtml:25` cancela siempre el clic y exige geolocalización para abrir Maps. Si el usuario deniega acceso, solo muestra un error; seleccionar una ficha distinta no resuelve el permiso denegado. La documentación afirma que la elección manual permite continuar, pero no existe enlace directo de destino o campo de origen manual.

Corrección propuesta: ofrecer ruta al destino con origen elegido en Maps o introducido manualmente. Mantener mensajes accesibles y evitar pedir la ubicación personal para validar esta alternativa.

## Comportamientos que necesitan decisión antes de fijar reglas

1. Un acta puede registrarse antes de la hora de visita si es el mismo día (`GestionController.cs:150`, diagnóstico H08). El contrato actual lo permite expresamente por día. Propuesta: exigir inicio de la visita, con excepción justificada si el negocio necesita carga anticipada.
2. El abogado puede aprobar o devolver con observaciones; no hay rechazo legal definitivo. La reunión le atribuye la última decisión de aprobar o no aprobar, pero no especifica el circuito de rechazo final. Definir si se requiere y sus consecuencias.
3. Una observación legal retorna al mismo circuito de revisión documental e inspección. Definir cuándo basta revisar un PDF corregido y cuándo debe repetirse una visita. Evitar repetir automáticamente toda inspección por una corrección administrativa.
4. Las alertas de licencia se disparan desde 30 días y los días hábiles excluyen solo fines de semana. Son convenciones documentadas; no se consideran errores de cálculo. Confirmar anticipación, destinatarios y calendario institucional antes de uso real.
5. Renovación anticipada agrega un año a la vigencia todavía vigente. El contrato actual lo establece y las pruebas lo confirman. Acordar si habrá una ventana de solicitud para evitar renovar indefinidamente con demasiada anticipación.
6. Hay datos demo y registros sin vigencia admitidos por compatibilidad. Definir qué se conserva como demo y cómo se valida/importa un registro heredado antes de que represente habilitación oficial.

La sospecha de desbordamiento no controlado por JSON numérico `1e100` no se reprodujo: el lector manejó el caso. Se descartó como hallazgo; aparece como H05 no reproducido en la evidencia para no ocultar el resultado negativo.

## Cobertura de los casos de uso

“Verificado” significa que hay implementación y evidencia para el comportamiento descrito, no certificación de ausencia de defectos. “Parcial” identifica una diferencia concreta que impide declarar cumplimiento completo.

- CU01 landing y tipos: parcial por F11 y F14.
- CU02 búsqueda, filtros y mapa: verificado en implementación, suite HTTP y navegación; el catálogo de tipos sigue pendiente.
- CU03 ficha de laboratorio: parcial por F14.
- CU04 ruta al abierto más cercano: algoritmo y enlaces implementados; parcial por F15 y por no comprobar navegación externa con ubicación real.
- CU05 acceso por rol: verificado en la suite con cookies, revocación y rechazo de accesos.
- CU06 nueva apertura: verificado como borrador con validaciones actuales; datos oficiales/catálogos por ampliar en F14.
- CU07 carga de PDF según requisito: parcial por F06 y F13.
- CU08 envío: funciona con requisitos obligatorios, pero es inconsistente en el catálogo opcional de F08.
- CU09 consulta de trámites propios: verificado con aislamiento; presentación histórica parcial por F05.
- CU10 revisión documental: parcial por F01 y F04.
- CU11 asignación: implementada; revisar separación de funciones en F09.
- CU12 historial de asignaciones: implementado y probado; historial documental por completar en F06.
- CU13 organización de agenda por supervisor: parcial por F09.
- CU14 ruta ordenada de inspección: implementada por orden horario; no optimiza distancias/tráfico, como ya está documentado. No se validó un recorrido real.
- CU15 registro de acta: parcial por F10 y decisión de horario.
- CU16 descarga y carga firmada: funciona el circuito de PDF; plantilla institucional parcial por F10.
- CU17 resultado de inspección: aprobado/observado/rechazado implementados; estructura de comprobaciones pendiente en F10.
- CU18 citación: parcial por F03.
- CU19 revisión/rechazo de acta: implementado y probado; proteger transición de F01.
- CU20 vigencia anual: verificada en apertura y renovación; falta conservar la vigencia histórica del cierre en F05.
- CU21 notificaciones en hitos: implementadas para decisiones principales; alertas de plazo incompletas en F03.
- CU22 usuarios: altas, edición, baja lógica y revocación verificadas.
- CU23 roles y permisos: seis perfiles y suspensión operativa verificadas; roles arbitrarios fuera del contrato actual.
- CU24 requisitos: CRUD lógico y obligatoriedad implementados; faltan versionado, publicación y plantilla en F05, F10 y F11.
- CU25 métricas por municipio: parcial por F12.
- CU26 informe por fechas: CSV filtrado implementado; contenido de indicadores parcial por F12.
- CU27 establecimientos propios y edición limitada: servicios protegidos implementados; consulta completa parcial por F14.
- CU28 alertas de licencia: lógica directa e idempotencia verificadas; proceso horario real no se esperó durante una hora.
- CU29 informe final: PDF generado, pero histórico incorrecto por F05.
- CU30 reprogramación: parcial por F07 y F09.
- CU31 plazo de subsanación: cálculo lunes a viernes implementado; seguimiento y alertas parciales por F03.
- CU32 renovación: recorrido completo y duplicados básicos verificados; decisiones sobre ventana y conservación histórica pendientes.
- CU-P1 revisión legal: existe, pero hereda las limitaciones documentales F04 y la validación F02.
- CU-P2 aprobación firmada y aviso al coordinador: verificado en el recorrido; revisar validación F02.
- CU-P3 aviso de aprobación al propietario: verificado en el recorrido.
- CU-P4 cierre y habilitación: funciona el caso normal; aceptación de datos dañados F02 e informe mutable F05 impiden declararlo cerrado.

## Plan propuesto de corrección

### Etapa 1 Acordar el flujo y los criterios de aceptación

Resolver las seis decisiones anteriores dentro de la revisión de este plan. Recuperar y mantener en los Markdown la lista explícita CU01–CU32 y CU-P1–CU-P4, con requisitos, responsable, evidencia y estado real. Marcar como parciales los casos que hoy están resumidos como implementados.

Entregable: diagrama y reglas por transición, por documento y por tipo de observación. La aprobación del plan autoriza la implementación local acordada, no una publicación.

### Etapa 2 Corregir las fallas de flujo y validación

Atender F01, F02, F07 y F08. Centralizar las transiciones y sus precondiciones; separar validación de dominio de presentación; corregir la exclusión de la reserva actual y la regla de documentos opcionales. Aplicar la decisión horaria y el circuito legal acordados.

Criterios de aceptación: un observado legal no salta controles; datos inválidos no habilitan un laboratorio; reprogramar no choca con la propia visita, pero sí con otras; el catálogo opcional permite un recorrido coherente; las decisiones concurrentes no se duplican.

### Etapa 3 Preservar requisitos, documentos y resoluciones históricas

Atender F04, F05, F06 y F13 con migraciones aditivas: versiones de requisitos/documentos, estado por documento, motivos y revisores; datos del cierre con vigencia otorgada y copia de la información oficial. Conservar registros y archivos anteriores. No reinterpretar retrospectivamente lo que no tiene evidencia.

Criterios de aceptación: renombrar/desactivar requisitos o renovar no altera un expediente cerrado; se consulta el PDF revisado en cada decisión; corregir un documento no obliga a duplicar cargas ni a perder decisiones anteriores; las migraciones preservan registros heredados.

### Etapa 4 Completar agenda, inspecciones y citaciones

Atender F03, F09 y F10: asignación y organización de inicio/fin según rol; acta estructurada y PDF versionado; citación con evidencia y plazos; recordatorios sin duplicados para propietario/supervisor según lo acordado. Mantener rutas ordenadas y trazabilidad de reasignaciones.

Criterios de aceptación: solo el supervisor asignado organiza/registra su visita; se detectan intervalos solapados; cada reprogramación avisa y conserva historial; citaciones se consultan con evidencia; plazos próximos y vencidos producen los avisos configurados una sola vez. No se aplican multas automáticas.

### Etapa 5 Completar portal, fichas y métricas

Atender F11, F12, F14 y F15: requisitos públicos y plantillas, catálogos institucionales, ficha y consulta de documentos propios, alternativa a geolocalización, indicadores y exportaciones concordantes. Ajustar navegación para las funciones completadas y comprobar escritorio/móvil.

Criterios de aceptación: modificar requisitos se refleja en la publicación sin alterar expedientes históricos; el propietario consulta sus documentos sin editar datos oficiales; los informes muestran el mismo período/municipio que la pantalla; borradores se distinguen de trámites recibidos; denegar ubicación no bloquea la ruta manual.

### Etapa 6 Validar los datos y cerrar la verificación

Respaldar base y documentos antes de aplicar migraciones sobre una instalación real. Generar informe de registros demo/heredados y solicitar la fuente institucional para validarlos, sin modificar fechas o propietarios por suposición. Revisar referencias, versiones, relaciones, coordenadas, duplicados, documentos y vigencias después de migrar una copia.

Agregar pruebas de regresión de cada falla confirmada y recorridos de apertura, subsanación documental/legal/de campo, rechazo, citación, renovación y permisos. Añadir comprobaciones con Kestrel para límites HTTP que TestServer no aplica; probar concurrencia de decisiones y conflictos de agenda con peticiones simultáneas. Ejecutar la suite completa y comparar los casos contra la referencia.

Entregable: informe final con casos aprobados, evidencia por rol, migraciones verificadas y pendientes externos explícitos. No declarar cumplimiento institucional de formularios ni datos sin su fuente oficial.

## Información institucional que falta para cerrar el alcance

- Catálogo oficial de tipos y municipios, y datos reales de establecimientos/propietarios.
- Lista definitiva de requisitos y plantillas oficiales descargables.
- Formulario de acta con sus artículos/lista de comprobación y formato de citación.
- Regla oficial de días hábiles, anticipación/destinatarios de avisos y posibles excepciones horarias.
- Definición de rechazo legal definitivo y de cuándo una observación obliga a una nueva inspección.
- Fórmulas de indicadores y período de referencia que desea el gerente.

La ausencia de estos elementos no impide corregir los defectos técnicos reproducidos, pero sí impide inventar el contenido institucional o certificarlo.

## Evidencia y límites

Las evidencias resumidas se conservan en `docs/auditoria-2026-10-18/`. El diagnóstico ejecutable y las imágenes de muestreo están en la carpeta de trabajo de auditoría fuera del código de la aplicación. Los siete diagnósticos problemáticos confirmados son H01, H02, H03, H04, H06, H07 y H09. H08 documenta la regla horaria a decidir; H05 fue descartado.

La revisión valida los casos ejecutados y la lógica inspeccionada. No incluye una certificación legal, auditoría de expedientes reales, autenticidad criptográfica de firmas, antivirus de PDF, carga productiva, ejecución remota de CI ni prueba visual exhaustiva de todas las pantallas en todas las resoluciones. Se observó el navegador en un viewport estrecho; no se presenta como una nueva certificación responsive completa.

Nota posterior: el usuario aprobó este plan y se aplicaron las correcciones. Consulte el informe de cierre enlazado al principio para conocer el estado actual y los insumos institucionales que siguen pendientes.
