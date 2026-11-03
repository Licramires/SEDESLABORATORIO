from pathlib import Path
import json, re
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).parent
doc = Document()
sec = doc.sections[0]
sec.page_height=Inches(11.69); sec.page_width=Inches(8.27)
sec.top_margin=Inches(.72); sec.bottom_margin=Inches(.68)
sec.left_margin=sec.right_margin=Inches(.78)
normal=doc.styles['Normal']; normal.font.name='Calibri'; normal.font.size=Pt(11)
normal.paragraph_format.space_after=Pt(7)
normal.paragraph_format.line_spacing=1.08
for name,size in [('Title',32),('Subtitle',15),('Heading 1',21),('Heading 2',13),('Heading 3',11)]:
 s=doc.styles[name]; s.font.name='Calibri'; s.font.size=Pt(size)
 s.font.color.rgb=RGBColor.from_string('000000' if name=='Title' else '18384B')
 s.paragraph_format.space_before=Pt(10); s.paragraph_format.space_after=Pt(7)
 s.paragraph_format.keep_with_next=True
 for border in list(s.element.xpath('./w:pPr/w:pBdr')):
  border.getparent().remove(border)
doc.styles['Caption'].font.size=Pt(9)
doc.core_properties.title='Informe integral del proyecto SI Lab'
doc.core_properties.subject='Casos de uso SQA SOLID y patrones de diseño'
doc.core_properties.author=''
doc.core_properties.keywords='SI_Lab, casos de uso, SQA, SOLID, arquitectura'
lang=OxmlElement('w:lang'); lang.set(qn('w:val'),'es-BO'); normal.element.get_or_add_rPr().append(lang)
footer=sec.footer.paragraphs[0]; footer.alignment=2
footer.add_run('SI Lab  |  26 de octubre de 2026  |  ').font.size=Pt(8)
field=OxmlElement('w:fldSimple'); field.set(qn('w:instr'),'PAGE'); footer._p.append(field)

def p(t,style=None): return doc.add_paragraph(t,style)
def h(t): doc.add_heading(t,2)
def page(title):
 doc.add_page_break(); doc.add_heading(title,1)
def b(label,text):
 x=p(''); x.add_run(label+' ').bold=True; x.add_run(text); return x
def table(headers,rows,widths=None):
 t=doc.add_table(rows=1,cols=len(headers)); t.style='Light Shading Accent 1'
 # Consistent restrained blue palette independent of the default Office theme.
 for c,txt in zip(t.rows[0].cells,headers):
  c.text=txt
  for r in c.paragraphs[0].runs: r.bold=True; r.font.color.rgb=RGBColor.from_string('18384B')
 repeat=OxmlElement('w:tblHeader'); t.rows[0]._tr.get_or_add_trPr().append(repeat)
 for row in rows:
  cells=t.add_row().cells
  for c,txt in zip(cells,row):
   c.text=str(txt)
   for par in c.paragraphs:
    par.paragraph_format.space_after=Pt(4); par.paragraph_format.line_spacing=1.02
    for r in par.runs: r.font.size=Pt(9.5)
  no=OxmlElement('w:cantSplit'); t.rows[-1]._tr.get_or_add_trPr().append(no)
 if widths:
  for row in t.rows:
   for c,w in zip(row.cells,widths): c.width=Inches(w)
 p('')
 return t
def code(t):
 x=p(t); x.paragraph_format.space_before=Pt(4); x.paragraph_format.space_after=Pt(8)
 for r in x.runs: r.font.name='Consolas'; r.font.size=Pt(9)
 shade=OxmlElement('w:shd'); shade.set(qn('w:fill'),'F1F4F6'); x._p.get_or_add_pPr().append(shade)

p('Informe integral del proyecto SI Lab','Title')
p('Funcionalidad aseguramiento de calidad y arquitectura de software','Subtitle')
p('26 de octubre de 2026')
p('Versión documental 1')
p('')
p('Sistema de gestión de registro inspección y habilitación de laboratorios','Heading 2')
p('SI_Lab integra la consulta pública de laboratorios y el trámite de apertura o renovación, desde la recepción documental hasta la resolución legal. Este informe reúne la especificación funcional, la evidencia de aseguramiento de calidad y las decisiones de arquitectura para su evaluación técnica y funcional.')
p('El estado verificado al 26 de octubre incluye 389 pruebas de servidor aprobadas, 28 pruebas JavaScript aprobadas y el cierre de los tres bugs confirmados en la campaña de SQA. La aceptación completa conserva comprobaciones manuales e institucionales pendientes.')
table(['Contenido','Ubicación'],[
 ('Resumen alcance y tecnología','Páginas 2 a 3'),('Flujo datos y seguridad','Páginas 4 a 6'),
 ('36 casos de uso','Páginas 7 a 15'),('SQA resultados bugs y recuperación','Páginas 16 a 19'),
 ('SOLID y patrones de diseño','Páginas 20 a 22'),('Pendientes y fuentes','Páginas 23 a 24')])
p('Ámbito de evaluación','Heading 2')
p('Aplicación y pruebas locales. La producción queda fuera del alcance. Los resultados de integración, navegación y pruebas simuladas se identifican por separado para conservar su significado.')

page('1 Resumen del proyecto')
h('Propósito y alcance funcional')
p('El proyecto organiza expedientes de laboratorios, controla la documentación exigida, programa inspecciones, registra actas y citaciones y permite resolver la habilitación con intervención legal. El portal público facilita consultar establecimientos, servicios, requisitos y ubicación. La gerencia dispone de métricas calculadas sobre los registros guardados.')
table(['Actor','Responsabilidad principal'],[
 ('Público','Consultar requisitos, buscar laboratorios, abrir fichas y solicitar rutas.'),
 ('Propietario','Crear y subsanar solicitudes; adjuntar PDF; consultar estado, avisos y establecimientos; renovar.'),
 ('Coordinador','Revisar documentos, asignar o reprogramar inspecciones y validar las actas.'),
 ('Supervisor','Organizar visitas asignadas, preparar y presentar actas y emitir citaciones.'),
 ('Abogado','Examinar el expediente y aprobar, observar o rechazar la resolución legal.'),
 ('Gerente','Consultar métricas, filtros y reportes de gestión.'),
 ('Administrador','Gestionar usuarios, capacidades por rol, secciones, requisitos, plantillas y catálogos.')])
