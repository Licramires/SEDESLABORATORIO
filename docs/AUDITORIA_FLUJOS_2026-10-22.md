# Auditoría de flujos y correcciones — 22/10/2026

Se revisaron los seis perfiles y la consulta pública contra `CASOS_DE_USO.md`, `CONTRATO_DATOS.md`, `CONTEXTO_PROYECTO.md` y la definición del proyecto recuperada de la reunión/transcripción. Se conservan MVC, los módulos existentes y DESIGN.md; React se usa para las interacciones del acta, junto a la agenda y las métricas que ya lo utilizaban.

## Resultado comprobado

- Compilación Release: 0 errores y 0 advertencias.
- 91 pruebas .NET aprobadas, 0 fallidas, 0 omitidas. Informe: `../tests/SiLab.Tests/TestResults/auditoria-flujos.trx`.
- 4 pruebas JavaScript aprobadas; bundle local generado. CI exige al menos 91 pruebas de servidor.
- Publicación local comprobada con recursos React/CSS/licencia y sin base de datos, documentos privados, App_Data ni ClientApp. No se desplegó ni se hizo commit/push.
- SQLite: integridad `ok`, 0 referencias rotas. 15 solicitudes, 39 documentos, 7 inspecciones, 5 laboratorios y 6 usuarios conservados. Los 47 archivos privados coinciden byte por byte con el respaldo. No quedan cruces en los horarios guardados.

## Correcciones aplicadas

1. **Agenda del supervisor y asignación del coordinador.** Se comparan intervalos completos, también contra visitas con acta registrada, validada o devuelta. Solo se excluyen reservas sustituidas y la reserva pendiente que se está reemplazando. Dos visitas contiguas sí se admiten. La comprobación y el guardado siguen dentro de la transacción; las pruebas existentes incluyen solicitudes simultáneas. Se rechaza fin sin inicio y se evitan desbordamientos con fechas extremas.
2. **Acta.** Cada verificación registra cumple, no cumple o no aplica. Solo el incumplimiento pide qué corregir. El servidor arma el resumen a partir de esos puntos; las notas adicionales son opcionales. Sin incumplimientos se deriva aprobado; con incumplimientos se decide entre subsanación y rechazo de campo. El plazo aparece cuando corresponde. No se admite aprobación con incumplimientos, todos los puntos no aplicables al enviar, IDs alterados, notas requeridas vacías, envío sin PDF ni registro antes del inicio.
3. **Errores del acta.** El formulario vuelve a mostrarse con lo escrito si falla la validación. Debe volver a elegirse el archivo; el navegador no permite restaurar automáticamente esa selección. Las copias históricas JSON ilegibles bloquean el avance con un mensaje recuperable en vez de tratarlas como una lista vacía. La comprobación administrativa también las enumera.
4. **Revisión documental.** Aprobar un PDF no obliga a repetir una frase genérica. Observarlo exige la corrección concreta. El cierre muestra requisitos faltantes y PDF sin conformidad; el avance se deshabilita hasta completar la revisión, y el servidor vuelve a comprobarlo. Devolución y rechazo están diferenciados.
5. **Propietario.** Puede corregir nombre, tipo, municipio, coordenadas y contacto del borrador de apertura antes del primer envío. Se conservan PDF e historial y se comprueban propiedad, catálogos, rangos y versión. No se alteran datos enviados ni los del establecimiento de una renovación desde ese formulario.
6. **Abogado.** La aprobación firmada tiene su propio formulario. La devolución identifica exactamente los documentos que deben corregirse; los demás conservan su aprobación. La opción de repetir campo solo corresponde a la devolución. El abogado puede consultar el seguimiento de sus decisiones después de devolver o rechazar. Se mantienen PDF, documentación revisada y acta conforme como precondiciones de habilitación.
7. **Renovación.** Un rechazo de campo que aún admite reprogramación sigue siendo un trámite activo y bloquea otra renovación del mismo establecimiento. Las renovaciones que heredaron el rótulo antiguo del laboratorio reciben el nombre corregido, conservando IDs y archivos.
8. **Administración.** Edición de usuarios con versión para impedir que una pantalla antigua sobrescriba una edición posterior. Se mantienen revocación de sesiones, protección del propio acceso y reasignación previa de supervisores con visitas pendientes. Requisitos, secciones y permisos conservan su cobertura anterior.
9. **Gerencia.** Las fechas inválidas muestran un formulario para corregirlas y volver a consultar; JSON/CSV mantienen respuesta de error. Se comprobó recuperar la consulta: 12 recibidos, 3 borradores, 1 habilitado, 10 pendientes y 1 rechazo definitivo en septiembre–octubre, según los datos guardados.
10. **Navegación.** Supervisor centra su trabajo en agenda, actas y citaciones; rutas/expedientes quedan en consultas. Coordinador tiene accesos a revisión documental, asignación y revisión de actas, con el siguiente paso visible por expediente y estados legibles. Administrador centra usuarios, permisos y requisitos; catálogos y comprobaciones permanecen en configuración avanzada. Cuenta y avisos se abren desde la cabecera, eliminando enlaces duplicados. Las acciones de tablas tienen área pulsable de al menos 44 px.

