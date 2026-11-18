"""Escenarios adicionales del Excel contra una copia local independiente.
Las escrituras SQL preparan fixtures sintéticos; las acciones bajo prueba son HTTP.
"""
import sys,json,sqlite3,re,html,os,hashlib,csv,io,datetime as dt,concurrent.futures,traceback,uuid
from pathlib import Path
from types import SimpleNamespace
base=Path(__file__).resolve().parent; project=base.parents[1]
sys.path.insert(0,str(project/'tests'))
from operational_audit import Audit,Session,tables,files
auditRoot=Path(r'C:/Users/Usuario/.codex/visualizations/2026/10/18/01a15111-6728-7203-8671-331a288cb707/campana-183-suplementos-20261023')
os.environ['Tramites__RenovacionDiasAnticipacion']='30'
audit=Audit(SimpleNamespace(content_root=str(project),audit_root=str(auditRoot),seed_db=str(project/'App_Data/checkpoints/20261022-234400-antes-cierre-pendientes/si_lab.db'),seed_files=str(project/'App_Data/documentos'),port=5283))
results=[];checks=[];clients={};num=0
PDF=(base/'archivos-sinteticos/archivo-1.pdf').read_bytes()
NOW=dt.datetime.utcnow(); CIVIL=NOW-dt.timedelta(hours=4)
def sql(query,args=(),one=False):
 with sqlite3.connect(str(audit.db)) as d:
  d.row_factory=sqlite3.Row;r=d.execute(query,args)
  if query.lstrip().lower().startswith(('select','pragma')):
   rows=[dict(x) for x in r.fetchall()];return (rows[0] if rows else None) if one else rows
  return r.lastrowid
def check(condition,detail):
 checks.append({'assertion':detail,'passed':bool(condition)})
 assert condition,detail