h('Resultado de la evaluación')
p('El circuito principal tiene controles de etapa, autorización, documentos, agenda y concurrencia. La refactorización SOLID separa decisiones de negocio, consultas y servicios de infraestructura. Los tres defectos registrados en el reporte de bugs tienen corrección y evidencia de reejecución. [S2, S3, S5]')
p('La campaña histórica de 183 casos mantiene 172 exitosos, 2 fallidos y 9 bloqueados. Ese registro no se sustituye por el resultado posterior de regresión. Permanecen nueve casos manuales o parciales sin certificar y el recorrido integral E2E-EXT-04 conserva un alcance mayor que las correcciones verificadas. [S4, S5]')

page('2 Tecnología y arquitectura general')
table(['Elemento','Uso comprobado'],[
 ('ASP.NET Core MVC y .NET 8','Controladores HTTP, validación, autorización y vistas Razor.'),
 ('Entity Framework Core 8.0.20','Consultas, seguimiento de entidades, migraciones y transacciones.'),
 ('SQLite','Persistencia local con restricciones y control de concurrencia.'),
 ('React 19.3.1 y React DOM','Componentes interactivos integrados en páginas existentes.'),
 ('esbuild 0.28.2','Construcción de los recursos del cliente.'),
 ('Leaflet 1.9.4 y OpenStreetMap','Mapas públicos y marcadores con recursos externos.'),
 ('Google Maps','Enlaces para abrir indicaciones de ruta.'),
 ('PDF y almacenamiento privado','Documentación, actas, evidencias y cierre del expediente.')])
h('Separación de responsabilidades')
p('La presentación combina Razor con componentes React para las áreas que necesitan interacción, como agenda, acta y métricas. El proyecto conserva MVC; React no reemplaza todo el sistema por una aplicación de página única. [S1, S8]')
code('Navegador y componentes React\n        ↓ peticiones HTTP\nFiltros de seguridad y controladores MVC\n        ↓ contratos de aplicación\nDecisiones de trámite y consultas especializadas\n        ↓ puertos de datos archivos y reloj\nEF Core con SQLite y almacenamiento privado')
p('Application contiene contratos y servicios de trámite y métricas. Infrastructure reúne implementaciones e inyección de dependencias. Data adapta el contexto de EF a los contratos. Models y Views presentan resultados al usuario. Los puertos de consulta conservan DbSet y LINQ; cambiar de ORM requeriría adaptar esas consultas. [S3, S7]')
h('Servicios externos y límites')
p('El mapa requiere recursos externos. Las rutas abren Maps; el sistema calcula cercanía geográfica, sin afirmar optimización por tráfico. Los avisos implementados son internos. No existe una integración contratada de envío de correo o SMS, ni una validación criptográfica de firmas de los PDF. [S2]')

page('3 Flujo de trabajo y estados')
table(['Etapa','Actor y condición de avance'],[
 ('Borrador y envío','El propietario completa datos y requisitos obligatorios vigentes.'),
 ('Revisión documental','El coordinador revisa cada versión y devuelve, rechaza o declara conformidad.'),
 ('Asignación y agenda','Se asigna un supervisor activo y un intervalo sin solapamientos.'),
 ('Inspección y acta','El supervisor prepara el contenido y adjunta el acta firmada cuando corresponde.'),
 ('Revisión del acta','El coordinador acepta el resultado o devuelve para nueva inspección.'),
 ('Revisión legal','El abogado verifica los antecedentes y emite una decisión sustentada.'),
 ('Habilitación y cierre','La aprobación legal habilita el laboratorio, fija vigencia y conserva el PDF histórico.'),
 ('Renovación','Se recorre el circuito conservando el establecimiento y la historia de sus cierres.')])
h('Significado de los estados')
p('Los estados persistidos son borrador, en_revision, aprobado, inspeccion, revision_acta, observado, rechazado, revision_legal y habilitado. El estado aprobado representa conformidad documental y espera de inspección; la habilitación requiere una decisión legal positiva. [S1, S2]')
p('Las observaciones documentales, de campo y legales conservan su origen para elegir el retorno permitido. La subsanación legal vuelve a revisión legal después de su revisión documental, salvo que se haya exigido una nueva visita. El rechazo definitivo no se utiliza como una observación temporal ni se reactiva por asignar supervisor.')
h('Reglas que evitan flujos contradictorios')
p('Las decisiones exigen el rol autorizado y la etapa adecuada antes de modificar el expediente. Un acta con incumplimientos no puede terminar globalmente aprobada. La revisión legal requiere documentos completos y revisados y un acta conforme validada. La agenda bloquea coincidencias totales y parciales, incluso con visitas que ya tienen acta.')
p('La vigencia anual de una renovación parte del vencimiento activo o de la aprobación si ya venció. Los plazos de subsanación usan días hábiles y feriados configurados. Las alertas avisan; no aplican sanciones ni rechazos automáticos. [S2, S6]')

page('4 Datos documentos y trazabilidad')
table(['Información','Control de integridad y conservación'],[
 ('Usuarios y permisos','Rol, estado activo, versión y registro de cambios de capacidades.'),
 ('Solicitud y laboratorio','Relación con propietario, tipo de trámite, estado, ubicación y establecimiento.'),
 ('Requisitos y secciones','Actividad, obligatoriedad, orden y versiones; copia congelada al primer envío.'),
 ('Documentos','Versión por requisito, revisión, observación y descarga autorizada.'),
 ('Inspecciones y actas','Supervisor, intervalo, lista de verificación versionada, resultado y evidencia.'),
 ('Citaciones y avisos','Motivo, emisión, vencimiento, evidencia y notificaciones internas.'),
 ('Historial y cierre','Actor, fecha y decisión; PDF inmutable de cada cierre.')])
