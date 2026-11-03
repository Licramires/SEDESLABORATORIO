# Casos de uso y estado de verificación

Ampliación local del 23/10/2026: restauración completa y fallos operativos verificados. Producción excluida; los pendientes de clics completos, GPS real, dispositivos e información institucional se documentan en `CIERRE_PENDIENTES_LOCALES_2026-10-23.md`.

Actualizado el 22 de octubre de 2026 después de la auditoría de flujos. La definición se recuperó del contexto original y de la reunión proporcionada. La cobertura técnica se apoya en la suite de 366 pruebas de servidor y 28 JavaScript, revisión de código, servidor real y navegación con los seis perfiles y el portal público; no equivale a certificar contenido institucional que todavía no fue entregado.

## Consulta pública y propietario

- CU01 Landing y tipos: implementados; catálogo público de requisitos añadido. Los ocho tipos definitivos los carga la institución.
- CU02 Buscar/filtrar y mapa: búsqueda por nombre, tipo y servicio, filtros y marcadores. Comprobado en suite y navegador.
- CU03 Detalle: ficha, mapa, servicios, dirección, contacto y código cuando están registrados. Los datos desconocidos no se inventan.
- CU04 Ruta: abierto más cercano por distancia geográfica y alternativa de origen manual en Maps. Se comprobaron enlaces y mensajes; no se usó ubicación personal ni se realizó un recorrido físico.
- CU05 Login por rol: seis perfiles, cookies, revocación, antiforgery, roles/permisos y límite de intentos comprobados.
- CU06 Nueva apertura: borrador, coordenadas/rangos, datos generales y validación contra catálogos. En producción exige configuración institucional.
- CU07 PDF por requisito: almacenamiento privado, asociación de cargas iniciales, versiones, decisiones y descarga autorizada comprobados.
- CU08 Envío: requisitos obligatorios y documentos observados se validan. Un catálogo sin obligatorios tiene un recorrido coherente sin exigir un archivo artificial.
- CU09 Estado propio: aislamiento entre propietarios e historial legible, con documentos históricos conservados.
- CU27 Mis establecimientos: ficha, acceso a expedientes/documentos y edición limitada a servicios protegida contra cambios simultáneos. Los datos oficiales permanecen bajo el circuito del trámite.
- CU28 Avisos de licencia: aviso próximo/vencido idempotente y cierre al vencer. El proceso se ejecutó de forma directa en las pruebas, sin esperar una hora de reloj.
- CU32 Renovación: circuito completo, conservación del establecimiento, duplicados bloqueados y vigencia histórica de cada cierre independiente. Ventana anticipada configurable.

## Coordinación e inspecciones

- CU10 Revisión documental: decisión por documento/versión; devolución subsanable y rechazo definitivo diferenciados. No se aprueba con documentos pendientes.
- CU11 Asignar supervisor: supervisor activo, horario opcional y aviso; las precondiciones impiden saltar revisiones documentales/legales.
- CU12 Historial de asignaciones: se conservan las visitas anteriores y los motivos.
- CU13 Organizar agenda: conflictos de intervalos idénticos, parciales y envolventes comprobados; las visitas con acta también reservan su horario.  supervisor asignado programa inicio/fin; se comprueban intervalos y se avisa al propietario.
- CU14 Ruta ordenada: visitas del día por horario con enlace a Maps. No se afirma optimización por tráfico o distancia.
- CU15 Registrar acta: resumen derivado de incumplimientos sin repetición obligatoria de hallazgos y notas adicionales opcionales.  lista de verificación versionada, hallazgos y preparación previa a firma. La lista oficial está pendiente de carga por el administrador.
- CU16 Formulario y firma: descarga de plantilla institucional/resumen, preparación de contenido y carga del PDF firmado. La plantilla real debe aportarla el SEDES.
- CU17 Resultado: aprobado, observado o rechazado; un punto incumplido impide resultado global aprobado.
- CU18 Citación: número interno, emisión, motivo, vencimiento, evidencia PDF/JPG/PNG y anticipación de aviso. Descarga privada comprobada.
- CU19 Revisar acta: aceptar resultado o rechazar acta para reprogramar; conserva las decisiones anteriores.
- CU20 Vigencia anual: solo después de revisión legal positiva; un año desde la vigencia activa o la aprobación si ya venció.
- CU21 Notificaciones: decisiones, asignaciones, programación, citaciones, subsanación y vigencia. Avisos internos, sin integración contratada de correo/SMS.
- CU29 Informe de cierre: se guarda un PDF inmutable con datos de ese cierre. Una renovación posterior no lo cambia.
- CU30 Reprogramar: puede asignarse al mismo u otro supervisor; la reserva anterior no causa un conflicto consigo misma.
- CU31 Subsanación: plazo de días hábiles, feriados configurables y avisos próximos/vencidos. No aplica sanciones automáticas.

## Administración y gerente

- CU22 Usuarios: edición protegida contra pantallas obsoletas mediante versión.  alta, edición, baja lógica, revocación de sesiones y protección de supervisores con visitas pendientes.
- CU23 Roles/permisos: seis perfiles y suspensión de capacidades dentro de sus atribuciones; las acciones nuevas también se comprueban en servidor.
- CU24 Requisitos: alta/edición/baja lógica, obligatoriedad, versión y plantilla pública. Las copias de expedientes recibidos permanecen estables.
- CU25 Métricas por municipio: recibidas, pendientes, habilitadas, rechazos definitivos, apertura/renovación, tiempos, tasa y rendimiento. Borradores separados y fórmulas explícitas.
- CU26 Informe por fechas: mismo conjunto de pantalla/exportación y protección del CSV contra fórmulas. Trazabilidad reciente incluida.

## Revisión legal

- CU-P1 Licencia/plano: consulta privada del expediente y de los documentos revisados; los datos oficiales se validan por separado de su presentación.
- CU-P2 Aprobación firmada y aviso al coordinador: requiere PDF, inspección conforme validada y documentación completa/revisada.
- CU-P3 Aviso al propietario: aprobación y decisiones quedan en su bandeja e historial.
- CU-P4 Cierre/habilitación: resolución y vigencia quedan guardadas; rechazos legales definitivos no permiten reactivar mediante asignación. La subsanación legal vuelve a revisión legal sin repetir campo, salvo indicación expresa.

## Pendientes que dependen de información institucional

Catálogos oficiales, lista de comprobación y artículos, formularios, feriados, fuente de datos reales y confirmación de las políticas configurables. Los tres establecimientos heredados con propietario/vigencia faltantes siguen señalados por esa incompletitud, con las mismas reglas que cualquier registro. Ningún resultado de prueba con archivos sintéticos acredita una firma real.

Detalle vigente de correcciones, inventario de acciones y controles, evidencia, respaldo y límites: `AUDITORIA_EXHAUSTIVA_2026-10-22.md`. La revisión anterior permanece en `AUDITORIA_FLUJOS_2026-10-22.md`.
