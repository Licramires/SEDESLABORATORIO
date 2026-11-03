# Auditoría ampliada de flujos, controles y validaciones

Fecha: 22 de octubre de 2026, hora de Bolivia. Algunos registros automáticos tienen fecha UTC del día 23.

Resultado: **360 pruebas .NET y 6 JavaScript aprobadas**, ninguna fallida ni omitida. Compilación Release sin advertencias ni errores. Se generó y revisó una publicación local; no se desplegó. El inventario y los resultados de esta revisión están en [auditoria-exhaustiva-20261022](auditoria-exhaustiva-20261022/resumen.json). Esta revisión amplía la anterior de 91 pruebas; las cifras de informes anteriores son históricas.

## Alcance comprobado

El inventario se obtiene de las acciones MVC registradas, para no depender de una selección manual de pantallas. Las mismas pruebas vuelven a recorrerlo cuando se agregan acciones.

- **69 acciones:** 43 de consulta y 26 POST. Las 43 consultas se ejercitaron con los seis perfiles y sin sesión: **301 combinaciones**, incluyendo descargas. Se verifican los accesos permitidos y denegados y la cabecera de los PDF recibidos.
- **603 enlaces internos distintos por perfil**, comprobando su respuesta; se aceptan las redirecciones propias de la navegación. No se siguieron enlaces externos que compartieran ubicación personal.
- **48 formularios renderizados y 50 botones dentro de ellos:** destino existente, método, token de protección, campos y texto de botones. Hay un registro adicional del detalle del expediente en las **54 combinaciones de seis roles y nueve estados**, con sus acciones, campos y botones. Los controles React se comprueban por separado en el navegador y con pruebas JavaScript.
- **318 comprobaciones de seguridad POST:** ausencia de token y perfiles sin autorización, contrastando todas las tablas antes y después para detectar escrituras indebidas.
- **685 combinaciones prohibidas de rol, estado y acción:** sin cambio de estado, versión ni historial. **17 transiciones permitidas**, con sus efectos y con repetición del mismo envío para comprobar que no se ejecutan dos veces.
- **Los 11 permisos configurables**, retirados y comprobados sobre sesiones ya abiertas. La edición de permisos también rechaza listas incompletas, duplicadas, ajenas y versiones desactualizadas.
- **228 casos de campos inválidos:** ausentes, vacíos, espacios, longitudes, caracteres de control, coordenadas, números, identificadores, versiones, correos, teléfonos, opciones, fechas y plazos. Se compara el contenido de todas las tablas para comprobar que los rechazos no guarden cambios.
- Cargas vacías, extensión falsa, contenido falso y PDF incompleto en los formularios de requisitos, catálogos, actas, citaciones y solicitudes. Se conserva la cobertura previa de tamaños, almacenamiento privado, versiones y evidencias PDF/JPG/PNG.
- Apertura y renovación completas, devoluciones documentales y legales, rechazo definitivo, rechazo de campo reprogramable, asignación con y sin horario, solapamientos y solicitudes simultáneas, actas y citaciones, vencimientos y alertas, informes, métricas, usuarios, secciones, requisitos, permisos y compatibilidad de rutas antiguas.

Los casos CU01–CU32 y CU-P1–CU-P4 se mantienen en [CASOS_DE_USO.md](CASOS_DE_USO.md). Las pruebas anteriores siguen ejecutándose junto con las nuevas.

## Errores reproducidos y corregidos

