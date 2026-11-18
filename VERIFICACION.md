## Estado vigente — pruebas locales del 23/10/2026

366 pruebas .NET y 28 JavaScript aprobadas. Restauración reconstruida desde el respaldo y arrancada, con sus 360 pruebas originales aprobadas. Ensayos reales locales de concurrencia, cortes de carga, reinicio inesperado, almacenamiento no disponible, caída entre archivo y confirmación y bloqueo de SQLite. Correcciones de recuperación privada de archivos, exclusión de instancia, respuestas 503/409 y espera limitada de actualización JSON. Sin migración ni cambios en la base original o sus 47 archivos privados.

Producción excluida por indicación del usuario. La ampliación de interfaz quedó detenida al no poder verificar la URL desde el control de Windows; GPS físico, otros dispositivos y contenido institucional pendiente no están certificados. Informe vigente: [Pendientes locales](docs/CIERRE_PENDIENTES_LOCALES_2026-10-23.md). Punto previo: `App_Data/checkpoints/20261022-234400-antes-cierre-pendientes/`. Los apartados siguientes conservan las revisiones anteriores.

## Auditoría ampliada — 22/10/2026 (revisión anterior)

360 pruebas .NET y 6 JavaScript aprobadas. Inventario de 69 acciones: 301 consultas por perfil, 603 enlaces, 318 comprobaciones de seguridad POST, 685 transiciones prohibidas, 17 permitidas y sus reenvíos, 228 casos de campos inválidos y los 11 permisos configurables. Navegación real con los seis perfiles y el portal público; correcciones de formularios, calendario, concurrencia de servicios y consistencia de renovaciones. Base original idéntica al respaldo y 47 archivos privados conservados, sin nueva migración. Siguen pendientes propietario/vigencia de tres laboratorios heredados y los contenidos institucionales definitivos.

Informe y evidencia: [Auditoría ampliada](docs/AUDITORIA_EXHAUSTIVA_2026-10-22.md). Punto previo: `App_Data/checkpoints/20261022-223121-antes-auditoria-exhaustiva/`. Las verificaciones siguientes describen etapas anteriores.

## Auditoría anterior — 91 pruebas, 22/10/2026

91 pruebas .NET y 4 JavaScript aprobadas; Release sin advertencias/errores. Se corrigieron agenda, actas, revisión documental/legal, edición de borrador, renovación, concurrencia de usuarios y recuperación de filtros; navegación más directa para supervisor/coordinador/admin. Un solo conjunto operativo, sin categorías demo/oficial. Base local con nueve migraciones, integridad `ok`, 0 referencias rotas y 0 cruces de agenda; registros previos y 47 archivos privados conservados. Cuatro horarios históricos corregidos con autorización y trazabilidad.

Informe y límites actuales: [Auditoría de flujos](docs/AUDITORIA_FLUJOS_2026-10-22.md). Evidencia: `docs/auditoria-flujos-20261021/`; pruebas: `tests/SiLab.Tests/TestResults/auditoria-flujos.trx`. Respaldo anterior: `App_Data/checkpoints/20261021-233744-antes-auditoria-flujos`. Las secciones siguientes conservan verificaciones anteriores.

## Supervisor, React y métricas — 21/10/2026

Resultado final: compilación Release con cero advertencias/errores; 62 pruebas de servidor aprobadas (0 fallidas, 0 omitidas) en `tests/SiLab.Tests/TestResults/supervisor-react-metricas.trx`, más cuatro pruebas JavaScript aprobadas. CI instala las dependencias fijadas, ejecuta ambas suites y genera React antes de compilar .NET. Se comprobó el paquete de publicación: incluye `operations.js`, su licencia y `operations.css`; excluye `ClientApp/`, dependencias de desarrollo y `App_Data/`. No se desplegó el proyecto ni se modificó Git.

