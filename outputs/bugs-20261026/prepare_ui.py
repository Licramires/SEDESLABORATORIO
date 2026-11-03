import sqlite3,json,datetime
from pathlib import Path
root=Path(r'C:/Users/Usuario/.codex/visualizations/2026/10/18/01a15111-6728-7203-8671-331a288cb707/bugs-pendientes-20261026')
assert json.loads((root/'ensayo-local.json').read_text())['purpose']=='disposable-local-audit'
with sqlite3.connect(root/'local.db') as db:
 db.row_factory=sqlite3.Row
 def clone(table,id,**changes):
  r=dict(db.execute('select * from '+table+' where Id=?',(id,)).fetchone());r.pop('Id');r.update(changes);cols=list(r)
  return db.execute('insert into '+table+' ('+','.join(cols)+') values ('+','.join('?' for _ in cols)+')',list(r.values())).lastrowid
 now=datetime.datetime.now(datetime.timezone.utc).replace(tzinfo=None).isoformat(' ')
 source=db.execute("select Id from Solicitudes where Codigo='DEMO-LEGAL'").fetchone()[0];requests={}
 for state in ['en_revision','borrador']:
  data=json.loads(db.execute('select DatosLaboratorioPropuesto from Solicitudes where Id=?',(source,)).fetchone()[0]);data['nombre']='Laboratorio corrección '+state
  sid=clone('Solicitudes',source,Estado=state,Version=1,Codigo='QA-BUGS-'+state,LaboratorioId=None,EsRenovacion=0,DatosLaboratorioPropuesto=json.dumps(data,ensure_ascii=False),FechaCreacion=now,PrimerEnvio=None if state=='borrador' else now,FechaEnvio=None if state=='borrador' else now,FechaCierre=None,FirmaLegalPdf='',InformeCierrePdf=None)
  docs=[]
  for d in db.execute('select Id from Documentos where SolicitudId=? and Actual=1',(source,)).fetchall():docs.append(clone('Documentos',d[0],SolicitudId=sid,EstadoRevision='pendiente',FechaRevision=None,RevisorId=None,Observacion=''))
  requests[state]={'id':sid,'documentos':docs}
 lid=clone('Laboratorios',4,Nombre='Laboratorio con ubicación por corregir',Lat='999',Lng='999',Codigo='QA-UBICACION-INVALIDA',VigenciaHasta='2030-01-01 00:00:00')
 result={'origin':'http://localhost:5287','requests':requests,'invalidLab':lid,'purpose':'Fixtures sintéticos solo en copia local'}
 (Path(__file__).parent/'fixtures-ui.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(result,ensure_ascii=False))