## Un solo conjunto de datos

Por indicación del usuario no se agregaron categorías, tablas ni indicadores de demo/oficial, ni filtros que oculten registros por su origen. Los datos iniciales usan los mismos estados, permisos, formularios y validaciones que los demás registros.

Se reemplazaron los rótulos de tareas usados como nombres de laboratorio, se limpiaron los servicios y se normalizaron los nombres iniciales de tipo/municipio. La reparación conserva nombres personalizados y relaciones y es idempotente. Los códigos históricos no se utilizan para conceder permisos ni saltar etapas. Los PDF de ejemplo y los informes de cierre anteriores se conservan; corregir rótulos no reescribe evidencias históricas.

El usuario autorizó distribuir los cuatro horarios históricos repetidos: inspecciones #4, #5, #6 y #7 del 07/10/2026 ahora ocupan 09–10, 11–12, 13–14 y 15–16, respectivamente. Se añadieron cuatro movimientos al historial y se actualizaron las versiones de sus expedientes. Detalle en `auditoria-flujos-20261021/horarios-corregidos.json`.

Los tres establecimientos heredados sin propietario/vigencia siguen identificados por esos datos faltantes en Validación de datos. Esta es una comprobación de completitud aplicable a cualquier registro, sin distinción por origen; no acredita por sí sola una habilitación. Los formularios y artículos institucionales definitivos siguen siendo configuración del proyecto.

## Esquema y restauración

Novena migración: `20261022035220_WorkflowValidationAndActNotes`. Agrega `Usuarios.Version`, `Inspecciones.NotasAdicionales` e índice supervisor/fecha. Las actas anteriores se conservan y no se reinterpretan sus textos como nuevas notas.

Punto de restauración previo: `../App_Data/checkpoints/20261021-233744-antes-auditoria-flujos/`. Contiene 299 archivos en ZIP, copia consistente de SQLite, documentos privados, instrucciones y hashes SHA-256 verificados. Para restaurar, detener el proyecto y extraer en otra carpeta; conservar el estado actual antes de reemplazarlo.

## Comprobación visual y límites

Se probaron acceso de los seis roles, preparación/persistencia del acta, error por PDF ausente sin pérdida de texto, aprobación documental sin motivo repetido, edición de borrador conservando tres PDF, selección legal de documentos, configuración avanzada y recuperación del filtro de gerencia. Las escrituras de navegación de prueba se hicieron en copias temporales. En la base local solo se aplicaron la migración, reparación de rótulos y horarios autorizados; la comprobación final fue de consulta.

Escritorio y móvil de 390 × 844: no se observó desbordamiento horizontal del documento en las superficies medidas; las tablas mantienen su desplazamiento interior. Casillas legales y enlaces de expedientes medidos en 44 px. El muestreo DOM de contraste no detectó texto por debajo del umbral en las páginas registradas; no certifica mapas, PDF, lectores de pantalla ni hardware táctil. Impeccable sobre las fuentes modificadas: ninguna observación del detector.

Capturas, medidas, integridad y auditoría mecánica: `auditoria-flujos-20261021/`. No se enviaron correos externos ni se probaron firmas institucionales reales o recorridos físicos en Maps. Las pruebas y la revisión cubren los casos descritos; no equivalen a una garantía de ausencia de todo defecto posible.
