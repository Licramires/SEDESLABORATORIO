# Verificación de pendientes en copias locales

El usuario delimitó la revisión: «Prueba solo lo que se pueda ahora, nada de producción». No se accedió a un dominio ni servidor de producción. Los ensayos que modifican datos usaron bases y documentos copiados en carpetas independientes.

**Resultado actual:** 366 pruebas de servidor y 28 JavaScript aprobadas; compilación Release sin errores ni advertencias. Además, se reconstruyó la solución del respaldo y se ejecutaron sus 360 pruebas originales, con resultado aprobado. La base del proyecto y sus 47 documentos privados siguen idénticos al punto previo.

## Resultado de los siete puntos

1. **Botones y estados: cobertura local ampliada; recorrido completo por clics pendiente.** Se conserva la cobertura anterior de 69 acciones, seis roles, seguridad, formularios, 54 combinaciones de detalle por rol/estado y transiciones. Se añadieron pruebas del código de confirmación: cancelar, aceptar y acciones sin advertencia. El mapa de la aplicación restaurada se observó en el navegador. Al intentar ampliar a Brave, el control de Windows detuvo la operación porque no pudo verificar la URL con suficiente certeza. No se realizaron más acciones de interfaz después de ese bloqueo. Por tanto, no se afirma haber completado todos los clics ni las altas/cambios de credenciales, eliminaciones y cambios de permisos desde una interfaz real en esta revisión. Esas operaciones conservan su cobertura de integración aislada.
2. **Geolocalización: lógica probada con proveedor sintético; permiso real pendiente.** Once pruebas ejecutan el código de las páginas para comprobar GPS ausente, permiso denegado, posición no disponible, espera agotada, ausencia de laboratorios abiertos, cálculo del abierto más cercano y construcción de las rutas. Las coordenadas son datos sintéticos definidos en las pruebas. No se recibió una posición física ni se aceptó/denegó el permiso real del sistema, y no se compartió ubicación personal con Maps.
3. **Producción: excluida por indicación expresa.** No se cambió dominio, HTTPS, permisos de servidor ni despliegue. Las comprobaciones de esta entrega son locales.
4. **Otros navegadores/dispositivos: sin certificar.** La comprobación de Brave quedó detenida antes de navegar por el proyecto. No se confirmó disponibilidad de un teléfono o tableta físicos. Se conserva la evidencia anterior de navegador integrado y tamaño móvil emulado; no equivale a una prueba física ni a compatibilidad certificada con Brave, Firefox o Safari.
5. **Fallos operativos: ejecutados contra procesos reales locales.** Se probaron consultas y escrituras concurrentes, corte de conexión durante una carga, terminación inesperada del proceso, reinicio y recuperación de sesión, almacenamiento no disponible, caída entre archivo y confirmación de base, y base bloqueada durante un guardado. También se probaron fallos y espera agotada de la actualización JSON, con recuperación para reintentar.
6. **Restauración completa: ejecutada.** Se extrajeron las fuentes y los documentos, se recuperó SQLite, se reinstalaron las dependencias fijadas y se reconstruyeron React y .NET. Se inició un proceso usando el proyecto restaurado y una copia de su base restaurada. Se comprobaron los seis accesos, una descarga privada y el rechazo de una descarga fuera del perfil autorizado. Se ejecutaron las 360 pruebas de esa versión recuperada. Los 47 archivos privados extraídos coinciden con los del ZIP.
7. **Contenido y datos institucionales: verificado lo disponible, sin inventar lo faltante.** Se comprobó que los datos originales permanezcan intactos, con integridad SQLite correcta y sin referencias rotas. Los tres laboratorios heredados siguen sin propietario o vigencia completos. No se aportaron fuentes definitivas para completar esos datos ni sustituir formularios y catálogos. No se inventaron propietarios, fechas de habilitación, artículos ni aprobaciones para ocultar esa incompletitud. Las pruebas usan el catálogo y los documentos configurados actualmente.

## Fallos encontrados y correcciones