Punto de restauración anterior: `App_Data/checkpoints/20261021-150038-antes-supervisor-react-metricas` (274 archivos, documentos privados incluidos, copia SQLite consistente e instrucciones). SHA-256 verificados. La base original coincide tabla por tabla con ese punto: 15 solicitudes, 39 registros de documentos, ocho migraciones; integridad SQLite `ok`, sin referencias rotas. Los 47 archivos privados coinciden con el ZIP y DESIGN.md no cambió. No se agregó ninguna migración. Evidencia: `docs/supervisor-react-20261021/integridad.json`.

Nuevas pruebas: cálculos sobre registros guardados y coherencia con CSV, promedio global de cierres, exclusión de borradores y rechazos de campo reprogramables, meses sin envíos, respuesta sin datos, actualización después de guardar un cambio, autorización de superficies y permisos suspendidos, agenda propia, programación desde la nueva vista y aislamiento de actas/citaciones/copia de formulario por supervisor. JavaScript verifica semanas, domingos, cambio de año, bisiesto, visitas nocturnas y rutas largas con ubicaciones ausentes o inválidas y límite móvil de Maps.

Recorrido en navegador sobre una copia temporal: supervisor programa visita del 23/10 de 14:00 a 15:00, guarda las verificaciones de una visita iniciada, adjunta un PDF ficticio y envía el acta; se confirma revisión pendiente y aparición en actas emitidas. Agenda móvil muestra esa visita al elegir viernes; enlace a rutas conserva el día y presenta la parada y su enlace de Maps. No se abrió navegación externa ni se enviaron mensajes externos. Gerente consulta gráficos reales de esa copia, actualiza, selecciona septiembre y prueba municipio sin trámites (sin tasa ni promedio inventados). Los ejemplos de las capturas pertenecen únicamente a esa copia.

Composición revisada a 1280 px y 1440 × 1000 en escritorio, y 390 × 844 en móvil. Sin desbordamiento horizontal del documento: 1265/1425 px útiles en escritorio y 375 px en móvil. El calendario conserva desplazamiento horizontal dentro de su región y mantiene visible el día elegido; en móvil presenta un solo día. Muestreo DOM de contraste en agenda/métricas sin pares inferiores a 4,5:1; consola consultada sin errores/advertencias. No implica certificación WCAG ni pruebas de hardware táctil, lector de pantalla o navegación real en Maps. Auditoría mecánica de Impeccable sobre los componentes y vistas nuevos: ninguna observación (`impeccable.json`). Capturas y medidas en `docs/supervisor-react-20261021/`.

## Verificación administrativa — 21/10/2026

Resultado: 53 pruebas aprobadas, 0 fallidas y 0 omitidas; compilación Release con cero advertencias y errores. Informe de pruebas: `tests/SiLab.Tests/TestResults/admin-secciones.trx`. Se incorporaron siete pruebas aisladas de edición conjunta de permisos, vigencia en sesiones abiertas, rechazos de capacidades ajenas/duplicadas/incompletas, recuperación de acceso administrativo, alta/edición/movimiento/orden/estados/eliminación de requisitos y secciones, conflictos de versión y compatibilidad de migración y copias históricas.

Punto previo completo: `App_Data/checkpoints/20261021-143538-antes-admin-secciones` (259 archivos en ZIP, documentos privados incluidos, SQLite consistente, SHA-256 e instrucciones). La octava migración se aplicó a la base local: integridad `ok`, ninguna referencia rota y todos los campos anteriores conservados en las once tablas previas, incluidos los 39 documentos y 14 solicitudes. Evidencia: `docs/admin-20261021/integridad.json`.

Navegador en copia temporal: se desmarcó y guardó un permiso del coordinador, se reabrió el editor verificando su persistencia y se comprobó el historial con autor y fecha; se creó una sección y un requisito desde ella y se verificó su publicación agrupada. Revisión a 1280 px y 390 × 844: editor, matriz y requisitos sin desbordamiento horizontal del documento (375 px útiles en móvil). Se corrigió la flotación heredada de `legend` que desplazaba la primera casilla. Muestreo DOM de contraste en editor y requisitos: sin pares inferiores a 4,5:1; no implica certificación WCAG. Capturas y medidas en `docs/admin-20261021/`. Los ejemplos de esas capturas están solo en la copia temporal.

