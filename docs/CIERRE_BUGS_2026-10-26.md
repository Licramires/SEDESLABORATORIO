# Corrección y verificación de bugs — 26/10/2026

Los tres bugs confirmados en la campaña del 23/10 están corregidos y verificados. Se mantienen el diseño de DESIGN.md, la arquitectura SOLID, las autorizaciones y los códigos de error. No hay migración de esquema.

## BUG-01: recuperación de formularios

Las navegaciones HTML que reciben errores 400/409 en las operaciones de gestión, administración o registro muestran una página con el menú habitual, el motivo y un enlace GET de recuperación. Una pestaña antigua sigue recibiendo 409 y no puede sobrescribir o repetir la operación. La recuperación abre la versión actual del expediente, sin volver a enviar el POST obsoleto. Los clientes HTTP y los endpoints de métricas conservan sus formatos anteriores. Las actas que ya mostraban sus errores dentro del formulario mantienen ese comportamiento.

Evidencia de navegador: `outputs/bugs-20261026/BUG-01-validacion.txt`, `BUG-01-conflicto.txt`, `BUG-01-recuperado.txt` y sus capturas. Se intentó observar un PDF sin motivo, se volvió al expediente y se aprobaron los datos correctos. Se abrió un borrador en dos pestañas: el segundo envío recibió el mensaje de conflicto y su enlace volvió a un expediente en revisión con un único envío en el historial.

## BUG-02: aprobación documental repetida

Un documento ya aprobado no ofrece de nuevo el botón de aprobar; conserva la opción de observarlo con fundamento. La protección también está en el servidor: repetir una aprobación con la versión actual no cambia fecha, revisor, observación, versión del expediente, avisos ni historial. Una versión antigua sigue bloqueada por el control de concurrencia. Después de una observación válida, el documento vuelve a admitir su revisión correspondiente.

Evidencia: `BUG-02-documento-aprobado.txt` y captura en la carpeta de esta entrega. La regresión comprueba la igualdad de todas las tablas antes/después de la aprobación repetida, el rechazo de la versión antigua y la posibilidad de observar después.

## BUG-03: ubicación pública inválida

Se valida la latitud −90…90 y longitud −180…180 de los datos persistidos. Los registros con ubicación inválida conservan su ficha y dirección de referencia, con un aviso de corrección pendiente. Se excluyen sus marcadores y el cálculo de laboratorio más cercano; no se ofrece una ruta a coordenadas inválidas, incluso entrando directamente por URL. No se inventa una ubicación para sustituir un dato dañado.

La regresión ensaya latitudes ±91, longitudes ±181 y 999,999. Comprueba listado, datos del mapa, ficha y ruta. Se verifican también los límites válidos ±90/±180, 0,0 y coordenadas ordinarias comparando numéricamente el destino. La navegación real usó un registro dañado creado únicamente en la copia desechable; evidencia `BUG-03-ficha.txt` y `BUG-03-ruta.png`.

## Resultado y alcance

- Compilación Release: cero errores y advertencias.
- 389 pruebas de servidor aprobadas: 377 existentes tras SOLID y 12 regresiones de estos bugs.
- 28 pruebas JavaScript aprobadas.
- Comprobación visual de los tres cambios en el navegador integrado de escritorio, en una copia local independiente.
- Impeccable revisó las vistas modificadas; el detector solo señaló dos colores semánticos preexistentes de marcadores abiertos/cerrados. Se conservaron esos indicadores.
- Verificación de integridad y conservación de la base original y sus 54 archivos privados en `outputs/bugs-20261026/verificacion.json`.

Punto de restauración previo: `App_Data/checkpoints/20261026-144304-antes-bugs-pendientes/`, con código, copia consistente SQLite, documentos y hashes.

Los Excel del 23/10 conservan el resultado histórico de aquella ejecución. PUB-EXT-09 supera esta reejecución dirigida. Los bugs detectados en E2E-EXT-04 están resueltos, pero eso no acredita por sí solo el recorrido de todos los botones en todos los estados. Permanecen sin certificar los nueve casos manuales/parciales enumerados en `verificacion.json`: ubicación física, otros tamaños/navegadores, selector nativo, accesibilidad completa, bloqueo externo del mapa, recorrido diario completo y contenido institucional definitivo. No se realizaron pruebas de producción.