def logged(email,password='EnsayoLocal123!'):
 c=Session(audit.origin);st,b,_,_=c.request('/auth/Auth/Login');token=html.unescape(re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"',b.decode()).group(1))
 st,b,h,_=c.request('/auth/Auth/Login',{'Email':email,'Password':password,'__RequestVerificationToken':token})
 if st==302:
  _,b,_,_=c.request('/auth/Auth/Dashboard');m=re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"',b.decode());c.token=html.unescape(m.group(1)) if m else ''
 return c,st,h
def client(role):
 if role not in clients:clients[role]=Session(audit.origin).login(role)
 return clients[role]
def post(role,path,fields,uploads=None):
 c=client(role) if isinstance(role,str) else role
 if not uploads:return c.post(path,fields)
 boundary='Campaign183Multipart'
 body=b''
 for k,v in dict(fields,__RequestVerificationToken=c.token).items():body+=f'--{boundary}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'.encode()
 for field,name,data in uploads:body+=f'--{boundary}\r\nContent-Disposition: form-data; name="{field}"; filename="{name}"\r\nContent-Type: application/octet-stream\r\n\r\n'.encode()+data+b'\r\n'
 body+=f'--{boundary}--\r\n'.encode()
 return c.request(path,body=body,headers={'Content-Type':'multipart/form-data; boundary='+boundary})
def create_user(role='Propietario',active=True):
 global num;num+=1;email=f'campana-{uuid.uuid4().hex}@example.test'
 st,_,_,_=post('administrador','/Administracion/Usuario',{'Nombre':f'Cuenta local {role} {num}','Email':email,'Password':'EnsayoLocal123!','Rol':role,'Activo':str(active).lower()})
 check(st==302,'Alta de fixture usuario '+email)
 return sql('select * from Usuarios where Email=?',(email,),True)
def edit_user(user,**changes):
 fields={k:user[k] for k in ['Id','Version','Nombre','Email','Rol']};fields['Activo']=str(bool(user['Activo'])).lower();fields.update(changes)
 return post('administrador','/Administracion/Usuario',fields)
def clone(table,id,**changes):
 row=sql(f'select * from {table} where Id=?',(id,),True);row.pop('Id');row.update(changes)
 columns=list(row);return sql('insert into '+table+' ('+','.join('"'+k+'"' for k in columns)+') values ('+','.join('?' for _ in columns)+')',[row[k] for k in columns])
def case(state='revision_legal',municipio=None,**overrides):
 source=sql("select Id from Solicitudes where Codigo='DEMO-LEGAL'",one=True)['Id']
 fields=dict(Estado=state,FechaCreacion=NOW.isoformat(' '),PrimerEnvio=NOW.isoformat(' '),FechaEnvio=NOW.isoformat(' '),FechaCierre=None,Version=1,Codigo='QA-'+str(num)+'-'+str(len(results))+'-'+str(len(checks)))
 if municipio:fields['Municipio']=municipio
 fields.update(overrides);sid=clone('Solicitudes',source,**fields)
 for doc in sql('select Id from Documentos where SolicitudId=? and Actual=1',(source,)):clone('Documentos',doc['Id'],SolicitudId=sid)
 return sid
def opening(**overrides):
 fields={'NombreLaboratorio':'Laboratorio local campaña','TipoLaboratorio':'Clínico','Municipio':'Cochabamba','Latitud':'-17.39','Longitud':'-66.16'};fields.update(overrides)
 return fields
def new_open(fields,uploads=None):return post('propietario','/solicitud-registro/SolicitudRegistro/Nueva',fields,uploads)
def auth_invalid():
 u=create_user(active=False)
 for email,pwd in [(u['Email'],'EnsayoLocal123!'),('inexistente-campana@example.test','EnsayoLocal123!')]:
  c,status,_=logged(email,pwd);check(status==200,'Login inválido no redirige al panel');check(c.request('/Gestion')[0]==302,'La identidad no se autentica')
def external_return():
 c=Session(audit.origin);_,b,_,_=c.request('/auth/Auth/Login?returnUrl=https://example.invalid');token=html.unescape(re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"',b.decode()).group(1))
 st,_,h,_=c.request('/auth/Auth/Login',{'Email':'propietario@si-lab.local','Password':'Demo123!','returnUrl':'https://example.invalid','__RequestVerificationToken':token})
 check(st==302 and h.get('Location','').startswith('/') and not h.get('Location','').startswith('//'),'Redirección únicamente local: '+h.get('Location',''))
def all_roles():
 destinations={'Propietario':'Nueva solicitud','Coordinador':'Abrir bandeja de trámites','Supervisor':'Abrir mi agenda','Abogado':'Abrir revisión legal','Gerente':'Ver métricas e informes','Administrador':'Usuarios'}
 for role,target in destinations.items():
  u=create_user(role);c,st,_=logged(u['Email']);check(st==302,'Acceso de cuenta nueva '+role)
  status,b,_,_=c.request('/auth/Auth/Dashboard');text=html.unescape(b.decode())
  check(status==200 and u['Nombre'] in text,'Identidad nueva correcta '+role)
  check(u['Rol']==role.lower() and target.lower() in text.lower(),'Rol guardado y acceso principal correcto '+role)
  check(u['Password']!='EnsayoLocal123!' and len(u['Password'])>30,'Contraseña cifrada mediante hash '+role)
def credential_change():
 u=create_user();old,st,_=logged(u['Email']);check(st==302,'Sesión previa de la cuenta')
 st,_,_,_=edit_user(u,Password='OtraClaveLocal123!');check(st==302,'Cambio de credencial guardado')
 check(old.request('/Gestion')[0]==302,'Sesión anterior revocada')
 check(logged(u['Email'])[1]!=302,'Contraseña anterior rechazada');check(logged(u['Email'],'OtraClaveLocal123!')[1]==302,'Contraseña nueva aceptada')
def edit_blank_password():
 u=create_user();old,_,_=logged(u['Email']);st,_,_,_=edit_user(u,Nombre='Nombre editado local',Password='')
 check(st==302,'Edición sin contraseña guardada');check(old.request('/Gestion')[0]==302,'Sesión revocada');check(logged(u['Email'])[1]==302,'Contraseña anterior se conserva')
def suspend_reactivate():
 u=create_user();c,_,_=logged(u['Email']);st,_,_,_=edit_user(u,Activo='false');check(st==302,'Suspensión guardada');check(c.request('/Gestion')[0]==302,'Sesión suspendida revocada');check(logged(u['Email'])[1]!=302,'Suspendido no inicia sesión')
 u=sql('select * from Usuarios where Id=?',(u['Id'],),True);check(edit_user(u,Activo='true')[0]==302,'Reactivación guardada');check(logged(u['Email'])[1]==302,'Reactivado inicia sesión')
def admin_protections():
 admin=sql("select * from Usuarios where Email='administrador@si-lab.local'",one=True)
 for change in [{'Activo':'false'},{'Rol':'Propietario'}]:
  before=tables(audit.db);check(edit_user(admin,**change)[0]==200,'Autobloqueo rechazado');check(tables(audit.db)==before,'El administrador conserva su acceso')
 u=create_user();old,_,_=logged(u['Email']);check(edit_user(u,Rol='Gerente')[0]==302,'Cambio de rol guardado');check(old.request('/Gestion')[0]==302,'Cookie anterior revocada');c,st,_=logged(u['Email']);check(st==302 and c.request('/Metricas')[0]==200,'Nueva sesión de gerente autorizada');check('AccessDenied' in c.request('/Gestion')[2].get('Location',''),'Nuevo rol sin atribuciones anteriores')
def supervisor_protected():
 u=create_user('Supervisor');sid=case('inspeccion')
 original=sql("select Id from Inspecciones where SolicitudId=(select Id from Solicitudes where Codigo='DEMO-LEGAL')",one=True)['Id']
 clone('Inspecciones',original,SolicitudId=sid,SupervisorId=u['Id'],Estado='pendiente',Resultado='',Programada=0,Fecha=CIVIL.isoformat(' '),FechaFin=None)
 for change in [{'Activo':'false'},{'Rol':'Abogado'}]:check(edit_user(u,**change)[0]==200,'Se bloquea dejar una visita pendiente sin supervisor')
 sup=sql("select Id from Usuarios where Email='supervisor@si-lab.local'",one=True)['Id']
 s=sql('select * from Solicitudes where Id=?',(sid,),True)
 check(post('coordinador','/Gestion/Decidir',{'Id':sid,'Version':s['Version'],'Accion':'asignar','SupervisorId':sup,'Motivo':'Reasignación local antes de suspender'})[0]==302,'Reasignación legítima de la visita')
 u=sql('select * from Usuarios where Id=?',(u['Id'],),True);check(edit_user(u,Activo='false')[0]==302,'Sin visita pendiente se permite suspender')
def coordinate_bounds():
 for field,values in [('Latitud',['-90','90']),('Longitud',['-180','180'])]:
  for value in values:check(new_open(opening(**{field:value}))[0]==302,'Límite inclusivo aceptado '+field+'='+value)
 for field,values in [('Latitud',['-90.000001','90.000001','','abc']),('Longitud',['-180.000001','180.000001','','abc'])]:
  for value in values:
   before=tables(audit.db);check(new_open(opening(**{field:value}))[0]==200,'Valor de coordenada inválido rechazado');check(before==tables(audit.db),'Sin persistencia de coordenada inválida')
def text_limits():
 for field,maxval in [('NombreLaboratorio',200),('Direccion',500),('Codigo',100),('Telefono',50)]:
  # Teléfono válido de 50 dígitos, sin inventar formato institucional adicional.
  value=('1' if field=='Telefono' else 'a')*maxval
  check(new_open(opening(**{field:value}))[0]==302,'Máximo textual válido '+field)
  before=tables(audit.db);check(new_open(opening(**{field:value+'a'}))[0]==200,'Exceso de longitud rechazado '+field);check(before==tables(audit.db),'Sin registro parcial '+field)
def pad_pdf(size):
 prefix=PDF[:PDF.rfind(b'%%EOF')];suffix=b'\n%%EOF\n';padding=size-len(prefix)-len(suffix)-2
 return prefix+b'\n%'+b'X'*padding+suffix
def upload_limits():
 ten=pad_pdf(10*1024*1024);check(len(ten)==10*1024*1024,'PDF de tamaño exacto 10 MB')
 check(new_open(opening(),[('DocumentosPdf','limite.pdf',ten)])[0]==302,'PDF válido de 10 MB aceptado')
 before=tables(audit.db);existing=files(audit.storage)
 check(new_open(opening(),[('DocumentosPdf','exceso.pdf',pad_pdf(len(ten)+1))])[0]==200,'PDF de 10 MB + 1 byte rechazado');check(before==tables(audit.db) and existing==files(audit.storage),'Exceso individual sin persistencia parcial')
 check(new_open(opening(),[('DocumentosPdf',f'limite-{i}.pdf',ten) for i in range(5)])[0]==302,'Carga inicial de 50 MB aceptada')
 before=tables(audit.db);existing=files(audit.storage)
 check(new_open(opening(),[('DocumentosPdf',f'exceso-{i}.pdf',ten) for i in range(5)]+[('DocumentosPdf','extra.pdf',PDF)])[0]==200,'Total superior a 50 MB rechazado');check(before==tables(audit.db) and existing==files(audit.storage),'Exceso total sin persistencia parcial')
def services_boundaries():
 lab=sql("select * from Laboratorios where Codigo='DEMO-LAB-HABILITADO'",one=True)
 for text in ['', 'S'*2000,'S'*2001]:
  current=sql('select * from Laboratorios where Id=?',(lab['Id'],),True)
  version=hashlib.sha256(current['Servicios'].encode()).hexdigest().upper()
  status=post('propietario','/Gestion/Servicios',{'id':lab['Id'],'version':version,'servicios':text})[0]
  saved=sql('select Servicios from Laboratorios where Id=?',(lab['Id'],),True)['Servicios']
  check(status==(302 if len(text)<=2000 else 400),'Límite de servicios '+str(len(text)));check(saved==(text if len(text)<=2000 else current['Servicios']),'Contenido de servicios conservado')
def renewal_window():
 original=sql("select Id from Laboratorios where Codigo='DEMO-LAB-HABILITADO'",one=True)['Id']
 for days,allowed in [(31,False),(30,True),(0,True),(-1,True)]:
  lid=clone('Laboratorios',original,Nombre='Licencia ventana '+str(days),Codigo='VENTANA-'+str(days),VigenciaHasta=(NOW+dt.timedelta(days=days)).isoformat(' '))
  status=post('propietario','/Gestion/Renovar',{'id':lid})[0]
  count=sql('select count(*) n from Solicitudes where LaboratorioId=?',(lid,),True)['n']
  check(status==302,'Respuesta controlada de renovación días '+str(days));check(count==(1 if allowed else 0),'Renovación solo en ventana permitida para '+str(days)+' días')
  if not allowed:
   page=html.unescape(client('propietario').request('/Gestion/Establecimientos')[1].decode());check('30 días previos' in page,'Mensaje explica la ventana configurada')
def metrics_exact():
 m='COHORTE-183-EXACTA'
 for i,state in enumerate(['habilitado','habilitado','rechazado','en_revision','inspeccion','rechazado','borrador','borrador']):
  case(state,m,PrimerEnvio=None if state=='borrador' else '2035-01-15 10:00:00',FechaCreacion='2035-01-14 10:00:00',MotivoRechazo='campo' if i==5 else 'legal' if state=='rechazado' else '')
 status,body,_,_=client('gerente').request('/Metricas/Datos?desde=2035-01-01&hasta=2035-01-31&municipio='+m);data=json.loads(body)
 for key,val in {'total':6,'habilitadas':2,'rechazadas':1,'pendientes':3,'borradores':2}.items():check(data[key]==val,f'{key}: {data[key]} = {val}')
 check(abs(data['tasaAprobacion']-200/3)<.0001,'Tasa 2/3 sin contar rechazo de campo como definitivo')
def metrics_average():
 m='COHORTE-183-PROMEDIO'
 for delta in [2,4,None,-1]:case('habilitado',m,PrimerEnvio='2035-02-01 10:00:00',FechaCierre=None if delta is None else (dt.datetime(2035,2,1,10)+dt.timedelta(days=delta)).isoformat(' '))
 data=json.loads(client('gerente').request('/Metricas/Datos?desde=2035-02-01&hasta=2035-02-28&municipio='+m)[1]);check(data['promedioDias']==3,'Promedio de cierres válidos 2 y 4 = 3 días');check(data['cierresConFecha']==2,'Se excluyen cierre ausente y negativo')
 empty=json.loads(client('gerente').request('/Metricas/Datos?municipio=SIN-COHORTE-183')[1]);check(empty['promedioDias'] is None and empty['tasaAprobacion'] is None,'Sin denominador se informa ausencia, no cero')
def csv_security():
 names=['=1+1','+cmd','-formula','@suma','  =oculto','Municipio, con coma','Texto "entrecomillado"','Texto\ncon salto']
 for name in names:case('en_revision',name,PrimerEnvio='2036-01-02 10:00:00')
 st,body,_,_=client('gerente').request('/Metricas?desde=2036-01-01&hasta=2036-01-31&descargar=true');check(st==200,'CSV descargado')
 parsed=list(csv.reader(io.StringIO(body.decode('utf-8-sig'))));saved=[r[0] for r in parsed if len(r)==11][1:]
 for text in names:check(("'"+text if text.lstrip().startswith(('=','+','-','@')) else text) in saved,'CSV conserva/neutraliza '+repr(text))
def metrics_range():
 start=dt.date(2000,1,1)
 for days,expected in [(3660,200),(3661,400)]:
  end=start+dt.timedelta(days=days);check(client('gerente').request(f'/Metricas/Datos?desde={start}&hasta={end}')[0]==expected,'Rango de '+str(days)+' días')
 check(client('gerente').request('/Metricas/Datos?desde=9999-12-01&hasta=9999-12-31')[0]==400,'Fecha final no extensible rechazada sin 500')
def quality_view():
 before=tables(audit.db);st,body,_,_=client('administrador').request('/Administracion/CalidadDatos');check(st==200,'Pantalla de validación disponible');check(tables(audit.db)==before,'Consulta sin reparar ni inventar datos');text=html.unescape(body.decode())
 for row in sql('select Nombre from Laboratorios where PropietarioId is null or VigenciaHasta is null'):check(row['Nombre'] in text,'Registro incompleto señalado: '+row['Nombre'])
def safe_content():
 marker='<script>alert("prueba-local")</script>';st,_,h,_=new_open(opening(NombreLaboratorio=marker))
 check(st==302,'Texto sintético guardado para comprobar escape')
 path=h['Location'];st,b,_,_=client('propietario').request(path);text=b.decode();check(marker not in text and '&lt;script&gt;' in text,'Texto escapado, sin script ejecutable')
 sid=int(re.search(r'\d+$',path).group());s=sql('select Version from Solicitudes where Id=?',(sid,),True)
 st,_,_,_=post('propietario','/Gestion/Documento',{'id':sid,'version':s['Version'],'requisitoId':0},[('pdf','../carpeta/archivo.pdf',PDF)])
 check(st==302,'Archivo válido con nombre externo normalizado')
 doc=sql('select * from Documentos where SolicitudId=?',(sid,),True);check('/' not in doc['NombreArchivo'] and '\\' not in doc['NombreArchivo'],'Nombre visible sin rutas');check(re.fullmatch(r'[a-f0-9]{32}\.pdf',doc['ArchivoPdf']) is not None,'Clave privada aleatoria independiente del nombre')
def empty_public_catalogue():
 backup=sql('select Id,Activo from Requisitos')
 try:
  sql('update Requisitos set Activo=0');st,b,_,_=Session(audit.origin).request('/publico/Publico/Requerimientos');text=html.unescape(b.decode());(base/'catalogo-vacio.html').write_text(text,encoding='utf-8');check(st==200 and all(name not in text for name in ['Licencia municipal','Plano arquitectónico','Certificado sanitario']),'Catálogo sin publicados no inventa lista');check('requisit' in text.lower() and ('publicad' in text.lower() or 'configur' in text.lower()),'Informa que no hay catálogo configurado/publicado')
 finally:
  for r in backup:sql('update Requisitos set Activo=? where Id=?',(r['Activo'],r['Id']))

def public_sections():
 st,_,_,_=post('administrador','/Administracion/Seccion',{'Nombre':'Sección campaña pública','Descripcion':'Descripción de sección verificable','Orden':2,'Activa':'true'})
 check(st==302,'Sección creada');sec=sql("select Id from SeccionesRequisitos where Nombre='Sección campaña pública' order by Id desc",one=True)['Id']
 for mandatory in ['true','false']:
  name='Requisito público '+mandatory
  check(post('administrador','/Administracion/Requisito',{'Nombre':name,'Obligatorio':mandatory,'Activo':'true','SeccionId':sec,'Orden':1 if mandatory=='true' else 2},[('Plantilla','plantilla.pdf',PDF)])[0]==302,'Alta requisito con plantilla '+mandatory)
 st,b,_,_=Session(audit.origin).request('/publico/Publico/Requerimientos');page=html.unescape(b.decode())
 check(st==200 and 'Sección campaña pública' in page and 'Descripción de sección verificable' in page,'Agrupación y descripción públicas')
 check(page.index('Requisito público true')<page.index('Requisito público false'),'Orden de requisitos respetado');check('Opcional' in page and 'Obligatorio para enviar' in page,'Obligatoriedad pública explícita')
 rid=sql("select Id from Requisitos where Nombre='Requisito público true' order by Id desc",one=True)['Id']
 st,b,_,_=Session(audit.origin).request('/publico/Publico/Plantilla/'+str(rid));check(st==200 and b==PDF,'Plantilla pública PDF correcta')
def bad_public_location():
 original=sql("select Id from Laboratorios where Codigo='DEMO-LAB-HABILITADO'",one=True)['Id']
 lid=clone('Laboratorios',original,Nombre='Laboratorio ubicación fuera de rango',Lat='999',Lng='999',Codigo='UBICACION-INVALIDA')
 check(Session(audit.origin).request('/publico/Publico/Detalle/9999999')[0]==404,'Ficha inexistente no sustituye otro laboratorio')
 st,b,_,_=Session(audit.origin).request('/publico/Publico/Ruta?laboratorioId='+str(lid));page=html.unescape(b.decode());(base/'PUB-EXT-09-ruta-invalida.html').write_text(page,encoding='utf-8')
 check('destination=999,999' not in page,'No generar ruta a coordenadas 999,999')
def public_expired():
 original=sql("select Id from Laboratorios where Codigo='DEMO-LAB-HABILITADO'",one=True)['Id']
 lid=clone('Laboratorios',original,Nombre='Laboratorio vencido campaña',Codigo='VENCIDO-QA',VigenciaHasta=(NOW-dt.timedelta(days=1)).isoformat(' '))
 c=Session(audit.origin);listing=html.unescape(c.request('/publico/Publico')[1].decode());check('Laboratorio vencido campaña' not in listing,'Directorio excluye vigencia vencida')
 detail=html.unescape(c.request('/publico/Publico/Detalle/'+str(lid))[1].decode());check('cerrado' in detail.lower(),'Ficha vencida se presenta cerrada')
 before=sql('select * from Laboratorios where PropietarioId is null or VigenciaHasta is null');c.request('/publico/Publico');check(before==sql('select * from Laboratorios where PropietarioId is null or VigenciaHasta is null'),'Datos incompletos no se inventan al consultar')
def recent_activity():
 sid=case('en_revision','COHORTE-ACTIVIDAD',PrimerEnvio='2037-01-01 00:00:00');actor=sql("select Id from Usuarios where Email='coordinador@si-lab.local'",one=True)['Id']
 for i in range(60):sql('insert into HistorialTramites (SolicitudId,ActorId,Fecha,Detalle) values (?,?,?,?)',(sid,actor,(dt.datetime(2037,1,1)+dt.timedelta(minutes=i)).isoformat(' '),'Actividad local '+str(i)))
 url='/Metricas?desde=2037-01-01&hasta=2037-01-31&municipio=COHORTE-ACTIVIDAD'
 page=html.unescape(client('gerente').request(url)[1].decode());check('Actividad local 59' in page and 'Actividad local 9<' not in page,'Pantalla conserva últimos movimientos')
 parsed=list(csv.reader(io.StringIO(client('gerente').request(url+'&descargar=true')[1].decode('utf-8-sig'))));i=next(i for i,r in enumerate(parsed) if r and r[0]=='FechaUTC');events=[r for r in parsed[i+1:] if r]
 check(len(events)==50,'CSV contiene los 50 movimientos más recientes');check(events[0][3]=='Actividad local 59' and events[-1][3]=='Actividad local 10','Orden reciente por fecha/ID')
 sql("update Solicitudes set Estado='habilitado' where Id=?",(sid,));data=json.loads(client('gerente').request(url.replace('/Metricas?','/Metricas/Datos?'))[1]);check(data['habilitadas']==1,'El estado es actual para la cohorte histórica seleccionada')
def add_visit(sid,state='validada',result='aprobado',supervisor=None):
 source=sql("select i.Id from Inspecciones i join Solicitudes s on s.Id=i.SolicitudId where s.Codigo='DEMO-LEGAL'",one=True)['Id']
 row=sql('select * from Inspecciones where Id=?',(source,),True);points=json.loads(row['ListaVerificacionJson'])
 for p in points:p['Resultado']='observado' if result=='observado' else 'cumple';p['Observacion']='Corregir área' if result=='observado' else ''
 return clone('Inspecciones',source,SolicitudId=sid,SupervisorId=supervisor or row['SupervisorId'],Estado=state,Resultado=result,Fecha=(CIVIL-dt.timedelta(hours=2)).isoformat(' '),FechaFin=(CIVIL-dt.timedelta(hours=1)).isoformat(' '),ListaVerificacionJson=json.dumps(points,ensure_ascii=False),Programada=1)
def legal_both_expiries():
 original=sql("select Id from Laboratorios where Codigo='DEMO-LAB-HABILITADO'",one=True)['Id']
 for delta in [15,-15]:
  old=NOW+dt.timedelta(days=delta);lid=clone('Laboratorios',original,Codigo='LEGAL-VENTANA-'+str(delta),VigenciaHasta=old.isoformat(' '))
  sid=case('revision_legal',EsRenovacion=1,LaboratorioId=lid);add_visit(sid)
  check(post('abogado','/Gestion/Decidir',{'Id':sid,'Version':1,'Accion':'aprobar_legal','Motivo':'Dictamen de ensayo local'},[('Pdf','dictamen.pdf',PDF)])[0]==302,'Aprobación legal de renovación '+str(delta))
  lab=sql('select * from Laboratorios where Id=?',(lid,),True);end=dt.datetime.fromisoformat(lab['VigenciaHasta'][:26]);expected=(old if delta>0 else NOW).replace(year=NOW.year+1)
  check(abs((end-expected).total_seconds())<10,'Año desde vigencia activa o desde aprobación al estar vencida')
  request=sql('select * from Solicitudes where Id=?',(sid,),True);check(request['Estado']=='habilitado' and request['LaboratorioId']==lid and request['InformeCierrePdf'] is not None,'Mismo laboratorio y cierre independiente')
def jpeg_citation_and_attention():
 from PIL import Image
 jpg=io.BytesIO();Image.new('RGB',(4,4),'white').save(jpg,format='JPEG');jpeg=jpg.getvalue()
 sid=case('observado',OrigenObservacion='campo');iid=add_visit(sid,result='observado')
 fields={'Id':sid,'Version':1,'InspeccionId':iid,'Motivo':'Corregir área observada','FechaEmision':CIVIL.strftime('%Y-%m-%d'),'Vencimiento':(CIVIL+dt.timedelta(days=5)).strftime('%Y-%m-%d'),'AnticipacionDias':0}
 check(post('supervisor','/Gestion/Citacion',fields,[('Evidencia','evidencia.jpg',jpeg)])[0]==302,'Citación con JPEG válido')
 cit=sql('select * from Citaciones where SolicitudId=?',(sid,),True);st,b,h,_=client('propietario').request('/Gestion/Descargar/'+str(cit['Id'])+'?tipo=citacion');check(st==200 and b==jpeg and h.get('Content-Type','').startswith('image/jpeg'),'JPEG privado se descarga con tipo correcto')
 fields['Version']=sql('select Version from Solicitudes where Id=?',(sid,),True)['Version'];check(post('supervisor','/Gestion/Citacion',fields,[('Evidencia','otra.jpg',jpeg)])[0]==409,'Segunda citación pendiente bloqueada')
 version=fields['Version'];sup=sql("select Id from Usuarios where Email='supervisor@si-lab.local'",one=True)['Id']
 check(post('coordinador','/Gestion/Decidir',{'Id':sid,'Version':version,'Accion':'asignar','SupervisorId':sup,'Motivo':'Verificar corrección'})[0]==302,'Nueva visita por observación de campo')
 new=sql('select * from Inspecciones where SolicitudId=? and Estado=? order by Id desc',(sid,'pendiente'),True);sql('update Inspecciones set Programada=1,Fecha=?,FechaFin=? where Id=?',((CIVIL-dt.timedelta(hours=1)).isoformat(' '),CIVIL.isoformat(' '),new['Id']))
 version=sql('select Version from Solicitudes where Id=?',(sid,),True)['Version'];act={'Id':sid,'Version':version,'Accion':'acta','Motivo':'Verificación conforme','Resultado':'aprobado','DiasSubsanacion':15}
 for i,p in enumerate(json.loads(new['ListaVerificacionJson'])):act[f'Verificaciones[{i}].Id']=p['Id'];act[f'Verificaciones[{i}].Resultado']='cumple'
 check(post('supervisor','/Gestion/Decidir',act,[('Pdf','acta.pdf',PDF)])[0]==302,'Acta nueva conforme registrada')
 version=sql('select Version from Solicitudes where Id=?',(sid,),True)['Version'];check(post('coordinador','/Gestion/Decidir',{'Id':sid,'Version':version,'Accion':'aprobar_acta','Motivo':'Conforme revisada'})[0]==302,'Coordinador valida acta conforme')
 check(sql('select FechaAtencion from Citaciones where Id=?',(cit['Id'],),True)['FechaAtencion'] is not None,'Citación anterior queda atendida')

SCENARIOS=[('AUTH-EXT-02',auth_invalid),('AUTH-EXT-04',external_return),('ADM-EXT-01',all_roles),('ADM-EXT-04',edit_blank_password),('ADM-EXT-05',credential_change),('ADM-EXT-06',suspend_reactivate),('ADM-EXT-07',admin_protections),('ADM-EXT-08',supervisor_protected),('REG-EXT-01',coordinate_bounds),('REG-EXT-03',text_limits),('REG-EXT-07',upload_limits),('SEG-EXT-14',services_boundaries),('SEG-EXT-13',renewal_window),('MET-EXT-01',metrics_exact),('MET-EXT-03',metrics_average),('MET-EXT-06',csv_security),('MET-EXT-10',metrics_range),('ADM-EXT-20',quality_view),('SEC-EXT-07',safe_content),('PUB-EXT-03',empty_public_catalogue)]
SCENARIOS += [('PUB-EXT-02',public_sections),('PUB-EXT-09',bad_public_location),('PUB-EXT-10',public_expired),('MET-EXT-09',recent_activity),('LEG-EXT-08',legal_both_expiries),('SUP-EXT-19',jpeg_citation_and_attention)]
if __name__=='__main__':
 if len(sys.argv)>1 and (base/'resultados-suplementarios.json').exists():results=json.loads((base/'resultados-suplementarios.json').read_text(encoding='utf-8'))
 try:
  audit.start()
  for id,fn in SCENARIOS:
   if len(sys.argv)>1 and id not in sys.argv[1:]:continue
   checks=[]
   try:fn();outcome='Exitoso';error=None
   except Exception as ex:outcome='Revisar';error=str(ex);traceback.print_exc()
   results=[r for r in results if r['id']!=id]
   results.append({'id':id,'status':outcome,'method':'HTTP contra proceso local real y fixtures SQLite sintéticos','checks':checks,'error':error})
   (base/'resultados-suplementarios.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps({'id':id,'status':outcome,'assertions':len(checks),'error':error},ensure_ascii=False),flush=True)
 finally:audit.stop()