La revisión mecánica de Impeccable señaló avisos de tipografía y radios ya presentes en la hoja global anterior; no se cambiaron superficies ajenas a este trabajo. Se conserva DESIGN.md sin alteraciones. No se retiraron catálogos ni validación de datos: se explicó su propósito al usuario, dado que su último mensaje fue una consulta sobre esos apartados.

# Verificación de SI Lab

Revisión del 21/10/2026 con Impeccable: DESIGN.md leído y conservado como especificación principal. Se ajustó la capa visual compartida, sin modificaciones a controladores, servicios, entidades, migraciones ni cálculos. Punto de control previo en `App_Data/checkpoints/20261021-135546-antes-impeccable`; pruebas en `tests/SiLab.Tests/TestResults/design-impeccable.trx`.

La revisión técnica cubrió tokens/tema, contraste de texto, controles y foco, tamaños responsive y coste de los recursos. El hook de diseño de Impeccable no informó defectos deterministas en el nuevo CSS. No se agregó ninguna biblioteca ni JavaScript de ejecución. En el formulario, escritorio de 1280 px, tableta de 1024 px y móvil de 390 px no presentaron desbordamiento horizontal; los botones móviles medidos fueron de 44 px. El menú respondió a Enter y el enlace para saltar al contenido enfocó `main-content`. Se corrigieron restos de superficies claras, enlaces con el azul anterior y el icono de los selectores.

Los pares de tokens medidos dieron 17,90:1 para texto principal sobre panel, 5,86:1 para texto secundario, 4,70:1 para blanco sobre botón primario y 7,32:1 para texto oscuro sobre hover lavanda. Las comprobaciones de texto del formulario y la tabla de usuarios no mostraron pares inferiores a 4,5:1 en el muestreo DOM. Es una comprobación de esos textos, no una certificación WCAG de mapas, PDF ni todas las rutas. La revisión móvil emplea viewport emulado en el navegador integrado; no se probó hardware táctil ni lector de pantalla. Capturas en `docs/diseno-20261021/`.

Fecha: 18 de octubre de 2026. Resultado después de aplicar el plan aprobado de correcciones. Este documento sustituye como estado actual la verificación del 12 de octubre; el diagnóstico previo se conserva en `docs/AUDITORIA_Y_PLAN_2026-10-18.md`.

## Resultado ejecutado

Actualización visual FigmaSedes: compilación Release con cero errores/advertencias y las mismas 46 pruebas aprobadas (`tests/SiLab.Tests/TestResults/estilo-figma.trx`). Se revisaron acceso, nueva solicitud, trámites, usuarios y portal público en el navegador sobre una copia temporal. Formulario y trámites a 390 × 844 sin desbordamiento horizontal (ancho útil 375 px y contenido 375 px); menú móvil abre y permite navegar. En la comprobación final del formulario no se registraron errores/advertencias de consola. Capturas en `docs/estilo-figma/`.

Punto de control previo: `App_Data/checkpoints/20261018-194800-antes-estilo-figma` con ZIP, SQLite, SHA-256 e instrucciones. Los hashes se volvieron a verificar al finalizar y todas las tablas locales coinciden con ese respaldo. Esta actualización modifica presentación y navegación; no agrega migraciones ni altera los registros locales.