h('Carga de archivos')
p('La selección permite agregar PDF progresivamente a una lista y retirar elementos antes del envío. La funcionalidad admite más de cinco documentos. El límite individual es de 10 MB y el total de la carga inicial es de 50 MB. Un borrador puede guardarse sin archivos; al enviarlo se comprueban los requisitos obligatorios. [S1, S2]')
p('Los nombres de almacenamiento son privados y generados por el sistema. Las descargas requieren autorización. El tratamiento distingue documentos actuales de versiones anteriores, de modo que reemplazar un archivo no borra la evidencia que fundamentó decisiones previas.')
h('Concurrencia y recuperación')
p('La versión del expediente impide que una pantalla antigua sobrescriba cambios nuevos. Los puertos de una petición comparten el mismo contexto EF y las operaciones de decisión utilizan una transacción. Cuando una carga falla se limpia el archivo nuevo que no debe quedar asociado. Una caída entre archivo y confirmación se atiende mediante recuperación privada de archivos sin referencia. [S6, S7]')
h('Calidad de datos institucionales')
p('La ubicación inválida no se publica como marcador o destino de ruta. La ficha sigue disponible con su dirección y un aviso. Los tres laboratorios heredados con propietario o vigencia incompletos permanecen identificados para corrección. Catálogos, formularios, artículos y feriados definitivos requieren validación institucional. Los registros de prueba siguen las mismas reglas operativas que los demás registros. [S2, S5]')

page('5 Seguridad y validaciones')
table(['Área','Validaciones implementadas y efecto esperado'],[
 ('Acceso','Autenticación por cookies, comprobación del rol y capacidades en servidor.'),
 ('Aislamiento','El propietario ve sus expedientes; el supervisor actúa en visitas asignadas; las descargas respetan el perfil.'),
 ('Sesiones','Cookie HttpOnly y SameSite Lax, duración de ocho horas con renovación y revocación tras cambios relevantes.'),
 ('Formularios','Protección antiforgery, obligatoriedad, catálogos válidos y control de versión.'),
 ('Agenda','Inicio y fin coherentes, supervisor activo y ausencia de solapamientos.'),
 ('Documentos y acta','Tipo y tamaño de archivo, requisitos completos, notas exigidas por observación y coherencia de resultados.'),
 ('Datos públicos','Latitud entre −90 y 90 y longitud entre −180 y 180, incluidos los límites.'),
 ('Métricas y exportación','Filtros consistentes, separación de borradores y protección del CSV contra fórmulas.'),
 ('Operaciones fallidas','Errores recuperables y retorno a la versión vigente sin repetir un envío obsoleto.')])
p('El acceso aplica autenticación, autorización y validación del recurso. Ocultar un botón es una ayuda de interfaz; la misma restricción debe comprobarse en el servidor. El acceso directo por URL o una petición modificada no puede saltar las reglas del flujo. [S1, S7]')
p('El inicio de sesión limita intentos a 30 por minuto por IP. La configuración de producción contempla cookies seguras, pero este informe no certifica HTTPS, dominio ni permisos de un despliegue productivo. [S1, S6]')
h('Alcance de la verificación')
p('La batería de integración cubre valores inválidos, permisos, estados, aislamiento y archivos. La comprobación visual completa de cada control en todos los estados continúa pendiente. Los ensayos con PDF preparados para pruebas verifican el tratamiento del archivo y su circuito, sin acreditar la autenticidad de una firma institucional.')

