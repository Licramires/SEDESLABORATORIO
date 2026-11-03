import sqlite3,json
from pathlib import Path
root=Path(r'C:/Users/Usuario/.codex/visualizations/2026/10/18/01a15111-6728-7203-8671-331a288cb707/campana-183-20261023')
assert json.loads((root/'ensayo-local.json').read_text())['purpose']=='disposable-local-audit'
with sqlite3.connect('file:'+str(root/'local.db')+'?mode=ro',uri=True) as d:
 d.row_factory=sqlite3.Row
 result={'requests':[dict(r) for r in d.execute('select Id,Estado,Version,DatosLaboratorioPropuesto from Solicitudes where Id in (7,16,17)')], 'inspection7':[dict(r) for r in d.execute('select Id,Estado,Acta,Resultado,NotasAdicionales,ListaVerificacionJson from Inspecciones where SolicitudId=7')], 'send16':d.execute("select count(*) from HistorialTramites where SolicitudId=16 and Detalle='Solicitud enviada para revisión documental.'").fetchone()[0]}
 Path(__file__).with_name('ui-db-evidence.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8');print(json.dumps(result,ensure_ascii=False,indent=2))
