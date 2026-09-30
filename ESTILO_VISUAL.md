# Estilo visual oficial de SI_Lab

Este documento define el lenguaje visual que deben seguir las vistas de SI_Lab. La referencia principal es la demostración funcional y visual entregada en el video de la reunión:

```text
C:\Users\Usuario\OneDrive\Desktop\Agent-resources\Grabación de la reunión de JUAN y STEVEN-20260915_203246.mp4
```

El objetivo es que todos los módulos parezcan partes de una misma plataforma institucional, no aplicaciones independientes. Las exportaciones de `FigmaSedes` muestran dos composiciones principales: landing pública con barra azul y mapa, y consola autenticada con sidebar azul, encabezado superior y tarjetas operativas.

## 1. Concepto visual

SI_Lab debe verse como un **portal institucional moderno para trámites de salud**, con una interfaz clara, ordenada y confiable.

Principios:

- Sobrio y profesional, sin parecer una aplicación bancaria.
- Mucho espacio blanco y bloques de contenido fáciles de escanear.
- Azul institucional como color dominante.
- Verde para estados positivos y establecimientos abiertos.
- Rojo únicamente para estados negativos, cerrados o rechazos.
- Amarillo/ámbar para revisión, pendientes y observaciones.
- Formularios divididos en secciones visibles.
- Mapas y estados destacados como información prioritaria.
- Textos cortos, etiquetas claras y acciones evidentes.
- Diseño responsive para escritorio, tablet y móvil.

## 2. Paleta de colores

Usar estos valores como referencia común:

| Uso | Color | Código |
|---|---|---|
| Azul institucional principal | Barra/sidebar SEDES | `#0785C5` |
| Azul institucional oscuro | Botones y encabezados | `#1E3A5F` |
| Azul secundario | Enlaces y acciones | `#2563EB` |
| Fondo general | Gris azulado muy claro | `#F4F6F8` |
| Superficie | Blanco | `#FFFFFF` |
| Borde | Gris claro | `#E2E8F0` |
| Texto principal | Azul gris oscuro | `#1E293B` |
| Texto secundario | Gris pizarra | `#64748B` |
| Verde abierto/éxito | Verde | `#16835B` |
| Verde de resaltado | Verde claro | `#35C98B` |
| Rojo cerrado/rechazo | Rojo | `#C2413B` |
| Rojo de resaltado | Rojo claro | `#EF8178` |
| Estado en revisión | Ámbar | `#B45309` |
| Fondo en revisión | Amarillo claro | `#FEF3C7` |
| Acento público | Verde menta | `#66D8B1` |

No introducir colores nuevos para estados sin actualizar este documento y coordinarlo con el equipo.

## 3. Tipografía y jerarquía

Se prioriza la tipografía del sistema para no agregar dependencias innecesarias:

```text
Inter, system-ui, -apple-system, "Segoe UI", sans-serif
```

Jerarquía recomendada:

- Título principal de landing: grande, pesado y de alto contraste.
- Título de pantalla: `1.5rem` a `2rem`, peso 700/800.
- Título de tarjeta: `1rem` a `1.15rem`, peso 700/800.
- Texto normal: `0.9rem` a `1rem`.
- Texto auxiliar y metadatos: `0.75rem` a `0.85rem`.
- Etiquetas de estado: mayúsculas pequeñas, peso 700 y separación ligera entre letras.

No usar más de tres niveles de título en una pantalla.

## 4. Estructura global

Todas las pantallas deben conservar:

1. Navegación global consistente.
2. Fondo general `#F4F7F9`.
3. Contenedor centrado con ancho máximo aproximado de `1200px`.
4. Secciones agrupadas en tarjetas blancas.
5. Bordes suaves y sombras discretas.
6. Pie de página sencillo.

### Landing pública

La navegación pública debe usar una barra horizontal azul, con:

- Marca SI_Lab a la izquierda.
- Espacio para identidad institucional y Cochabamba.
- Búsqueda de laboratorio visible.
- Botón `Iniciar Sesión / Registrarse` a la derecha.

### Consola autenticada

Las pantallas autenticadas deben usar:

- Sidebar fijo azul de aproximadamente `280px`.
- Marca SI_Lab en la parte superior del sidebar.
- Navegación vertical con icono, texto y estado activo azul oscuro.
- Identidad institucional en el pie del sidebar.
- Encabezado superior blanco con breadcrumb, notificaciones y usuario.
- Fondo de trabajo gris muy claro.
- Contenido principal con ancho amplio y tarjetas blancas.

La navegación global debe mantener:

- Marca `SI_Lab`.
- Acceso a laboratorios públicos.
- Enlaces de propietario solo cuando el usuario tenga ese rol.
- Enlace a cuenta o inicio de sesión.
- No mostrar acciones de otros roles a usuarios no autorizados.

## 5. Landing pública y mapa

La landing pública es la pantalla más visual del sistema.

### Encabezado principal

- Fondo azul institucional con imagen o textura visual cuando exista un recurso aprobado.
- Texto blanco.
- Etiqueta institucional pequeña, por ejemplo `SEDES · PORTAL CIUDADANO`.
- Título grande orientado a la acción.
- Una palabra o fragmento puede resaltarse en verde menta.
- Botón blanco o de alto contraste para explorar laboratorios.
- Decoración geométrica sutil, sin competir con el contenido.

### Directorio

- Título de sección y contador de resultados.
- Barra de búsqueda destacada.
- Filtros por tipo y estado.
- Botón principal azul.
- Resultados en tarjetas blancas.
- Mapa visible junto al listado en escritorio, con panel lateral de filtros/resultados.
- En móvil, primero el listado y después el mapa.

### Tarjeta de laboratorio

Cada tarjeta debe mostrar:

- Icono o identificador visual del establecimiento.
- Tipo de laboratorio.
- Nombre.
- Servicios resumidos.
- Estado con punto de color:
  - verde: `Abierto`;
  - rojo: `Cerrado`.
- Acciones `Ver detalle` y `Cómo llegar`.

### Mapa

- Leaflet/OpenStreetMap en la implementación actual.
- Marcadores verdes para abiertos.
- Marcadores rojos para cerrados.
- Popup con nombre, tipo y enlace a detalle.
- Borde redondeado, altura suficiente y sombra suave.

## 6. Autenticación

La pantalla de login debe sentirse institucional y separada del contenido operativo:

- Composición dividida en dos columnas en escritorio.
- Panel izquierdo azul institucional con marca SI_Lab, mensaje institucional y referencia a Cochabamba.
- Panel derecho gris claro con tarjeta blanca de login.
- Ancho de tarjeta aproximado de `420px` a `480px`.
- Encabezado breve: bienvenida y propósito.
- Campos grandes y cómodos.
- Botón azul a ancho completo.
- Mensajes de validación debajo de los campos.
- En móvil, el panel institucional se coloca encima y se reduce su altura.

No mostrar información técnica de la autenticación al usuario final.

## 7. Formularios de trámite

Las pantallas de propietario, como `Nueva Solicitud`, deben seguir una estructura de formulario administrativo:

- Encabezado superior con breadcrumb y usuario.
- Sidebar con la opción `Nueva Solicitud` resaltada.
- Tarjeta principal blanca.
- Encabezado interno con icono, título y descripción.
- Secciones separadas por líneas y títulos:
  - datos del establecimiento;
  - ubicación;
  - requisitos/documentos;
  - acciones.
- Etiquetas arriba de cada campo.
- Inputs con bordes suaves y foco azul.
- Mensajes de validación visibles.
- Botón principal para enviar.
- Botón secundario para cancelar.

### Ubicación

- El mapa de previsualización debe ser un componente real, no un placeholder, ocupando el bloque izquierdo de la sección de ubicación.
- Debe mostrar un marcador.
- Debe actualizarse al cambiar latitud/longitud.
- Debe permitir arrastrar el marcador y devolver las coordenadas a los campos.
- Debe conservar altura y ancho adecuados en móvil.

### Documentos

- Zona de carga con borde discontinuo.
- Icono de carga.
- Descripción breve de formatos y límites.
- Mensajes de error debajo del selector.
- En futuras iteraciones, cada requisito deberá mostrarse como fila o tarjeta individual con estado:
  - pendiente;
  - cargado;
  - rechazado;
  - opcional.

