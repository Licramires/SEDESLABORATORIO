"""Cierre individual de variantes pendientes en el proceso local de ensayo."""
import supplemental_cases as s
import json,traceback,re,html

def legal_selections():
 sid=s.case();doc=s.sql('select * from Documentos where SolicitudId=? and Actual=1',(sid,),True)
 old=s.clone('Documentos',doc['Id'],Actual=0);foreign=s.sql('select Id from Documentos where SolicitudId<>? and Actual=1',(sid,),True)['Id']
 variants=[([], 'Corregir'),([doc['Id'],doc['Id']], 'Corregir'),([old], 'Corregir'),([foreign], 'Corregir'),([doc['Id']], '')]
 for ids,motive in variants:
  before=s.tables(s.audit.db)
  fields={'Id':sid,'Version':1,'Accion':'observar_legal','Motivo':motive}
  for i,id in enumerate(ids):fields[f'DocumentosObservados[{i}]']=id
  status=s.post('abogado','/Gestion/Decidir',fields)[0]
  s.check(status==302,'Selección legal inválida devuelve al detalle: '+str(ids)+' motivo='+motive)
  s.check(before==s.tables(s.audit.db),'Sin cambios de tabla por selección legal inválida')
  page=html.unescape(s.client('abogado').request('/Gestion/Detalle/'+str(sid))[1].decode())
  s.check(('Seleccione los documentos actuales' if motive else 'Registre la revisión de licencia') in page,'Mensaje de corrección visible')

def registration_catalogue():
 for field,kind in [('TipoLaboratorio','tipo'),('Municipio','municipio')]:
  row=s.sql('select * from Catalogos where Clase=? and Activo=1',(kind,),True)
  s.sql('update Catalogos set Activo=0 where Id=?',(row['Id'],))
  try:
   before=s.tables(s.audit.db);s.check(s.new_open(s.opening(**{field:row['Nombre']}))[0]==200,'Valor inactivo bloqueado '+field);s.check(before==s.tables(s.audit.db),'Sin solicitud parcial')
  finally:s.sql('update Catalogos set Activo=1 where Id=?',(row['Id'],))
  value='T'*100 if kind=='tipo' else 'M'*100
  fixture=s.clone('Catalogos',row['Id'],Nombre=value)
  try:
   s.check(s.new_open(s.opening(**{field:value}))[0]==302,'Catálogo activo de 100 caracteres aceptado '+field)
   before=s.tables(s.audit.db);s.check(s.new_open(s.opening(**{field:value+'X'}))[0]==200,'Catálogo de 101 caracteres bloqueado '+field);s.check(before==s.tables(s.audit.db),'Sin persistir exceso')
  finally:s.sql('update Catalogos set Activo=0 where Id=?',(fixture,))

def establishment():
 c=s.client('propietario');lab=s.sql("select Id from Laboratorios where Codigo='DEMO-LAB-HABILITADO'",one=True)['Id']
 status,body,_,_=c.request('/Gestion/Establecimiento/'+str(lab));page=html.unescape(body.decode());s.check(status==200,'Ficha propia accesible')
 requests=s.sql('select Id,EsRenovacion from Solicitudes where LaboratorioId=?',(lab,))
 s.check(any(r['EsRenovacion'] for r in requests) and any(not r['EsRenovacion'] for r in requests),'Apertura y renovación vinculadas al mismo laboratorio')
 for r in requests:
  s.check('/Gestion/Detalle/'+str(r['Id']) in page,'Vínculo de expediente '+str(r['Id']))
  s.check(c.request('/Gestion/Detalle/'+str(r['Id']))[0]==200,'Expediente propio accesible')
  for doc in s.sql('select Id from Documentos where SolicitudId=?',(r['Id'],)):
   st,b,_,_=c.request('/Gestion/Descargar/'+str(doc['Id']));s.check(st==200 and b.startswith(b'%PDF'),'PDF privado propio descargable '+str(doc['Id']))

def catalogues():
 for kind in ['tipo','municipio','inspeccion','feriado']:
  name=(s.dt.date(2050,1,1)+s.dt.timedelta(days=int(s.uuid.uuid4().hex[:6],16)%100000)).isoformat() if kind=='feriado' else 'Catálogo cierre '+s.uuid.uuid4().hex
  fields={'Clase':kind,'Nombre':name,'Referencia':'Referencia de ensayo','Activo':'true'}
  s.check(s.post('administrador','/Administracion/Catalogo',fields)[0]==302,'Alta catálogo '+kind)
  c=s.sql('select * from Catalogos where Clase=? and Nombre=?',(kind,name),True)
  before=s.tables(s.audit.db);s.check(s.post('administrador','/Administracion/Catalogo',fields)[0]==200,'Duplicado de catálogo bloqueado');s.check(before==s.tables(s.audit.db),'Sin duplicación')
  edit=dict(fields,Id=c['Id'],Version=c['Version'],Referencia='Referencia editada',Activo='false')
  s.check(s.post('administrador','/Administracion/Catalogo',edit)[0]==302,'Edición y desactivación '+kind)
  row=s.sql('select * from Catalogos where Id=?',(c['Id'],),True);s.check(row['Referencia']=='Referencia editada' and not row['Activo'],'Edición persistida')
  before=s.tables(s.audit.db);s.check(s.post('administrador','/Administracion/Catalogo',dict(edit,Referencia='Pestaña antigua'))[0]==409,'Edición obsoleta bloqueada');s.check(before==s.tables(s.audit.db),'No sobreescribe catálogo vigente')
  s.check(s.post('administrador','/Administracion/Catalogo',dict(edit,Version=row['Version'],Clase='inspeccion' if kind!='inspeccion' else 'tipo'))[0]==400,'Clase existente inmutable')
  for change in [{'Nombre':'X'*(101 if kind in ['tipo','municipio'] else 201)},{'Referencia':'X'*1001}]:
   before=s.tables(s.audit.db);s.check(s.post('administrador','/Administracion/Catalogo',dict(edit,Version=row['Version'],**change))[0]==200,'Límites de catálogo rechazados '+kind);s.check(before==s.tables(s.audit.db),'Sin efecto parcial')
 for invalid in ['2040-02-30','03/02/2040','2040-02-03T09:00']:
  before=s.tables(s.audit.db);s.check(s.post('administrador','/Administracion/Catalogo',{'Clase':'feriado','Nombre':invalid,'Activo':'true'})[0]==200,'Feriado inválido rechazado '+invalid);s.check(before==s.tables(s.audit.db),'Sin feriado inválido')

