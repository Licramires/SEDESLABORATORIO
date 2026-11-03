"""Verificación independiente y preservación de funciones nativas sin API pública.
Los valores, estilos e imágenes se han escrito con Artifact Tool.
"""
import copy,hashlib,json,zipfile,xml.etree.ElementTree as E
from pathlib import Path
from collections import Counter
base=Path(__file__).parent;ns='http://schemas.openxmlformats.org/spreadsheetml/2006/main';n={'m':ns};E.register_namespace('',ns)
order=['sheetPr','dimension','sheetViews','sheetFormatPr','cols','sheetData','sheetCalcPr','sheetProtection','protectedRanges','scenarios','autoFilter','sortState','dataConsolidate','customSheetViews','mergeCells','phoneticPr','conditionalFormatting','dataValidations','hyperlinks','printOptions','pageMargins','pageSetup','headerFooter','rowBreaks','colBreaks','customProperties','cellWatches','ignoredErrors','smartTags','drawing','legacyDrawing','legacyDrawingHF','picture','oleObjects','controls','webPublishItems','tableParts','extLst']
def insert(root,child):
 tag=child.tag.split('}')[-1];position=next((i for i,k in enumerate(root) if k.tag.split('}')[-1] in order and order.index(k.tag.split('}')[-1])>order.index(tag)),len(root));root.insert(position,child)
def patch(src,dest,cases=False):
 with zipfile.ZipFile(src) as z:original=E.fromstring(z.read('xl/worksheets/sheet1.xml'));styles=E.fromstring(z.read('xl/styles.xml'))
 with zipfile.ZipFile(dest) as z:parts={p:z.read(p) for p in z.namelist()}
 root=E.fromstring(parts['xl/worksheets/sheet1.xml'].decode('utf8'))
 for tag in (['autoFilter','conditionalFormatting','pageMargins','pageSetup'] if cases else ['pageMargins','pageSetup']):
  for old in root.findall('m:'+tag,n):root.remove(old)
  for child in original.findall('m:'+tag,n):
   child=copy.deepcopy(child)
   if tag=='autoFilter':child.set('ref','A2:N185')
   insert(root,child)
 if cases:
  exported=E.fromstring(parts['xl/styles.xml'].decode('utf8'));orig=styles.find('m:dxfs',n);dxfs=exported.find('m:dxfs',n)
  if orig is not None and dxfs is not None:
   for i,old in enumerate(orig):dxfs.remove(list(dxfs)[i]);dxfs.insert(i,copy.deepcopy(old))
  parts['xl/styles.xml']=E.tostring(exported,encoding='utf-8',xml_declaration=True)
 parts['xl/worksheets/sheet1.xml']=E.tostring(root,encoding='utf-8',xml_declaration=True)
 temp=dest.with_suffix('.verified.tmp')
 with zipfile.ZipFile(temp,'w',zipfile.ZIP_DEFLATED) as z:
  for p,data in parts.items():z.writestr(p,data)
 temp.replace(dest)
def read(path,sheet=1):
 with zipfile.ZipFile(path) as z:
  assert z.testzip() is None
  for part in z.namelist():
   if part.endswith(('.xml','.rels')):E.fromstring(z.read(part))
  strings=[''.join(si.itertext()) for si in E.fromstring(z.read('xl/sharedStrings.xml')).findall('m:si',n)] if 'xl/sharedStrings.xml' in z.namelist() else []
  root=E.fromstring(z.read(f'xl/worksheets/sheet{sheet}.xml'));cells={}
  for c in root.findall('m:sheetData/m:row/m:c',n):
   v=c.find('m:v',n);value=v.text if v is not None else ''
   if c.get('t')=='s':value=strings[int(value)]
   elif c.get('t')=='inlineStr':value=''.join(c.find('m:is',n).itertext())
   assert c.get('t')!='e',(path,c.get('r'),value)
   cells[c.get('r')]=value
  return root,cells,parts_info(z)
def parts_info(z):
 return {'sheets':[s.get('name') for s in E.fromstring(z.read('xl/workbook.xml')).findall('m:sheets/m:sheet',n)],'images':len([p for p in z.namelist() if p.startswith('xl/media/')])}
cases=base/'TEST_CASES_PROYECTO_EJECUTADOS.xlsx';bugs=base/'REPORTE_DE_BUGS_PROYECTO.xlsx'
patch(base/'TEST_CASES_PROYECTO_REVISADOS.xlsx',cases,True);patch(Path(r'C:/Users/Usuario/Downloads/Template Reporte de Bugs.xlsx'),bugs)
root,values,info=read(cases);definitions=json.loads((base/'cases.json').read_text(encoding='utf8'));results=json.loads((base/'ejecucion-consolidada.json').read_text(encoding='utf8'))
for i,(row,result) in enumerate(zip(definitions,results['cases']),3):
 for j,value in enumerate(row):assert values[chr(65+j)+str(i)]==(result['status'] if j==6 else value),(i,j)
 assert values['J'+str(i)] and values['K'+str(i)] and values['L'+str(i)] and values['M'+str(i)]
assert len(set(values['B'+str(i)] for i in range(3,186)))==183
assert root.find('m:autoFilter',n).get('ref')=='A2:N185'
assert root.find('m:dataValidations/m:dataValidation',n).get('sqref')=='G3:G185'
assert root.find('m:sheetViews/m:sheetView/m:pane',n).get('ySplit')=='2'
assert len(root.findall('m:conditionalFormatting/m:cfRule',n))==5
br,bv,bi=read(bugs);tr,tv,ti=read(Path(r'C:/Users/Usuario/Downloads/Template Reporte de Bugs.xlsx'))
assert bi['sheets']==ti['sheets']==['Bugs_Reporting','Sheet1'];assert bi['images']==3
assert {c.get('ref') for c in br.findall('m:mergeCells/m:mergeCell',n)}=={c.get('ref') for c in tr.findall('m:mergeCells/m:mergeCell',n)}
for row in [5,15,25]:
 for offset in range(8):assert bv.get('C'+str(row+offset))==tv['C'+str(row+offset)] and bv.get('D'+str(row+offset))
assert 'Univalle' not in '\n'.join(bv.values());assert not read(bugs,2)[1]
missing=[]
for r in results['cases']:
 for reference in r['evidence']:
  for ref in reference.split('; '):
   path=base/ref.split(' # ')[0]
   if not path.exists():missing.append((r['id'],ref))
assert not missing,missing
source=Path(r'C:/Users/Usuario/Downloads/TEST_CASES_SPRINT1 1.xlsx');assert hashlib.sha256(source.read_bytes()).hexdigest()=='a86f0547f82300baf0953c744e2b840d580aadee22d4a6c3bcacba72ec2564ce'
summary={'cases':len(definitions),'statuses':dict(Counter(values['G'+str(i)] for i in range(3,186))),'bugs':3,'template_sheets_merges_labels_preserved':True,'case_ids_and_definitions_verified':True,'all_evidence_paths_exist':True,'outputs':[{ 'file':p.name,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in [cases,bugs]]}
(base/'verificacion-ejecucion.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf8');print(json.dumps(summary,ensure_ascii=False))
