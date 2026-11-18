import zipfile,json,hashlib,shutil,sqlite3
from pathlib import Path
project=Path(__file__).resolve().parents[2]
backup=project/'App_Data/checkpoints/20261022-234400-antes-cierre-pendientes'
target=Path(r'C:/Users/Usuario/.codex/visualizations/2026/10/18/01a15111-6728-7203-8671-331a288cb707/campana-183-restauracion-20261023')
manifest=json.loads((backup/'manifest.json').read_text())
assert not target.exists(),'No sobrescribir una restauración existente'
for name,expected in manifest['sha256'].items():assert hashlib.sha256((backup/name).read_bytes()).hexdigest()==expected
target.mkdir()
with zipfile.ZipFile(backup/'proyecto.zip') as z:
 for entry in z.infolist():
  dest=(target/entry.filename).resolve();assert target==dest or target in dest.parents
 z.extractall(target)
shutil.copy2(backup/'si_lab.db',target/'si_lab.db')
with sqlite3.connect(str(target/'si_lab.db')) as d:
 integrity=d.execute('pragma integrity_check').fetchone()[0];foreign=d.execute('pragma foreign_key_check').fetchall()
 assert integrity=='ok' and not foreign
 private=list((target/'App_Data/documentos').glob('*'))
result={'case':'OPS-EXT-01','target':str(target),'archiveFiles':manifest['archivos'],'hashesVerified':True,'integrity':integrity,'foreignKeyProblems':len(foreign),'privateFiles':len(private)}
Path(__file__).with_name('restauracion-verificada.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(result))