# Each specification states the normal route and the principal rejection or alternate route.
cases=[
('CU01','Consultar inicio y requisitos','Público','Catálogo publicado disponible.','Abrir el portal; consultar secciones y requisitos activos para el registro.','La institución debe completar los ocho tipos definitivos y las plantillas oficiales.','Requisitos visibles y ordenados sin iniciar sesión.'),
('CU02','Buscar laboratorios y consultar el mapa','Público','Laboratorios registrados.','Escribir nombre, tipo o servicio; aplicar filtros; seleccionar ficha o marcador.','Sin resultados se informa la ausencia. Las ubicaciones inválidas no generan marcadores; la consulta debe seguir utilizable si falta el mapa.','Listado y mapa corresponden a la búsqueda.'),
('CU03','Consultar ficha del laboratorio','Público','Identificador existente.','Abrir ficha; revisar dirección, servicios, contacto, código y ubicación registrada.','Un identificador inexistente se controla. Una ubicación inválida mantiene la ficha y avisa que requiere corrección.','Datos disponibles consultables sin inventar información.'),
('CU04','Obtener una ruta','Público','Destino válido y laboratorio disponible para el cálculo.','Solicitar ubicación o indicar origen manual; elegir el abierto más cercano por distancia geográfica; abrir Maps.','GPS denegado o sin respuesta ofrece alternativa manual. Sin destino válido no se crea una ruta.','Enlace de navegación válido; permiso físico pendiente de certificar.'),
('CU05','Iniciar sesión por rol','Los seis perfiles','Usuario activo con credenciales válidas.','Ingresar credenciales; validar cuenta; abrir menú y funciones autorizadas.','Credenciales incorrectas, suspensión, límite de intentos o sesión revocada impiden el acceso.','Sesión autenticada con capacidades comprobadas en servidor.'),
('CU06','Crear solicitud de apertura','Propietario','Sesión activa y catálogos disponibles.','Completar datos del laboratorio y coordenadas; validar opciones; guardar borrador.','Campos inválidos o catálogos ajenos se rechazan. Se puede guardar sin adjuntar documentos.','Borrador propio persistido para completar después.'),
('CU07','Adjuntar documentación por requisito','Propietario','Solicitud editable y requisito aplicable.','Agregar PDF a la lista; retirar selecciones si es necesario; guardar y consultar versiones.','Archivo inválido o exceso de tamaño impide la carga. Solo usuarios autorizados descargan documentos privados.','Documentos asociados al expediente con versiones conservadas.'),
('CU08','Enviar solicitud a revisión','Propietario','Borrador o subsanación editable, con versión vigente.','Completar requisitos obligatorios; reemplazar observados; confirmar envío.','Faltantes o documentos observados sin corregir bloquean el avance. Sin obligatorios no se exige un archivo artificial.','Expediente enviado y requisitos congelados según el primer envío.'),
('CU09','Consultar estado e historial propio','Propietario','Expediente perteneciente al usuario.','Abrir solicitud; revisar estado, documentos, decisiones y avisos.','Otro propietario no obtiene el expediente ni sus archivos por cambiar el identificador.','Seguimiento de la evolución sin modificar decisiones pasadas.'),
('CU10','Revisar documentación','Coordinador','Solicitud en etapa documental y documentos disponibles.','Abrir cada versión; aprobar u observar con motivo; resolver la revisión conjunta.','No se aprueba con pendientes. Una devolución subsanable se diferencia del rechazo definitivo; repetir una aprobación no duplica efectos.','Documentación conforme o devolución sustentada y notificada.'),
('CU11','Asignar supervisor','Coordinador','Conformidad documental y supervisor activo.','Seleccionar supervisor; indicar horario si corresponde; confirmar asignación.','Se bloquean saltos de etapa, supervisores inactivos y horarios en conflicto.','Visita asignada con historial y aviso.'),
('CU12','Consultar historial de asignaciones','Coordinador y perfiles autorizados','Expediente con inspecciones.','Abrir historial; identificar supervisores, visitas anteriores y motivos.','La reasignación no elimina la visita histórica ni habilita acceso fuera del perfil.','Trazabilidad de quién tuvo la responsabilidad de inspección.'),
('CU13','Programar la agenda','Supervisor asignado','Visita activa programable.','Seleccionar inicio y fin; comprobar disponibilidad; guardar y avisar al propietario.','Se rechazan intervalos idénticos, parciales o envolventes. Las visitas con acta reservan horario; intervalos contiguos son admisibles.','Agenda consistente sin doble reserva del mismo supervisor.'),
('CU14','Consultar visitas y rutas del día','Supervisor','Visitas propias con fechas y ubicaciones registradas.','Elegir el día; revisar orden por horario; abrir indicaciones de cada visita.','Sin visitas se muestra estado vacío. Una ubicación inválida no genera destino. No se calcula tráfico.','Itinerario diario por horario; recorrido completo de enlaces pendiente de certificar.'),
('CU15','Preparar y registrar el acta','Supervisor asignado','Inspección válida con lista de verificación aplicable.','Completar puntos y hallazgos; revisar resumen derivado; añadir notas generales opcionales; preparar el acta.','Un incumplimiento no permite aprobación global; no se exige repetir el mismo hallazgo en dos campos.','Contenido coherente vinculado a la versión de la lista de inspección.'),
('CU16','Descargar formulario y adjuntar acta firmada','Supervisor','Acta preparada y etapa adecuada.','Descargar plantilla o resumen; completar firma; adjuntar el PDF firmado y presentar.','No se presenta antes del inicio de la visita. Se validan archivo y campos; la institución aporta el formulario definitivo.','Acta disponible para revisión con evidencia privada.'),
('CU17','Determinar resultado de inspección','Supervisor','Acta y lista completas.','Evaluar hallazgos; seleccionar aprobado, observado o rechazado; presentar resultado consistente.','Un punto incumplido impide aprobado. Las observaciones requieren fundamento y conducen a su circuito de corrección.','Resultado sustentado en el contenido del acta.'),
('CU18','Registrar citación','Supervisor autorizado','Expediente e inspección accesibles.','Completar número, emisión, motivo y vencimiento; agregar evidencia y anticipación del aviso.','Se controlan fechas y evidencia PDF, JPG o PNG; la descarga exige autorización.','Citación trazable con aviso según configuración.'),
('CU19','Validar el acta','Coordinador','Acta presentada para revisión.','Consultar contenido y PDF; aceptar resultado o devolver con motivo.','Rechazar el acta permite reprogramación y conserva decisiones anteriores; no equivale automáticamente a resolución legal.','Acta validada para continuar o nueva inspección justificada.'),
('CU20','Otorgar vigencia anual','Abogado mediante resolución','Documentación revisada y acta conforme validada.','Aprobar legalmente; fijar vigencia anual según apertura o renovación.','Una conformidad documental o de campo aislada no habilita. Renovación vigente extiende desde su vencimiento activo.','Vigencia registrada como parte de la habilitación legal.'),
('CU21','Recibir y consultar notificaciones','Perfiles implicados','Evento de negocio o alerta aplicable.','Registrar decisión o programación; generar aviso; consultar bandeja e historial.','Las alertas deben ser idempotentes. La bandeja interna no implica envío de correo o SMS.','Destinatario informado sin duplicar alertas equivalentes.'),
('CU22','Administrar usuarios','Administrador','Capacidad administrativa activa.','Crear o editar usuario; asignar perfil; activar o suspender según reglas.','Versión antigua se rechaza. Cambios relevantes revocan sesiones; se protege la asignación de supervisores con visitas pendientes.','Cuenta actualizada mediante operación autorizada y trazable.'),
('CU23','Configurar capacidades de roles','Administrador','Rol existente entre los seis perfiles definidos.','Consultar capacidades; habilitar o suspender las permitidas; guardar cambios.','No se concede acceso por ocultar controles ni por registrar un manejador; el servidor verifica capacidades.','Permisos efectivos acordes con las atribuciones del perfil.'),
('CU24','Administrar requisitos y secciones','Administrador','Capacidad de configuración.','Crear secciones y requisitos; editar texto, obligatoriedad, actividad y plantilla; retirar mediante baja lógica.','Las modificaciones no alteran copias congeladas de expedientes enviados ni su evidencia histórica.','Catálogo publicado vigente y versiones anteriores conservadas.'),
('CU25','Consultar métricas de gestión','Gerente','Registros disponibles y filtros válidos.','Seleccionar ámbito municipal y periodo; consultar recibidos, pendientes, habilitaciones, rechazos, tiempos y rendimiento.','Borradores se muestran separados; conjuntos vacíos no fabrican tasas. Promedios agregados ponderan el universo pertinente.','Indicadores calculados sobre los datos almacenados.'),
('CU26','Exportar informe por fechas','Gerente','Consulta filtrada válida.','Establecer fechas; consultar panel; descargar CSV del mismo conjunto.','Rangos inválidos se rechazan; se neutralizan valores interpretables como fórmulas. Ante fallo de actualización se conserva el último resultado.','Pantalla y exportación coherentes con sus filtros.'),
('CU27','Consultar mis establecimientos','Propietario','Establecimiento propio.','Abrir ficha; acceder a expedientes y documentos; actualizar servicios permitidos.','No se permite sustituir datos oficiales fuera del circuito. Cambios simultáneos se protegen mediante versión.','Vista y edición limitada del establecimiento del usuario.'),
('CU28','Procesar alertas de licencia','Proceso periódico y propietario','Vigencia registrada y configuración de alerta.','Evaluar proximidad o vencimiento; generar aviso; reflejar cierre por vencimiento.','Ejecuciones repetidas no duplican avisos. Las pruebas invocan el proceso sin esperar una hora real.','Estado y alertas consistentes con la vigencia.'),
('CU29','Consultar informe histórico de cierre','Perfiles autorizados','Trámite concluido con PDF generado.','Abrir expediente cerrado; descargar informe que conserva datos de ese cierre.','Una renovación posterior no modifica el PDF anterior; se impide acceso ajeno.','Evidencia histórica estable de la resolución.'),
('CU30','Reprogramar o reasignar inspección','Coordinador','Visita que admite nueva asignación.','Indicar motivo; elegir el mismo u otro supervisor; validar nuevo intervalo; confirmar.','La reserva reemplazada no entra en conflicto consigo misma; otras visitas sí se respetan.','Nueva programación notificada con historia conservada.'),
('CU31','Gestionar subsanación y plazos','Propietario y proceso de alertas','Observación subsanable con plazo aplicable.','Calcular días hábiles y feriados; corregir lo observado; avisar proximidad o vencimiento.','El vencimiento no genera sanción o rechazo automático. La vuelta de etapa depende del origen de la observación.','Plazo trazable y retorno coherente a revisión.'),
('CU32','Solicitar renovación','Propietario','Laboratorio propio y condiciones de renovación válidas.','Iniciar renovación; completar documentos; recorrer revisión, inspección y legal; cerrar nuevo periodo.','Se bloquean trámites activos duplicados y se respeta la ventana anticipada configurada.','Mismo establecimiento con nueva vigencia y cierres independientes.'),
('CU-P1','Consultar antecedentes legales','Abogado','Expediente en revisión legal y acceso autorizado.','Revisar licencia, plano, documentación y acta; contrastar antecedentes oficiales.','La presentación del archivo no reemplaza la validación de sus datos ni acredita una firma real.','Antecedentes accesibles para fundamentar resolución.'),
('CU-P2','Emitir aprobación legal firmada','Abogado','Documentación completa revisada y acta conforme validada.','Cargar PDF firmado de aprobación; confirmar decisión; notificar al coordinador.','Sin PDF o con antecedentes pendientes no se aprueba; se conserva control de versión.','Aprobación legal registrada con su evidencia.'),
('CU-P3','Notificar decisión al propietario','Proceso de resolución','Decisión legal confirmada.','Registrar movimiento; generar aviso al propietario; mostrar resultado en bandeja e historial.','La notificación interna no garantiza un mensaje externo; se respeta el acceso al expediente.','Propietario informado de la resolución y su continuidad.'),
('CU-P4','Cerrar y habilitar o resolver rechazo','Abogado y sistema','Resolución legal válida.','Guardar resolución; habilitar y fijar vigencia si procede; generar cierre histórico.','El rechazo legal definitivo no se reactiva por asignación. La subsanación legal vuelve a legal sin repetir campo salvo indicación expresa.','Laboratorio y expediente coherentes con la decisión definitiva.')]
for k in range(0,36,4):
 subset=cases[k:k+4]
 page(f'{6+k//4} Casos de uso {k+1} a {k+4}')
 for cid,title,actor,pre,steps,alt,end in subset:
  h(cid.replace('-',' ')+' '+title)
  b('Actor y precondición',actor+'. '+pre)
  b('Flujo principal',steps)
  b('Alternativas y controles',alt)
  b('Resultado',end)
 p('Trazabilidad funcional: catálogo de casos de uso y contrato de datos. [S1, S2]','Caption')

