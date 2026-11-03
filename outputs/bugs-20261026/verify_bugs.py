from pathlib import Path
import sqlite3,zipfile,json,hashlib,xml.etree.ElementTree as E
base=Path(__file__).parent;root=base.parents[1];checkpoint=root/'App_Data/checkpoints/20261026-144304-antes-bugs-pendientes'
manifest=json.loads((checkpoint/'manifest.json').read_text(encoding='utf8'))
assert all(hashlib.sha256((checkpoint/name).read_bytes()).hexdigest()==value for name,value in manifest['sha256'].items())
def snapshot(path,immutable=False):
 c=sqlite3.connect('file:'+str(path)+'?mode=ro'+('&immutable=1' if immutable else ''),uri=True)
 try:
  assert c.execute('pragma integrity_check').fetchone()[0]=='ok'
  tables=[r[0] for r in c.execute("select name from sqlite_master where type='table' and name not like 'sqlite_%' order by name")]
  return {t:c.execute('select * from "'+t+'" order by rowid').fetchall() for t in tables}
 finally:c.close()
assert snapshot(root/'si_lab.db')==snapshot(checkpoint/'si_lab.db',True)
with zipfile.ZipFile(checkpoint/'documentos.zip') as z:
 for name in z.namelist():assert hashlib.sha256(z.read(name)).digest()==hashlib.sha256((root/'App_Data/documentos'/name).read_bytes()).digest()
 private=len(z.namelist())
trx=E.parse(base/'pruebas/bugs-corregidos.trx').getroot();tests=trx.find('.//{*}Counters').attrib
assert tests['failed']=='0' and tests['passed']=='389'
ui=Path(r'C:/Users/Usuario/.codex/visualizations/2026/10/18/01a15111-6728-7203-8671-331a288cb707/bugs-pendientes-20261026')
assert json.loads((ui/'ensayo-local.json').read_text())['purpose']=='disposable-local-audit'
with sqlite3.connect('file:'+str(ui/'local.db')+'?mode=ro',uri=True) as c:
 evidence={'request18':c.execute('select Estado,Version from Solicitudes where Id=18').fetchone(),'approval51':c.execute('select count(*) from HistorialTramites where SolicitudId=18 and Detalle like ?',('Documento #51%aprobado%',)).fetchone()[0],'request19':c.execute('select Estado,Version from Solicitudes where Id=19').fetchone(),'send19':c.execute('select count(*) from HistorialTramites where SolicitudId=19 and Detalle=?',('Solicitud enviada para revisión documental.',)).fetchone()[0]}
assert evidence['approval51']==evidence['send19']==1 and evidence['request19']==('en_revision',2)
results={'date':'26/10/2026 (Bolivia)','checkpoint':str(checkpoint),'original_database_preserved':True,'private_files_preserved':private,'server_tests':tests,'javascript_tests_passed':28,'browser':'Navegador integrado; escritorio 1280×720; copia desechable local','ui_database_checks':evidence,'bugs':[
 {'id':'BUG-01','status':'Corregido y verificado','evidence':['BUG-01-validacion.txt','BUG-01-conflicto.txt','BUG-01-recuperado.txt','BUG-01-conflicto.png']},
 {'id':'BUG-02','status':'Corregido y verificado','evidence':['BUG-02-documento-aprobado.txt','pruebas/bugs-corregidos.trx']},
 {'id':'BUG-03','status':'Corregido y verificado','evidence':['BUG-03-ficha.txt','BUG-03-ruta.png','pruebas/limites-mapa.trx']}],
 'case_retest':{'PUB-EXT-09':'Exitoso en reejecución dirigida; incluye límites válidos e ID inexistente de la regresión existente.','E2E-EXT-04':'BUG-01 y BUG-02 corregidos. La campaña completa de todos los controles/estados no se certifica con esta reejecución dirigida.'},'manual_cases_not_certified':['PUB-HL-02','PUB-EXT-04','REG-HL-03','E2E-EXT-06','E2E-EXT-07','E2E-EXT-05','PUB-EXT-08','SUP-EXT-08','REG-EXT-05']}
for bug in results['bugs']:
 for name in bug['evidence']:assert (base/name).exists()
(base/'verificacion.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf8')
print(json.dumps({'bugs_verified':len(results['bugs']),'server_tests_passed':tests['passed'],'private_files_preserved':private,'original_database_preserved':True,'browser_database_checks':evidence},ensure_ascii=False))
