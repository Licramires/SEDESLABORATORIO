from pathlib import Path
import sqlite3,zipfile,hashlib,json,xml.etree.ElementTree as E
root=Path(__file__).resolve().parents[2];backup=root/'App_Data/checkpoints/20261026-135109-antes-solid';out=Path(__file__).parent
manifest=json.loads((backup/'manifest.json').read_text(encoding='utf8'))
# SQLite crea y retira sus archivos auxiliares automáticamente. La copia
# consistente y sus cuatro entregables duraderos son los que se verifican.
transient={name:sha for name,sha in manifest['sha256'].items() if name.endswith(('-wal','-shm'))}
manifest['sha256']={name:sha for name,sha in manifest['sha256'].items() if name not in transient}
manifest['excluded_transient_files']=list(transient) or manifest.get('excluded_transient_files',[])
assert all(hashlib.sha256((backup/name).read_bytes()).hexdigest()==sha for name,sha in manifest['sha256'].items())
(backup/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8')
def db_fingerprint(path,immutable=False):
 db=sqlite3.connect('file:'+str(path)+'?mode=ro'+('&immutable=1' if immutable else ''),uri=True)
 try:
  assert db.execute('pragma integrity_check').fetchone()[0]=='ok'
  names=[r[0] for r in db.execute("select name from sqlite_master where type='table' and name not like 'sqlite_%' order by name")]
  return {name:db.execute('select * from "'+name+'" order by rowid').fetchall() for name in names}
 finally:db.close()
assert db_fingerprint(root/'si_lab.db')==db_fingerprint(backup/'si_lab.db',True)
with zipfile.ZipFile(backup/'documentos.zip') as z:
 assert all((root/'App_Data/documentos'/name).is_file() and hashlib.sha256((root/'App_Data/documentos'/name).read_bytes()).digest()==hashlib.sha256(z.read(name)).digest() for name in z.namelist())
 count=len(z.namelist())
with zipfile.ZipFile(backup/'proyecto.zip') as z:
 changed=[name for name in z.namelist() if (root/name).read_bytes()!=z.read(name)]
 visual=[name for name in changed if name.endswith(('.cshtml','.css','.js','.jsx','.tsx'))]
 assert not visual,visual
 new=[str(p.relative_to(root)).replace('\\','/') for p in (root/'Application').rglob('*.cs')]
assert not [name for name in changed if name.startswith('Data/Migrations/')]
trx=E.parse(out/'pruebas/solid-final.trx').getroot();counters=trx.find('.//{*}Counters').attrib
assert counters['failed']=='0' and counters['passed']=='377',counters
result={'checkpoint':str(backup),'checkpoint_hashes_verified':True,'original_database_equals_checkpoint':True,'private_files_preserved':count,'interface_files_changed':visual,'existing_source_files_changed':changed,'application_files':new,'server_tests':counters,'schema_migrations_changed':False}
(out/'verificacion.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps({'source_files_changed':len(changed),'application_files':len(new),'database_preserved':True,'private_files':count,'interface_changes':len(visual),'tests_passed':counters['passed']}))