1. **Coordenadas fuera de rango por redondeo.** La validación entera admitía valores como `90.000001`. Latitud y longitud se validan ahora como decimales, tanto al crear como al corregir el borrador.
2. **Opciones mal interpretadas que modificaban datos.** Algunas acciones escalares no comprobaban los errores del formulario; un booleano inválido podía convertirse en falso y desactivar una opción. Se comprueba la interpretación y se exigen los parámetros que deben venir expresamente en la petición.
3. **Errores de página al volver a mostrar casillas inválidas.** El tratamiento de booleanos conserva el error y proporciona a la casilla un valor representable, evitando un error 500. Se probaron usuarios, requisitos, secciones y catálogos.
4. **Filas incompletas del editor de permisos.** Omitir una opción o su versión ya no se interpreta como una decisión válida por defecto.
5. **Copias históricas estructuralmente dañadas.** Se rechazan elementos nulos, nombres ausentes, versiones/identificadores inválidos y duplicados en las copias de requisitos y verificaciones. No se sustituyen por una lista vacía ni por el catálogo actual.
6. **Validación insuficiente de campos.** Se reforzaron correos, teléfonos, longitudes, texto con caracteres de control, valores enumerados, campos requeridos y fechas civiles. El alta de solicitud también comprueba nombre/coordenadas obligatorios, límites numéricos y longitudes en el navegador antes de enviar los archivos. Los campos opcionales siguen admitiendo ausencia de valor; no se añadieron reglas de formato de teléfono específicas de un país.
7. **Consultas mal escritas que abrían otra pantalla.** Los filtros y los identificadores de administración inválidos se rechazan; no se abre silenciosamente un alta nueva ni una lista sin filtrar.
8. **Pérdida de cambios en los servicios.** Una pestaña antigua podía sobrescribir una edición posterior. Se envía una revisión del texto leído y se guarda mediante actualización condicional atómica. Si cambió, se pide recargar. No requiere una columna ni una migración nueva.
9. **Renovaciones inconsistentes al habilitar.** Se bloquean la renovación sin laboratorio, el propietario distinto al del establecimiento y la vigencia que no permite agregar un año. Se comprueba antes de almacenar el PDF o guardar la aprobación.
10. **Agenda en los extremos del calendario.** Se evita crear años de cinco cifras o días repetidos; la última semana puede ser parcial y la navegación se desactiva en el límite. El servidor acepta las mismas fechas civiles que la interfaz.
11. **Actualizar agenda perdía el contexto.** Ahora conserva la fecha seleccionada y la búsqueda al obtener los datos nuevamente.
12. **Observaciones documentales repetidas.** Al devolver, se pueden reutilizar las correcciones ya indicadas por documento. El motivo adicional es opcional para esa devolución; el rechazo definitivo conserva su fundamento obligatorio.
13. **Instrucciones que no correspondían al estado.** Un expediente ya enviado no invita al propietario a agregar archivos como si siguiera siendo editable.
14. **Control de ruta sin acceso normal por teclado.** «Usar mi ubicación» usa un botón nativo con su acción correspondiente.
15. **Compatibilidad de preparación de acta.** Su acción específica elige la operación del servidor sin exigir al cliente un nombre de operación redundante.
16. **Claves de archivos históricas inválidas.** Una ruta dañada se trata como archivo no disponible, en vez de provocar una excepción de ruta.

Las pruebas que reprodujeron inicialmente los errores de campos, servicios y renovaciones se conservaron en `tests/SiLab.Tests/TestResults/`. El resultado final aprobado está copiado en la evidencia de esta auditoría.

## Comprobación con navegador real

Se inició una copia consistente de la base y de los archivos en una carpeta aislada. Las escrituras de prueba no se hicieron en la base del proyecto. Se accedió a los seis perfiles y al portal público.

- Supervisor: ambos botones de semana, los siete días, fecha por teclado, búsqueda, actualización conservando filtros, volver a hoy, rutas con y sin visitas y calendario extremo. Acta con no aplicables, incumplimiento, ambos dictámenes, observaciones condicionales, guardado previo y envío con PDF. Registro de citación con fecha, plazo y evidencia.
- Coordinador: bandeja, aceptación de acta observada y nueva asignación sin horario conservando las visitas anteriores. Aprobación individual sin frase repetida, observación de un plano, cierre desactivado mientras faltan revisiones y devolución reutilizando la corrección existente.
- Propietario: comprobación de campos obligatorios, selección de un PDF y siete posteriores, eliminación de uno, reposición y selección duplicada. Se guardaron los ocho archivos, se comprobó el rechazo de envío por requisitos faltantes, se asociaron tres archivos existentes y se envió el expediente. Guardado de servicios y bloqueo de renovación duplicada.
- Abogado: devolución sin seleccionar documentos rechazada; aprobación con fundamento y PDF que deja el expediente habilitado y conserva la vigencia otorgada.
- Administrador: sección, requisito dentro de esa sección, carga de plantilla, desactivación/reactivación y consulta pública del resultado. Editor de permisos, cancelación, configuración avanzada, seis clases de catálogo y rechazo de fecha inexistente. Las altas/cambios de credenciales y los cambios efectivos de acceso se cubren en integración aislada; no se cambiaron credenciales personales ni se concedió acceso desde el navegador.
- Gerente: las tres opciones de series, ambos meses disponibles, actualización de gráficos, tablas y descarga CSV. Consulta con fechas invertidas y recuperación mediante el formulario de corrección.
- Portal público: zoom del mapa y búsqueda conjunta por servicio, tipo y estado; listado de requisitos y menús móviles. Avisos: marcar como leído y comprobar la retirada del botón correspondiente. Acceso y cierre de sesión de cada perfil.