def form_templates():
 for kind in ['formulario_acta','formulario_citacion']:
  current=s.sql('select * from Catalogos where Clase=? and Activo=1',(kind,))
  for row in current:s.sql('update Catalogos set Activo=0 where Id=?',(row['Id'],))
  name='Plantilla cierre '+s.uuid.uuid4().hex;fields={'Clase':kind,'Nombre':name,'Activo':'true'}
  s.check(s.post('administrador','/Administracion/Catalogo',fields,[('Plantilla','plantilla.pdf',s.PDF)])[0]==302,'Plantilla activa '+kind)
  c=s.sql('select * from Catalogos where Nombre=?',(name,),True);before=s.tables(s.audit.db)
  s.check(s.post('administrador','/Administracion/Catalogo',dict(fields,Nombre=name+' segunda'),[('Plantilla','segunda.pdf',s.PDF)])[0]==200,'Segunda plantilla activa bloqueada');s.check(before==s.tables(s.audit.db),'Sin segunda plantilla ni registro parcial')
  s.check(len(s.sql('select Id from Catalogos where Clase=? and Activo=1',(kind,)))==1,'Solo una activa por clase')
  s.check((s.audit.storage/c['ArchivoPdf']).read_bytes()==s.PDF,'Contenido de la plantilla guardada exacto')

def positive_days():
 for days in [1,90]:
  sid=s.case('inspeccion');iid=s.add_visit(sid,state='pendiente',result='observado')
  row=s.sql('select * from Inspecciones where Id=?',(iid,),True);points=json.loads(row['ListaVerificacionJson'])
  fields={'Id':sid,'Version':1,'Accion':'preparar_acta','Resultado':'observado','DiasSubsanacion':days}
  for i,p in enumerate(points):fields[f'Verificaciones[{i}].Id']=p['Id'];fields[f'Verificaciones[{i}].Resultado']='observado';fields[f'Verificaciones[{i}].Observacion']='Corregir área observada'
  s.check(s.post('supervisor','/Gestion/Decidir',fields)[0]==302,'Plazo inclusivo aceptado '+str(days))
  s.check(s.sql('select DiasSubsanacion,PlazoSubsanacion from Inspecciones where Id=?',(iid,),True)['DiasSubsanacion']==days,'Plazo válido persistido')

def states():
 labels={'borrador':'Borrador','en_revision':'Revisión documental','aprobado':'Documentación conforme','inspeccion':'Inspección','revision_acta':'Revisión de acta','observado':'Observado','rechazado':'Rechazado','revision_legal':'Revisión legal','habilitado':'Habilitado'}
 pages=[]
 for state,label in labels.items():
  sid=s.case(state);role='propietario' if state=='borrador' else 'coordinador'
  status,body,_,_=s.client(role).request('/Gestion/Detalle/'+str(sid));page=html.unescape(body.decode());s.check(status==200,'Detalle del estado '+state+' accesible por su rol')
  # Se conserva el HTML real para valorar el texto y las acciones sin atribuir clics.
  (s.base/('estado-http-'+state+'.html')).write_text(page,encoding='utf8')
  match=re.search(r'data-state="'+state+r'">([^<]+)</span>',page)
  s.check(match is not None and bool(match.group(1).strip()),'Etiqueta textual de estado '+state)
  pages.append({'state':state,'role':role,'label':match.group(1)})
 (s.base/'SEG-LL-05-http.json').write_text(json.dumps(pages,ensure_ascii=False,indent=2),encoding='utf8')

scenarios=[('ADM-EXT-01',s.all_roles),('SUP-EXT-22',s.jpeg_citation_and_attention),('LEG-EXT-05',legal_selections),('REG-EXT-02',registration_catalogue),('SEG-EXT-09',establishment),('ADM-EXT-18',catalogues),('ADM-EXT-19',form_templates),('SUP-EXT-17',positive_days),('SEG-LL-05',states),('ADM-EXT-21',catalogues)]
results=json.loads((s.base/'resultados-cierre.json').read_text(encoding='utf8')) if (s.base/'resultados-cierre.json').exists() else []
try:
 s.audit.start()
 for id,fn in scenarios:
  if len(s.sys.argv)>1 and id not in s.sys.argv[1:]:continue
  s.checks=[]
  try:fn();state='Exitoso';error=None
  except Exception as ex:traceback.print_exc();state='Revisar';error=str(ex)
  results=[x for x in results if x['id']!=id];results.append({'id':id,'status':state,'method':'HTTP real en copia local; preparación sintética de casos límite','checks':s.checks,'error':error})
  (s.base/'resultados-cierre.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf8');print(id,state,error,flush=True)
finally:s.audit.stop()