- Compilación Release de la solución con `--warnaserror`: 0 errores, 0 advertencias.
- Suite completa después de mejorar requerimientos y archivos: 46 pruebas ejecutadas, 46 aprobadas, 0 fallidas, 0 omitidas.
- Modelo EF Core: siete migraciones; `DocumentOriginalName` agrega el nombre visible de los archivos sin alterar los existentes.
- Base local actualizada después de respaldo: integridad SQLite `ok`, sin referencias rotas.
- Comparación de campos originales contra el respaldo de la auditoría: todos conservados en las nueve tablas de negocio anteriores; las pruebas automáticas usan bases temporales. La carga demo posterior solicitada por el usuario sí agrega ejemplos a la base local, después de un segundo respaldo.
- Kestrel real: peticiones con longitud declarada de unos 12 MB a carga individual y unos 56 MB a registro inicial respondieron 413 antes de procesar el formulario.
- Navegador: directorio/requisitos, login, revisión individual, programación del supervisor y métricas comprobados con ejemplos en una copia temporal.
- Detalle y métricas a 390 × 844: sin desbordamiento horizontal del documento. Se revisó también escritorio a 1280 px. Las tablas conservan su desplazamiento interior.
- Consola del navegador en el recorrido final consultado: sin errores ni advertencias.

Entorno: Windows, SDK .NET 9.0.315 compilando destino .NET 8, EF Core 8.0.20, SQLite, ASP.NET Core TestServer y Kestrel. No se modificaron protecciones de Windows. Las pruebas se ejecutaron fuera del aislamiento porque el registro de eventos de Windows y las claves de sesión no eran accesibles dentro de él.

## Cobertura de la suite

Requerimientos y archivos: dos pruebas adicionales verifican el acceso público al apartado, enlaces desde los menús y registro, requisitos activos/inactivos, ocho PDF iniciales con nombres conservados, un noveno PDF individual, retiro/reposición de un obligatorio, bloqueo del envío incompleto y protección por versión/etapa. Evidencia: `tests/SiLab.Tests/TestResults/requerimientos-archivos.trx`. En navegador se seleccionó un archivo y luego otros siete, se quitó y repuso uno, se comprobó que una selección repetida no duplica y que un TXT se excluye; los ocho PDF llegaron al borrador. La captura está en `docs/requerimientos-archivos/lista-pdf.png`. Estas comprobaciones se realizaron sobre una copia temporal, sin agregar expedientes de prueba a la base local.

Antes de aplicar `20261018233307_DocumentOriginalName`, se respaldó la base en `App_Data/backups/20261018-193411-antes-requerimientos`. Tras aplicarla, todos los registros y campos anteriores coinciden con el respaldo, integridad `ok` y cero referencias rotas. Los nombres antiguos permanecen vacíos; los nuevos se guardan sin rutas y se conservan al asociar documentos.

La ampliación de datos demo agrega dos pruebas de integración: carga de 13 expedientes con documentos accesibles, historial y tareas para los seis perfiles; reinicio sin duplicados ni pérdida de avances; conservación de cuentas/catálogos/registros previos y creación de cuentas faltantes. También se recorren las pantallas de cada perfil y se aprueba por HTTP el expediente demo de revisión legal hasta su cierre. La prueba de producción confirma que no se crean escenarios ni cuentas demo incluso si ambas opciones están habilitadas. Evidencia: `tests/SiLab.Tests/TestResults/datos-demo.trx`.

Se conservaron los 16 casos existentes y se ampliaron sus recorridos para revisar cada PDF antes de aprobar la documentación. El total de 46 incluye casos parametrizados; no son 46 recorridos completamente independientes.

La suite cubre:

