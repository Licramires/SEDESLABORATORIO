import json,shutil
from pathlib import Path
from collections import Counter
base=Path(__file__).parent
rows=json.loads((base/'cases.json').read_text(encoding='utf8'));results={r[1]:{'id':r[1],'status':'No ejecutado','method':[],'actual':[],'evidence':[],'note':[]} for r in rows}
def add(id,state,method,actual,evidence,note=''):
 r=results[id];r['status']=state
 for key,value in [('method',method),('actual',actual),('evidence',evidence),('note',note)]:
  if value and value not in r[key]:r[key].append(value)
for r in json.loads((base/'resultados-tecnicos.json').read_text(encoding='utf8')):
 assert r['passed'] and all(c['outcome']=='Passed' for c in r['checks'])
 names=list(dict.fromkeys(c['test'].split('.')[-1].split('(')[0] for c in r['checks']))
 add(r['id'],'Exitoso',r['method'],str(len(r['checks']))+' comprobaciones aprobadas: '+', '.join(names),r['evidence'],'Ejecución de servidor; no acredita clics de interfaz.')
for file in ['resultados-suplementarios.json','resultados-cierre.json']:
 for r in json.loads((base/file).read_text(encoding='utf8')):
  state='Fallido' if r['id']=='PUB-EXT-09' else r['status']
  assert state in ['Exitoso','Fallido'],(file,r['id'],state)
  add(r['id'],state,r['method'],'; '.join(c['assertion'] for c in r['checks'] if c['passed'])+(' | Falló: '+r['error'] if r.get('error') else ''),file+' # '+r['id'])
for r in json.loads((base/'evidencia-ui/resultados-ui.json').read_text(encoding='utf8')):
 for id in r['ids']:add(id,r['status'],'Interacción de navegador integrado en copia local',r['actual'],'evidencia-ui/'+r['evidence'])
for id,detail in [('PUB-EXT-05','Ocho variantes sintéticas: sin soporte, denegado/no disponible/timeout en mapa y ruta; ninguna navega indebidamente.'),('PUB-EXT-06','Con coordenadas sintéticas se elige el abierto más cercano, excluyendo el cerrado y conservando origen/destino.')]:
 tap=(base/(id+'.tap')).read_text(encoding='utf8');assert '# fail 0' in tap and '# pass 0' not in tap
 add(id,'Exitoso','JavaScript publicado; proveedor GPS simulado',detail,id+'.tap','No usa ubicación física ni comprueba el permiso del sistema.')
visual=Path(r'C:/Users/Usuario/.codex/visualizations/2026/10/18/01a15111-6728-7203-8671-331a288cb707')
shutil.copy2(visual/'campana-183-operaciones-20261023/operaciones.json',base/'operaciones-ejecutadas.json')
shutil.copy2(visual/'campana-183-restauracion-arranque-20261023/operaciones.json',base/'restauracion-arranque.json')
op=json.loads((base/'operaciones-ejecutadas.json').read_text(encoding='utf8'));assert not any(r['test']=='fallo' for r in op)
for id,detail in {
 'OPS-EXT-01':'Respaldo extraído: 351 archivos; hashes, SQLite y 47 archivos privados íntegros. Dependencias reconstruidas, interfaz y compilación correctas; 360 pruebas del código restaurado aprobadas. Arranque, seis roles y descarga privada comprobados.',
 'OPS-EXT-02':'Corte del cliente durante carga: base y archivos sin cambios parciales.',
 'OPS-EXT-03':'Proceso detenido durante carga y reiniciado: integridad correcta, sin registros parciales, sesión recuperada.',
 'OPS-EXT-04':'Almacenamiento inaccesible: respuesta 503 sin alterar base ni archivos.',
 'OPS-EXT-05':'Escritura con SQLite ocupada: respuesta 409 sin efectos parciales; concurrencia conserva solo un cambio.',
 'OPS-EXT-06':'Caída después del archivo y antes del commit: archivo huérfano conservado privadamente para recuperación; tablas confirmadas y archivos registrados preservados.',
 'OPS-EXT-09':'60 lecturas con 12 simultáneas: todas 200, p95 0,079 s. 16 escrituras con 8 simultáneas: una guardada y 15 conflictos rechazados.'}.items():
 add(id,'Exitoso','Procesos locales reales; fallos controlados en copia desechable',detail,'restauracion-verificada.json; restauracion-arranque.json; evidencia-restauracion/restauracion-actual-360.trx' if id=='OPS-EXT-01' else 'operaciones-ejecutadas.json')
