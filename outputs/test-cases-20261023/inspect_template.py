import zipfile,xml.etree.ElementTree as E,json
from pathlib import Path
p=Path(r'C:/Users/Usuario/Downloads/Template Reporte de Bugs.xlsx')
n={'m':'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
with zipfile.ZipFile(p) as z:
 ss=[]
 if 'xl/sharedStrings.xml' in z.namelist():ss=[''.join(si.itertext()) for si in E.fromstring(z.read('xl/sharedStrings.xml')).findall('m:si',n)]
 rel={r.get('Id'):r.get('Target') for r in E.fromstring(z.read('xl/_rels/workbook.xml.rels'))}
 result=[]
 for s in E.fromstring(z.read('xl/workbook.xml')).find('m:sheets',n):
  t=rel[s.get('{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id')];t=t.lstrip('/') if t.startswith('/') else 'xl/'+t
  root=E.fromstring(z.read(t));cells={}
  for c in root.findall('m:sheetData/m:row/m:c',n):
   v=c.find('m:v',n);v=v.text if v is not None else ''
   if c.get('t')=='s':v=ss[int(v)]
   if c.get('t')=='inlineStr':v=''.join(c.find('m:is',n).itertext())
   if v:cells[c.get('r')]=v
  result.append({'name':s.get('name'),'file':t,'cells':cells,'merges':[x.attrib for x in root.findall('m:mergeCells/m:mergeCell',n)],'dimension':root.find('m:dimension',n).attrib,'features':[k.tag.split('}')[-1] for k in root]})
 (Path(__file__).parent/'bug_template.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
 print(json.dumps(result,ensure_ascii=False,indent=2))