- Apertura y renovación completas, estado anual, duplicados básicos y cierre legal firmado.
- Aislamiento de propietarios, supervisores asignados y descargas privadas.
- Roles incorrectos, permisos suspendidos en sesiones existentes y acciones nuevas de documentos/actas/citaciones.
- Edición administrativa que revoca cookies, correo duplicado y validación de campos.
- Antiforgery, logout y 31 intentos desde un mismo cliente que activan el límite 429.
- Coordenadas inválidas y datos oficiales dañados/incompletos que no pueden habilitar un laboratorio.
- Archivos PDF falsos, incompletos, demasiado grandes y versiones obsoletas del expediente.
- Revisión individual, devolución subsanable, versiones de PDF, conservación del original y del catálogo recibido.
- Asociación de una carga inicial sin volver a cargar el archivo.
- Retirada de complementos observados sin borrar la historia ni retirar documentos obligatorios.
- Catálogo totalmente opcional que puede cerrar su revisión sin exigir un documento artificial.
- Observación legal que no puede saltar controles y que no repite inspección salvo indicación.
- Rechazo legal definitivo y bloqueo de reactivación por asignación.
- Organización de intervalos por supervisor, conflictos reales y exclusión de la propia reserva.
- Dos decisiones simultáneas sobre la misma versión: una se aplica y otra responde conflicto.
- Asignaciones simultáneas de dos solicitudes al mismo horario: solo una reserva queda activa.
- Actas estructuradas y plantillas copiadas a la inspección, preparación previa a firma y rechazo de resultado aprobado con incumplimientos.
- Citaciones con PDF y con imagen PNG, evidencia obligatoria, descarga con tipo correcto y avisos sin duplicación a propietario/supervisor.
- Imágenes falsas JPG/PNG rechazadas por su contenido.
- Avisos de subsanación, cierres por vigencia y feriados configurados.
- Informes de cierre que permanecen idénticos aunque cambie el laboratorio después.
- Métricas/CSV con borradores separados y plantillas públicas separadas de expedientes privados.
- Captura desde navegador con campos opcionales enviados vacíos.
- Rechazo de longitud HTTP excesiva antes de la interpretación del formulario.
- Producción que oculta laboratorios sin validar y no crea requisitos demo.
- Migración desde Sprint 1 y desde octubre con documentos existentes; conserva registros y distingue la aprobación global heredada sin inventar revisores/fechas.

Los clientes diferentes de TestServer tienen direcciones diferentes mediante un filtro exclusivo del proyecto de pruebas. La aplicación real no confía en esa cabecera y el límite de login continúa activo.

## Evidencias

- Resultado TRX: `tests/SiLab.Tests/TestResults/correcciones-final.trx` (ignorado por Git).
- Resumen portable: `docs/auditoria-2026-10-18/pruebas-finales.json`.
- Conservación e integridad de datos locales: `docs/auditoria-2026-10-18/datos-despues.json`.
- Rechazos de Kestrel: `docs/auditoria-2026-10-18/limites-http.json`.
- Captura de métricas: `docs/auditoria-2026-10-18/metricas-corregidas.png`.
- Correspondencia de casos de uso: `docs/CASOS_DE_USO.md`.

La comprobación de Kestrel usó cabeceras con tamaño declarado excesivo y token válido para confirmar el rechazo previo, sin completar el envío de esos cuerpos. Se corrigió el fallo detectado en la prueba real que podía acabar en 500 por un formulario no leído. Los límites de archivo y las validaciones de PDF/imágenes se ejercitan aparte en la suite.

## Repetir

```powershell
dotnet restore SEDESLABORATORIO.sln
dotnet build SEDESLABORATORIO.sln -c Release --no-restore --warnaserror
dotnet test tests/SiLab.Tests/SiLab.Tests.csproj -c Release --no-build --logger "trx;LogFileName=correcciones-final.trx"
```

La automatización de CI exige al menos 46 pruebas y ninguna fallida. Está preparada, pero no se ejecutó remotamente ni se hizo push.

## Límites y pendientes externos

La ejecución comprueba los escenarios anteriores; no certifica ausencia absoluta de defectos. No hubo prueba de carga productiva, despliegue, validación de firma criptográfica, antivirus ni ruta física usando ubicación personal.

Las fuentes oficiales de tipos, municipios, artículos, formularios, feriados y datos reales siguen pendientes de entrega/configuración. Los tres laboratorios demo locales continúan sin propietario/vigencia y no se convirtieron en datos institucionales. La base local no contiene expedientes reales con PDF que puedan certificarse. Las pruebas usan archivos y expedientes sintéticos en almacenamiento temporal.

La corrección técnica está implementada; la aceptación institucional requiere esos insumos y el contraste de datos reales. El detalle del cierre está en `docs/CIERRE_CORRECCIONES_2026-10-18.md`.