## 8. Seguimiento de trámites

La vista de seguimiento debe parecer una bandeja de trámites:

- Encabezado con título y explicación breve.
- En la consola del propietario, usar tarjetas o tabla con:
  - nombre del laboratorio propuesto;
  - tipo;
  - fecha;
  - estado;
  - acción para ver detalle.
- Estado como badge redondeado:
  - `en revisión`: ámbar;
  - `aprobado`: verde;
  - `rechazado`: rojo.
- Nunca mostrar el JSON interno de `DatosLaboratorioPropuesto`.
- El detalle debe presentar la información en bloques legibles.
- Mantener el botón principal azul oscuro y los estados como badges suaves.

## 9. Paneles de roles futuros

Cuando se implementen los módulos de coordinador, supervisor, gerente, administrador y abogado, deben reutilizar la misma base visual:

- Panel con título, filtros y contador.
- Tarjetas o tablas según la cantidad de información.
- Acciones agrupadas en la columna derecha.
- Estados y prioridades visibles sin abrir el detalle.
- Confirmaciones para acciones destructivas.
- Modales solo para operaciones cortas; usar páginas completas para formularios largos.

### Coordinador

Debe priorizar:

- solicitudes recibidas;
- documentos;
- inspecciones;
- supervisor asignado;
- acciones de aprobar, rechazar o reasignar.

### Supervisor

Debe priorizar:

- agenda;
- establecimientos asignados;
- ruta de inspección;
- actas;
- observaciones y plazos de subsanación.

### Gerente

Debe priorizar:

- métricas;
- filtros por municipio y fechas;
- indicadores;
- descarga de informes.

### Administrador

Debe priorizar:

- usuarios;
- roles y permisos;
- requisitos obligatorios/opcionales;
- catálogos y configuración.

### Abogado

Debe priorizar:

- licencia;
- plano;
- revisión legal;
- firma;
- aprobación final.

## 10. Componentes reutilizables

Antes de crear estilos nuevos, reutilizar:

- `.silab-panel`
- `.silab-panel-header`
- `.silab-table`
- `.silab-status`
- `.public-lab-card`
- `.public-state`
- `.public-btn-primary`
- `.public-btn-outline`
- `.auth-card`
- `.auth-submit`

Si un patrón aparece en dos o más módulos, debe convertirse en estilo compartido en `wwwroot/css/site.css`, evitando copiar CSS extenso en cada vista.

## 11. Accesibilidad y responsive

- Todos los inputs deben tener etiqueta.
- Los botones deben indicar claramente su acción.
- Los estados no deben depender solo del color; deben incluir texto.
- Los mapas deben tener `aria-label`.
- Mantener contraste suficiente entre texto y fondo.
- En pantallas pequeñas:
  - las columnas pasan a una sola columna;
  - los filtros se apilan;
  - los botones pueden ocupar todo el ancho;
  - las tablas deben desplazarse horizontalmente o transformarse en tarjetas.

## 12. Qué no hacer

- No usar gradientes fuertes en formularios operativos.
- No llenar las pantallas de colores brillantes.
- No usar rojo para botones normales.
- No mostrar datos técnicos o JSON al usuario.
- No mezclar estilos de Bootstrap sin personalizarlos con la paleta del sistema.
- No crear una navegación diferente por módulo.
- No modificar el layout compartido sin coordinación del Integrante 1.
- No agregar una librería visual nueva sin justificarla.

## 13. Estado de implementación actual

La base visual ya aplicada incluye:

- landing pública azul con acento verde;
- directorio con tarjetas y filtros;
- mapa con estados verde/rojo;
- detalle y ruta;
- login institucional dividido en panel azul y tarjeta blanca;
- consola autenticada con sidebar azul y encabezado superior;
- formulario de nueva solicitud con tarjeta, secciones y mapa Leaflet;
- seguimiento con estados y presentación legible.

La siguiente etapa visual debe centrarse en uniformar las pantallas pendientes de los roles de Sprint 2 y Sprint 3, reutilizando esta guía en lugar de crear estilos independientes.
