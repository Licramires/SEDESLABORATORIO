"""Preserva funciones nativas omitidas/cambiadas por el exportador y verifica el XLSX.
La autoría de valores y formato se realiza con Artifact Tool. Este ajuste OOXML
se limita a autofiltro/impresión sin API pública y colores condicionales originales.
"""
import copy, hashlib, json, zipfile, xml.etree.ElementTree as E
from pathlib import Path
base=Path(__file__).parent
source=Path(r'C:/Users/Usuario/Downloads/TEST_CASES_SPRINT1 1.xlsx')
dest=base/'TEST_CASES_PROYECTO_REVISADOS.xlsx'
ns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'
n={'m':ns}
E.register_namespace('',ns)
rows=json.loads((base/'cases.json').read_text(encoding='utf-8'))
end=len(rows)+2
with zipfile.ZipFile(source) as z:
 original=E.fromstring(z.read('xl/worksheets/sheet1.xml'))
 original_styles=E.fromstring(z.read('xl/styles.xml'))
with zipfile.ZipFile(dest) as z:
 parts={name:z.read(name) for name in z.namelist()}
sheet=E.fromstring(parts['xl/worksheets/sheet1.xml'])
styles=E.fromstring(parts['xl/styles.xml'])
for tag in ['autoFilter','conditionalFormatting','pageMargins','pageSetup']:
 for old in sheet.findall('m:'+tag,n): sheet.remove(old)
 for old in original.findall('m:'+tag,n):
  new=copy.deepcopy(old)
  if tag=='autoFilter': new.set('ref',f'A2:I{end}')
  if tag=='conditionalFormatting': new.set('sqref',f'G3:G{end}')
  # Mantener el orden de elementos establecido por el esquema de SpreadsheetML.
  order=['sheetPr','dimension','sheetViews','sheetFormatPr','cols','sheetData','sheetCalcPr','sheetProtection','protectedRanges','scenarios','autoFilter','sortState','dataConsolidate','customSheetViews','mergeCells','phoneticPr','conditionalFormatting','dataValidations','hyperlinks','printOptions','pageMargins','pageSetup','headerFooter','rowBreaks','colBreaks','customProperties','cellWatches','ignoredErrors','smartTags','drawing','legacyDrawing','legacyDrawingHF','picture','oleObjects','controls','webPublishItems','tableParts','extLst']
  position=next((i for i,k in enumerate(sheet) if k.tag.split('}')[-1] in order and order.index(k.tag.split('}')[-1])>order.index(tag)),len(sheet))
  sheet.insert(position,new)
dxfs=styles.find('m:dxfs',n)
source_dxfs=list(original_styles.find('m:dxfs',n))
for i,original_dxf in enumerate(source_dxfs):
 dxfs.remove(list(dxfs)[i]); dxfs.insert(i,copy.deepcopy(original_dxf))
parts['xl/worksheets/sheet1.xml']=E.tostring(sheet,encoding='utf-8',xml_declaration=True)
parts['xl/styles.xml']=E.tostring(styles,encoding='utf-8',xml_declaration=True)
tmp=dest.with_suffix('.tmp')
with zipfile.ZipFile(tmp,'w',zipfile.ZIP_DEFLATED) as z:
 for name,data in parts.items(): z.writestr(name,data)
tmp.replace(dest)

with zipfile.ZipFile(dest) as z:
 assert z.testzip() is None
 for name in z.namelist():
  if name.endswith('.xml') or name.endswith('.rels'): E.fromstring(z.read(name))
 r=E.fromstring(z.read('xl/worksheets/sheet1.xml'))
 shared=[]
 if 'xl/sharedStrings.xml' in z.namelist():
  shared=[''.join(si.itertext()) for si in E.fromstring(z.read('xl/sharedStrings.xml')).findall('m:si',n)]
 cells={}
 for c in r.findall('m:sheetData/m:row/m:c',n):
  v=c.find('m:v',n)
  value=v.text if v is not None else ''
  if c.get('t')=='s': value=shared[int(value)]
  elif c.get('t')=='inlineStr': value=''.join(c.find('m:is',n).itertext())
  cells[c.get('r')]=value
 for i,row in enumerate(rows,3):
  for j,expected in enumerate(row): assert cells.get(chr(65+j)+str(i))==expected,(i,j)
 assert r.find('m:autoFilter',n).get('ref')==f'A2:I{end}'
 assert r.find('m:dataValidations/m:dataValidation',n).get('sqref')==f'G3:G{end}'
 assert r.find('m:conditionalFormatting',n).get('sqref')==f'G3:G{end}'
 assert len(r.findall('m:conditionalFormatting/m:cfRule',n))==5
 assert r.find('m:sheetViews/m:sheetView/m:pane',n).get('ySplit')=='2'
 assert not r.findall('.//m:f',n)
 assert len(E.fromstring(z.read('xl/workbook.xml')).findall('m:sheets/m:sheet',n))==1
summary={'originales_revisados':36,'casos_nuevos':len(rows)-36,'total':len(rows),'estado_de_todos':'No ejecutado','ids_unicos':len(set(row[1] for row in rows)),'filas_comparadas_con_contenido':len(rows),'hojas':1,'autofiltro':f'A2:I{end}','validacion_estados':f'G3:G{end}','encabezados_congelados':2,'sin_formulas':True,'fuente_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'salida_sha256':hashlib.sha256(dest.read_bytes()).hexdigest()}
(base/'verificacion.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(summary,ensure_ascii=False))