page('15 Estrategia de aseguramiento de calidad')
p('SQA significa aseguramiento de calidad del software. En SI_Lab combina revisión de requisitos y flujos, inspección de código, pruebas automatizadas, solicitudes HTTP reales, navegación de usuario y ensayos de recuperación. Cada modalidad responde una pregunta distinta: la integración valida reglas y persistencia; la navegación comprueba el uso visible de la interfaz. [S4, S6]')
table(['Nivel','Cobertura y finalidad'],[
 ('Revisión funcional','Contrastar casos de uso, contratos, estados y restricciones de cada rol.'),
 ('Servidor e integración','Formularios, transiciones, permisos, aislamiento, archivos, fechas y concurrencia.'),
 ('JavaScript','Confirmaciones, fechas, geolocalización simulada y actualización JSON.'),
 ('HTTP y navegador','Recorridos con perfiles, enlaces, respuestas y evidencia visual de correcciones.'),
 ('Operación local','Red interrumpida, proceso detenido, disco no disponible y SQLite ocupado.'),
 ('Restauración','Recuperar código, base y archivos; reconstruir e iniciar el sistema recuperado.')])
h('Preparación y trazabilidad')
p('El archivo inicial tenía 36 casos. La revisión amplió el catálogo a 183, con pasos, resultado esperado, estado, método y referencia de evidencia. Los resultados se consolidaron en Excel y JSON. El reporte de bugs siguió el formato del template proporcionado. La evidencia separa ejecución satisfactoria, defecto confirmado y bloqueo de un paso requerido. [S4]')
p('Las pruebas que modifican datos utilizan copias independientes. Un caso solo se cierra completamente cuando todos sus pasos requeridos tienen evidencia. Por eso una prueba simulada de permiso GPS denegado no sustituye la acción real de denegarlo, y una comprobación HTTP no acredita cada botón del navegador.')
h('Criterios de aceptación')
p('Se exige respetar permisos y etapas, conservar integridad y evidencia, impedir efectos duplicados, rechazar entradas inválidas y ofrecer recuperación ante fallos previstos. La aceptación institucional requiere además catálogos y formularios finales y completar las comprobaciones manuales identificadas en la página 23.')

