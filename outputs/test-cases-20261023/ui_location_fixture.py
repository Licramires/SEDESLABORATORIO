import sqlite3,json
from pathlib import Path
root=Path(r'C:/Users/Usuario/.codex/visualizations/2026/10/18/01a15111-6728-7203-8671-331a288cb707/campana-183-20261023')
assert json.loads((root/'ensayo-local.json').read_text())['purpose']=='disposable-local-audit'
with sqlite3.connect(str(root/'local.db')) as d:
 d.row_factory=sqlite3.Row
 existing=d.execute("select Id from Laboratorios where Codigo='UBICACION-INVALIDA-UI'").fetchone()
 if existing:lab=existing['Id']
 else:
  row=dict(d.execute("select * from Laboratorios where Codigo='DEMO-LAB-HABILITADO'").fetchone());row.pop('Id');row.update(Nombre='Laboratorio ubicación fuera de rango',Lat='999',Lng='999',Codigo='UBICACION-INVALIDA-UI')
  cols=list(row);lab=d.execute('insert into Laboratorios ('+','.join('"'+k+'"' for k in cols)+') values ('+','.join('?' for _ in cols)+')',[row[k] for k in cols]).lastrowid
 output={'id':lab,'url':f'http://localhost:5281/publico/Publico/Detalle/{lab}','route':f'http://localhost:5281/publico/Publico/Ruta?laboratorioId={lab}'}
 Path(__file__).with_name('ui-location-fixture.json').write_text(json.dumps(output),encoding='utf-8');print(json.dumps(output))