Se guardaron **28 registros agrupados de comprobación**; no representan el número total de clics ni de instancias de botones. Las pantallas medidas en móvil no mostraron desbordamiento horizontal del documento. Los registros de consola inspeccionados no mostraron errores ni advertencias. Se respetó DESIGN.md y el detector de Impeccable no informó observaciones sobre las fuentes de interfaz modificadas.

## Evidencia consultable

- [Inventario de acciones](auditoria-exhaustiva-20261022/inventario-endpoints.json), [consultas por perfil](auditoria-exhaustiva-20261022/paginas-descargas-por-rol.json) y [enlaces](auditoria-exhaustiva-20261022/enlaces-verificados.json).
- [Formularios y botones renderizados](auditoria-exhaustiva-20261022/formularios-botones-renderizados.json) y [detalle por rol y estado](auditoria-exhaustiva-20261022/botones-por-rol-y-estado.json).
- [Seguridad POST](auditoria-exhaustiva-20261022/seguridad-todos-post.json), [permisos](auditoria-exhaustiva-20261022/permisos-configurables.json), [transiciones prohibidas](auditoria-exhaustiva-20261022/transiciones-prohibidas.json) y [permitidas](auditoria-exhaustiva-20261022/transiciones-validas.json).
- [Campos inválidos](auditoria-exhaustiva-20261022/campos-invalidos.json), [360 pruebas ejecutadas](auditoria-exhaustiva-20261022/pruebas-ejecutadas.json) y [resultado TRX](auditoria-exhaustiva-20261022/resultado-final.trx).
- [Controles del navegador](auditoria-exhaustiva-20261022/controles-navegador.json), [agenda móvil](auditoria-exhaustiva-20261022/agenda-movil.png), [requisitos móviles](auditoria-exhaustiva-20261022/requerimientos-movil.png), [devolución sin duplicar observaciones](auditoria-exhaustiva-20261022/devolucion-sin-observaciones-duplicadas.png) y [habilitación legal](auditoria-exhaustiva-20261022/revision-legal-habilitada.png).
- [Integridad y conservación de datos](auditoria-exhaustiva-20261022/datos-originales.json).

## Datos, respaldo y límites

Base original: 6 usuarios, 15 solicitudes, 39 documentos, 7 inspecciones, 5 laboratorios y 9 migraciones. Integridad SQLite `ok`, cero referencias rotas, cero archivos de documentos ausentes, cero coordenadas fuera de rango, cero renovaciones sin vínculo, cero propietarios vinculados inconsistentes y cero cruces de agenda en los registros actuales. Las 14 tablas coinciden con el respaldo previo y los 47 archivos privados mantienen su contenido. **Esta revisión no migró ni modificó la base original.**

Punto de restauración: `App_Data/checkpoints/20261022-223121-antes-auditoria-exhaustiva/`. Incluye ZIP de 321 archivos, copia SQLite consistente, documentos privados, instrucciones y SHA-256 verificados. No se hizo commit, reinicio de datos ni despliegue.

Siguen pendientes los datos de propietario/vigencia de tres laboratorios heredados y los contenidos institucionales definitivos: formularios, artículos, lista de comprobación, catálogos y feriados. No se inventaron para hacer desaparecer avisos de datos incompletos. Todos los registros usan el mismo circuito operativo, sin categorías demo/oficial.

No se compartió ubicación personal con Maps ni se hizo un recorrido físico; la geolocalización real y sus permisos no están certificados por estas pruebas. Los avisos son internos conforme al contrato, sin integración de correo/SMS. Los PDF de prueba no certifican firmas criptográficas o validez institucional. La cobertura documenta el inventario y los casos ejecutados; no constituye una garantía de ausencia de cualquier defecto bajo toda combinación de datos, navegador o interrupción de red.