page('16 Resultados de pruebas y cobertura')
table(['Conjunto y fecha','Resultado','Interpretación'],[
 ('Campaña funcional del 23 de octubre','183 casos: 172 exitosos, 2 fallidos y 9 bloqueados.','Registro histórico del Excel; no se reescribe tras correcciones.'),
 ('Servidor al 26 de octubre','389 ejecutadas y aprobadas; 0 fallos.','Regresión actual: 366 previas, 11 de contratos SOLID y 12 de bugs.'),
 ('JavaScript al 26 de octubre','28 aprobadas.','Lógica de cliente cubierta por sus pruebas automatizadas.'),
 ('Compilación Release','0 errores y 0 advertencias.','Validación de construcción del cierre de bugs.'),
 ('Restauración del 23 de octubre','360 pruebas aprobadas de la versión recuperada.','Valida el respaldo histórico; no es la suite actual de 389.')])
p('La campaña histórica tuvo un 94,0 % de casos exitosos sobre el total definido, un 1,1 % fallido y un 4,9 % bloqueado. Entre los 174 casos con resultado exitoso o fallido, la proporción exitosa fue 98,9 %. Estos porcentajes describen aquella campaña y no equivalen a un porcentaje de ausencia de errores del producto. [S4]')
h('Inventario de comprobaciones')
p('La auditoría previa documentó 69 acciones, consultas por perfil, enlaces, controles de seguridad en peticiones POST, transiciones prohibidas y variantes de campos inválidos. Son dimensiones de cobertura de la revisión; no se suman a los 183 casos ni a las 389 pruebas como si fueran casos independientes. [S9]')
h('Reejecución posterior a los bugs')
p('PUB-EXT-09 supera la reejecución dirigida de coordenadas, incluidos límites válidos y datos inválidos. Los defectos BUG-01 y BUG-02 de E2E-EXT-04 están corregidos, pero no se ha certificado todo el recorrido de controles y estados incluido en ese caso. No corresponde convertir automáticamente el Excel histórico en 174 exitosos y cero fallidos. [S5]')
p('El 100 % de aprobación de la suite automatizada indica que sus comprobaciones pasaron en esa ejecución. La cobertura física de GPS, compatibilidad real de dispositivos y aceptación institucional conservan el estado pendiente detallado en este informe.')

page('17 Bugs corregidos y evidencia')
h('BUG 01 Recuperación de formularios fallidos')
p('Problema: algunas operaciones devolvían errores 400 o 409 como una respuesta aislada, sin una salida clara para volver al expediente. Corrección: las navegaciones HTML de gestión, administración y registro muestran el motivo y un enlace GET a una pantalla recuperable, manteniendo el menú y el código de error.')
p('Verificación: una observación documental sin motivo devuelve validación con retorno. Un borrador enviado desde dos pestañas acepta el primer envío, rechaza el segundo por versión antigua y abre después el expediente vigente con un único envío en el historial. APIs y formatos de métricas conservan su comportamiento. [S5]')
h('BUG 02 Aprobación documental repetida')
p('Problema: aprobar otra vez una versión ya aprobada producía efectos repetidos sobre el historial y los datos de revisión. Corrección: el botón de aprobar desaparece cuando ya corresponde ese estado y el servidor trata la repetición con versión vigente como una operación sin cambios.')
p('Verificación: se compara la persistencia completa antes y después y no cambian fecha, revisor, observación, versión, avisos ni historial. Una versión antigua sigue recibiendo conflicto y un documento aprobado puede observarse de forma válida. [S5]')
h('BUG 03 Coordenadas públicas fuera de rango')
p('Problema: datos persistidos como 999,999 podían llegar al mapa y a enlaces de ruta. Corrección: una validación común controla latitud y longitud; las fichas inválidas permanecen consultables con aviso, sin marcador ni ruta a una posición imposible.')
p('Verificación: se ensayan latitudes ±91, longitudes ±181 y 999,999; también límites válidos ±90 y ±180 y el punto 0,0. Se comprueban listado, ficha y ruta, incluso por URL directa. El navegador utilizó un registro dañado únicamente en una copia local. [S5]')
h('Estado de cierre')
p('Los tres bugs están corregidos y verificados al 26 de octubre. La batería actual y la navegación dirigida respaldan ese cierre. Se conserva el reporte histórico y se enlaza la evidencia posterior, sin borrar los fallos originales. La base original y sus 54 archivos privados quedaron preservados en la verificación de ese día.')

page('18 Operación respaldo y restauración')
table(['Ensayo local del 23 de octubre','Resultado observado'],[
 ('60 consultas con 12 en paralelo','60 respuestas correctas; mediana 0,016 s, percentil 95 de 0,094 s y máximo 0,125 s.'),
 ('16 escrituras con 8 en paralelo','Una confirmación y 15 conflictos por versión antigua; sin sobrescritura.'),
 ('Corte de cliente durante carga','Base y archivos anteriores sin cambios.'),
 ('Terminación durante carga incompleta','Integridad correcta; reinicio y sesión recuperados.'),
 ('Almacenamiento no disponible','Respuesta 503 y conservación de los datos anteriores.'),
 ('Caída tras copiar archivo y antes de confirmar','Archivo privado recuperable; cero archivos sin registro en la raíz.'),
 ('SQLite bloqueado al guardar','Respuesta 409; expediente y archivos anteriores conservados.')])
p('Las cifras de tiempo describen ese equipo, esos datos y ese nivel de concurrencia. No establecen una capacidad de producción ni una garantía de disponibilidad. [S6]')
h('Restauración completa ejecutada')
p('Se extrajo el respaldo, se verificaron 351 archivos de fuente y los 47 documentos privados presentes en aquel punto, y se comprobó integridad SQLite sin referencias rotas. Se reconstruyeron cliente y servidor, se arrancó el proyecto recuperado y se verificaron los seis accesos, una descarga privada y el rechazo de una descarga ajena. Sus 360 pruebas aprobaron. [S6]')
h('Puntos de recuperación posteriores')
p('Antes de SOLID se creó el punto 20261026-135109-antes-solid. Antes de corregir los tres bugs se creó 20261026-144304-antes-bugs-pendientes. Incluyen código, copia consistente de SQLite, documentos privados, hashes e instrucciones. Los 54 archivos preservados el 26 de octubre corresponden a un corte posterior a los 47 del respaldo anterior. [S3, S5]')
p('El respaldo operativo debe conservar conjuntamente base, almacenamiento privado y carpeta de recuperación si existe. El archivo de exclusión de una instancia no es dato del negocio. Los archivos recuperados no se reinsertan como aprobaciones sin revisar su procedencia.')