- **Almacenamiento no disponible:** la operación devolvía un error 500. Ahora devuelve 503 con un mensaje recuperable y conserva los registros y archivos anteriores. Las peticiones mal formadas mantienen su tratamiento propio; no se confunden con un fallo de almacenamiento.
- **Archivo sin registro tras una caída:** al terminar el proceso entre copiar el PDF y confirmar SQLite, el archivo quedaba suelto. Al iniciar, los archivos generados sin referencia se conservan bajo `Storage:Root/recuperacion/<lote>/`, con manifiesto; no se borran. Se respetan documentos, actas, evidencias, firmas, plantillas actuales y plantillas históricas de copias congeladas. Si una copia histórica está dañada, se omite la recuperación automática para no mover una posible evidencia sin conocer sus referencias.
- **Exclusión de procesos durante la recuperación:** se hace cumplir el contrato de una instancia por SQLite mediante un archivo de exclusión junto a la base. Se libera al finalizar o caer el proceso. Esto impide que otro arranque recupere archivos mientras una instancia del proyecto está escribiendo en esa base.
- **Bloqueo de SQLite envuelto por la persistencia:** se reconoce también cuando llega dentro de un error de guardado y se devuelve 409. Se comprobó que el PDF temporal de esa operación se retira y que el expediente no cambia.
- **Actualización de gráficos sin respuesta:** la petición JSON ahora tiene una espera máxima de 15 segundos. Los errores de red, sesión, formato y servidor se rechazan sin reemplazar el último resultado; la interfaz conserva su manejo de error y permite volver a intentar. Las pruebas de esta rama son unitarias; no se completó su revisión visual nueva tras el bloqueo del control de Windows.

Los cambios no requieren migración y no se aplicaron escrituras sobre la base original. Se mantuvo DESIGN.md; no se cambiaron colores, tipografía ni disposición. El detector de Impeccable no informó hallazgos sobre las fuentes de interfaz modificadas. La revisión visual nueva de estados y dispositivos no se certifica en este cierre.

## Ensayo operativo final

- 60 consultas, con 12 en paralelo: 60 respuestas correctas. Mediana de 0,016 s, percentil 95 de 0,094 s y máximo de 0,125 s en este equipo y conjunto de datos.
- 16 escrituras, con 8 en paralelo y la misma revisión leída: una guardada y 15 rechazadas por versión antigua.
- Carga cortada desde el cliente: base y archivos anteriores sin cambios.
- Proceso terminado durante una carga incompleta: integridad SQLite correcta; base y archivos sin cambios. Tras reiniciar se recuperó la sesión.
- Almacenamiento temporalmente no disponible: 503; base y archivos sin cambios.
- Proceso terminado después de copiar un PDF y antes de confirmar la base: un archivo recuperable en la carpeta privada, cero archivos sin registro en la raíz y documentos registrados conservados.
- Base bloqueada durante el guardado: 409; base y archivos sin cambios.

Estos resultados describen un ensayo local con esos niveles de concurrencia y datos. No establecen una capacidad de producción ni certifican todos los fallos posibles de sistema operativo, disco, red o hardware.

## Evidencias y reproducción

- [Resumen](cierre-pendientes-20261023/resumen.json).
- [Ensayo final con procesos reales](cierre-pendientes-20261023/operaciones-finales.json).
- [Fallos antes y después de corregirlos](cierre-pendientes-20261023/fallos-reproducidos-y-corregidos.json).
- [Arranque y pruebas de la restauración](cierre-pendientes-20261023/restauracion-operaciones.json) y [360 pruebas de la copia restaurada](cierre-pendientes-20261023/restauracion-360.trx).
- [366 pruebas actuales](cierre-pendientes-20261023/suite-366.trx), [28 pruebas JavaScript](cierre-pendientes-20261023/javascript.tap) y [conservación de datos originales](cierre-pendientes-20261023/datos-originales.json).
- Código del ensayo: `tests/operational_audit.py`. Inicia y termina exclusivamente sus procesos hijos, exige una copia distinta de la base de su carpeta de proyecto y mantiene el entorno Development. Comandos de entrada: `probe`, `storage`, `crashcommit`, `busy`, `down`, `up`, `quit`. No debe apuntarse a datos en uso; los ensayos de caída están diseñados para copias desechables.

Punto previo verificado: `App_Data/checkpoints/20261022-234400-antes-cierre-pendientes/`, con 351 archivos en ZIP, copia SQLite, documentos privados e instrucciones. Para respaldos futuros, excluir `*.db.instance.lock`: es una exclusión de proceso y no forma parte de los datos. El respaldo de SQLite debe ser consistente y acompañarse de los documentos privados. La carpeta de recuperación también debe conservarse si existe; sus archivos no se deben reinsertar como aprobaciones sin revisar su procedencia.

No se hizo commit, despliegue ni reinicio de datos. Todos los procesos de ensayo iniciados por esta revisión se cerraron al terminar. Los pendientes de interfaz, GPS real, dispositivos e información institucional se dejan explícitos para no confundir pruebas simuladas o integración HTTP con una certificación visual o institucional.