blocks={
 'PUB-HL-02':('No se dispone de una posición física verificable ni de la interacción manual del permiso del sistema. La URL y el cálculo con GPS sintético sí se probaron.','PUB-EXT-06.tap'),
 'PUB-EXT-04':('La denegación simulada funciona; queda pendiente rechazar el permiso real del navegador/sistema. No se certifica con una simulación.','PUB-EXT-05.tap'),
 'REG-HL-03':('Se intentó tamaño 390×844, pero el navegador mantuvo 1280 px: no se pudo validar realmente el formulario móvil.','evidencia-ui/REG-EXT-05.txt'),
 'E2E-EXT-06':('Solo se pudo usar el navegador integrado de escritorio. El cambio de tamaño solicitado no tuvo efecto; faltan otros navegadores y dispositivos.','evidencia-ui/resultados-ui.json'),
 'E2E-EXT-07':('Faltan formularios y catálogo institucionales definitivos y propietario/vigencia de tres laboratorios históricos; no se inventaron esos datos. La vista de carencias sí se verificó.','resultados-suplementarios.json # ADM-EXT-20'),
 'E2E-EXT-05':('Revisión parcial de etiquetas y foco en pantallas de seis roles guardada. Falta el recorrido completo con Tab/Shift+Tab, ampliación de texto y medición de contraste. La herramienta no permitió enfocar un control de fecha. No se declara accesibilidad aprobada.','evidencia-ui/accesibilidad.json'),
 'PUB-EXT-08':('El JavaScript real, ejecutado sin Leaflet, informa mapa no disponible en directorio y ficha. Falta reproducir el bloqueo externo dentro del navegador para confirmar el recorrido completo.','PUB-EXT-08-fallback.json'),
 'SUP-EXT-08':('Aislamiento de supervisor y datos del recorrido verificados en servidor. Falta comprobar en navegador el orden diario y los enlaces de cada visita: la navegación quedó bloqueada por la herramienta al mostrar una respuesta de acceso denegado.','por-caso/SUP-EXT-08.trx')}
for id,(detail,evidence) in blocks.items():add(id,'Bloqueado','Comprobación parcial; pendiente manual indicado',detail,evidence,'No marcar ejecutado completamente hasta cerrar el paso pendiente.')
add('E2E-EXT-04','Fallido','Recorrido real en navegador','Un conflicto deja una respuesta aislada sin regreso al expediente. Aprobar otra vez el mismo PDF desde un formulario reciente añade otra decisión idéntica al historial.','evidencia-ui/bug-409-sin-regreso.png; evidencia-ui/BUG-02-aprobacion-repetida.txt','BUG-01 y BUG-02')
add('PUB-EXT-09','Fallido','HTTP y navegador con coordenadas sintéticas inválidas persistidas','La ficha muestra 999,999 y la ruta pública genera Google Maps destination=999,999; el ID inexistente sí devuelve 404. El registro ordinario bloquea esas coordenadas.','evidencia-ui/PUB-EXT-09.txt; evidencia-ui/bug-03-ruta-invalida.png','BUG-03')
for id in ['SEG-LL-04','COO-EXT-03']:results[id]['note'].append('BUG-01 observado; el bloqueo de datos esperado sí funciona.')
results['COO-EXT-02']['note'].append('BUG-02: aprobación repetida del mismo PDF añade historial redundante.')
results['REG-EXT-03']['evidence'].append('resultados-cierre.json # REG-EXT-02 (tipo/municipio de 100 y 101 caracteres)')
assert all(r['status']!='No ejecutado' for r in results.values()),[r['id'] for r in results.values() if r['status']=='No ejecutado']
output={'date':'23/10/2026 (Bolivia)','counts':dict(Counter(r['status'] for r in results.values())),'cases':[results[r[1]] for r in rows]}
(base/'ejecucion-consolidada.json').write_text(json.dumps(output,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(output['counts'],ensure_ascii=False))