page('19 SOLID en la aplicación')
p('SOLID reúne cinco principios para reducir el acoplamiento y delimitar responsabilidades. Su aplicación se demuestra mediante contratos, clases y pruebas concretas. En SI_Lab la refactorización del 26 de octubre mantiene MVC, EF Core, SQLite y las vistas existentes. [S3, S7]')
h('S Responsabilidad única')
p('GestionController.Decidir enlaza y valida la petición y presenta el resultado. DecisionesTramite coordina transacción e historial. Siete familias de manejadores separan envío, revisión documental, asignación, programación, registro de acta, validación del acta y resolución legal. Las métricas se calculan en ConsultaMetricas y el CSV se genera en ExportadorMetricasCsv.')
p('Una modificación de la regla de agenda corresponde al servicio de reservas o a su acción; una modificación del formato CSV corresponde al exportador. La separación reduce el riesgo de alterar funciones ajenas y permite probarlas por su contrato.')
h('O Abierto a extensión y cerrado a modificación')
p('Cada manejador declara sus acciones y su permiso mediante IAccionTramite. RegistroAccionesTramite las resuelve por clave, comprueba duplicados y rechaza declaraciones vacías. Una acción nueva se implementa y registra en el punto de composición; el controlador y el coordinador no añaden otro selector central.')
code('public interface IAccionTramite\n{\n    IReadOnlyCollection<string> Acciones { get; }\n    string Permiso { get; }\n    Task<string> Ejecutar(ContextoDecision contexto);\n}')
p('Fragmento de Application/Tramites/ContratosDecision.cs. El filtro de permisos consulta la misma declaración de la acción. Una capacidad institucional nueva requiere su definición y administración explícitas: registrar una clase no concede privilegios. [S7]','Caption')
h('Cómo se comprueba')
p('SolidContractsTests valida la extensión mediante un manejador sustituible, las claves duplicadas o desconocidas y las acciones incompletas. La separación tiene respaldo de comportamiento y no depende solamente de la existencia de interfaces. [S10]')

page('20 Sustitución interfaces y dependencias')
h('L Sustitución de Liskov')
p('Los consumidores utilizan contratos sustituibles. Un manejador valida actor y etapa antes de mutar el expediente, devuelve el texto auditable y comunica un incumplimiento de negocio sin confirmar la transacción por su cuenta. Si crea un PDF, informa su clave para permitir limpieza ante error.')
p('Las pruebas reemplazan manejador, verificador de contraseñas y reloj. Comprueban éxito, rechazo, actualización del hash, confirmación única y reversión de datos y archivos. Así se verifica que una sustitución respete las condiciones que espera el consumidor. [S3, S10]')
h('I Segregación de interfaces')
p('Los consumidores reciben contratos ajustados a su necesidad. ILectorArchivos ofrece resolver una descarga; IAlmacenPdf agrega guardar y borrar. La agenda recibe inspecciones; el calendario recibe feriados; las consultas públicas y de métricas no reciben confirmación ni transacciones.')
code('public interface ILectorArchivos\n{\n    string? Resolve(string key);\n}\n\npublic interface IAlmacenPdf : ILectorArchivos\n{\n    string Root { get; }\n    Task<string> SaveAsync(IFormFile file,\n        CancellationToken token = default);\n    void Delete(string key);\n}')
h('D Inversión de dependencias')
p('Los servicios reciben interfaces por constructor. RegistroServicios relaciona contratos con implementaciones y hace que los puertos de datos compartan ApplicationDbContext durante una petición. IReloj permite evaluar tiempo sin fijarlo al reloj real y IPasswordHasher<Usuario> permite sustituir la comprobación de contraseñas.')
code('services.AddScoped<IDecisionesTramite, DecisionesTramite>();\nservices.AddScoped<IConsultaMetricas, ConsultaMetricas>();\nservices.AddScoped<IExportadorMetricas, ExportadorMetricasCsv>();\nservices.AddSingleton<IReloj, RelojSistema>();')
p('Fragmentos de Application/Abstractions/Archivos.cs e Infrastructure/RegistroServicios.cs. [S7]','Caption')
p('La evidencia respalda la aplicación de los cinco principios en las áreas refactorizadas. SOLID no tiene un porcentaje universal de certificación. Los contratos de datos todavía conocen EF y las utilidades puras no necesitan una interfaz por cada función.')

page('21 Patrones de diseño utilizados')
table(['Patrón o enfoque','Implementación y utilidad'],[
 ('MVC','Controladores, modelos y vistas Razor separan peticiones, datos de presentación e interfaz. Es un patrón arquitectónico.'),
 ('Inyección de dependencias','Program y RegistroServicios construyen servicios a partir de contratos; centralizan elección de implementaciones y duración.'),
 ('Manejadores de comandos y estrategia','IAccionTramite encapsula familias de decisiones; el registro elige una por clave y DecisionesTramite coordina su ejecución.'),
 ('Capa de servicios','Decisiones, agenda, métricas, calendario y avisos concentran responsabilidades fuera de las vistas y controladores.'),
 ('Unidad de trabajo','El contexto EF compartido y el contrato de transacción agrupan cambios y confirmación de una operación.'),
 ('Adaptador','TransaccionDatos envuelve la transacción EF bajo ITransaccionDatos; InformeCierre expone la generación del informe mediante un contrato.'),
 ('Fachada de compatibilidad','TramiteService delega en servicios especializados para consumidores anteriores; no duplica las reglas del flujo de producción.'),
 ('Cadena de procesamiento','Middleware y filtros MVC aplican controles sucesivos y continúan mediante el siguiente componente.'),
 ('Proceso en segundo plano','VigenciaWorker ejecuta alertas periódicas usando servicios con alcance propio.')])
h('Ejemplo integrado de una decisión')
p('Al programar una inspección, el controlador recibe la petición; los filtros comprueban acceso y capacidad; DecisionesTramite busca el expediente visible y verifica su versión. El registro resuelve ProgramarInspeccion, que comprueba rol, etapa e intervalo mediante sus dependencias. El coordinador registra historial, guarda y confirma. Si una regla falla, devuelve un resultado controlado y revierte lo necesario. [S7]')
h('Precisión de la clasificación')
p('La tabla distingue patrones de arquitectura, composición y comportamiento. El registro de manejadores se aproxima a Strategy y Command, sin implementar deshacer o rehacer. EF cumple la función de unidad de trabajo; no hay una capa Repository personalizada independiente del ORM. Los estados del flujo no constituyen el patrón GoF State con una clase por estado. Tampoco se acredita un bus de eventos, microservicios ni un CQRS completo.')

page('22 Pendientes para aceptación completa')
table(['Caso pendiente','Comprobación necesaria'],[
 ('PUB-HL-02','Aceptar ubicación física real y confirmar apertura de la navegación.'),
 ('PUB-EXT-04','Denegar el permiso real del navegador o sistema; la denegación simulada ya está cubierta.'),
 ('REG-HL-03','Confirmar formulario en viewport móvil efectivo; la solicitud de 390 × 844 no cambió el navegador de prueba.'),
 ('E2E-EXT-06','Ejecutar recorridos en otros navegadores, tamaños y dispositivos reales.'),
 ('E2E-EXT-07','Usar formularios y catálogos institucionales definitivos; completar propietario y vigencia de tres laboratorios.'),
 ('E2E-EXT-05','Cerrar teclado, zoom y contraste en todos los perfiles y pantallas.'),
 ('PUB-EXT-08','Bloquear los recursos externos del mapa en navegador y completar directorio y ficha sin bloqueo del flujo.'),
 ('SUP-EXT-08','Completar en navegador el orden y todos los enlaces de las rutas de visitas del día.'),
 ('REG-EXT-05','Abrir selector nativo y cancelar realmente; agregar y retirar archivos ya tiene evidencia parcial.')])
p('El JavaScript público ejecutado sin Leaflet presenta el mensaje de mapa no disponible. Falta reproducir el bloqueo del proveedor dentro del navegador y recorrer las páginas para cerrar PUB-EXT-08. La implementación del fallback y su prueba parcial no sustituyen ese paso. [S4, S5]')
h('Recorrido global de controles')
p('E2E-EXT-04 requiere completar todos los controles y estados definidos por su recorrido. Los dos defectos vinculados a ese caso están cerrados, pero el cierre dirigido no certifica el resto de su alcance. Esta condición es adicional a la lista histórica de nueve bloqueados.')
h('Orden recomendado de cierre')
p('Primero, completar los recorridos manuales reproducibles y registrar evidencia por caso. Después, validar catálogos, plantillas y datos con la institución y repetir el circuito de apertura y renovación. Finalmente, ejecutar la matriz de compatibilidad y accesibilidad sobre pantallas reales. Producción permanece excluida de esta evaluación.')

page('23 Fuentes y trazabilidad documental')
p('Las referencias identifican documentos y evidencias del repositorio SI_Lab. Las rutas se expresan desde la raíz del proyecto. La fecha de corte de este informe es el 26 de octubre de 2026.')
sources=[
 ('S1','Contexto y contrato de información','README.md; CONTEXTO_PROYECTO.md; CONTRATO_DATOS.md.'),
 ('S2','Catálogo funcional','docs/CASOS_DE_USO.md. Define CU01 a CU32 y CU-P1 a CU-P4.'),
 ('S3','Aplicación de SOLID','docs/ARQUITECTURA_SOLID.md; outputs/solid-20261026/verificacion.json.'),
 ('S4','Campaña funcional histórica','outputs/test-cases-20261023/TEST_CASES_PROYECTO_EJECUTADOS.xlsx; ejecucion-consolidada.json; REPORTE_DE_BUGS_PROYECTO.xlsx, en esa misma carpeta.'),
 ('S5','Cierre y regresión de bugs','docs/CIERRE_BUGS_2026-10-26.md; outputs/bugs-20261026/verificacion.json; pruebas/bugs-corregidos.trx y evidencias BUG-01, BUG-02 y BUG-03 de esa carpeta.'),
 ('S6','Operación y restauración','docs/CIERRE_PENDIENTES_LOCALES_2026-10-23.md; docs/cierre-pendientes-20261023/operaciones-finales.json; restauracion-operaciones.json y restauracion-360.trx.'),
 ('S7','Contratos y servicios del código','Application/Abstractions; Application/Tramites; Application/Metricas; Infrastructure/RegistroServicios.cs; Data/ApplicationDbContext.Puertos.cs.'),
 ('S8','Dependencias declaradas','SEDESLABORATORIO.csproj; ClientApp/package.json; recursos del mapa en las vistas públicas.'),
 ('S9','Auditoría ampliada','docs/AUDITORIA_EXHAUSTIVA_2026-10-22.md y sus evidencias asociadas.'),
 ('S10','Pruebas de contratos y bugs','tests/SiLab.Tests/SolidContractsTests.cs; PendingBugRegressionTests.cs; outputs/solid-20261026/pruebas/solid-final.trx.')]
for sid,title,path in sources:
 b(sid+' '+title,path)
h('Antecedentes de requisitos')
p('La reunión de JUAN y STEVEN del 15 de septiembre de 2026, su transcripción y la referencia FigmaSedes forman parte de los antecedentes funcionales y visuales del proyecto. El catálogo de casos de uso recoge la definición recuperada de ese contexto. La aceptación institucional de formularios, políticas y datos definitivos conserva su validación propia.')
p('Conservar esta edición junto a sus evidencias permite revisar cambios futuros sin perder los resultados históricos de SQA ni atribuirles el alcance de una ejecución posterior.')

OUT.mkdir(parents=True,exist_ok=True)
path=OUT/'Informe_integral_SI_Lab.docx'
doc.save(path)
print(path)
print('Casos de uso:',len(cases))
