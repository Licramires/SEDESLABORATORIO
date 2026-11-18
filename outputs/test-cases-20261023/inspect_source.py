import zipfile, xml.etree.ElementTree as E, json
from pathlib import Path
p=Path(r'C:/Users/Usuario/Downloads/TEST_CASES_SPRINT1 1.xlsx')
n={'m':'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
with zipfile.ZipFile(p) as z:
 ss=[]
 if 'xl/sharedStrings.xml' in z.namelist():
  ss=[''.join(t.itertext()) for t in E.fromstring(z.read('xl/sharedStrings.xml')).findall('m:si',n)]
 wb=E.fromstring(z.read('xl/workbook.xml'))
 rel={x.attrib['Id']:x.attrib['Target'] for x in E.fromstring(z.read('xl/_rels/workbook.xml.rels'))}
 all_sheets=[]
 for sh in wb.find('m:sheets',n):
  target=rel[sh.attrib['{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id']]
  target=target.lstrip('/') if target.startswith('/') else 'xl/'+target
  root=E.fromstring(z.read(target))
  rows=[]
  for row in root.findall('m:sheetData/m:row',n):
   cells={}
   for c in row.findall('m:c',n):
    v=c.find('m:v',n); value=v.text if v is not None else ''
    if c.attrib.get('t')=='s': value=ss[int(value)]
    elif c.attrib.get('t')=='inlineStr': value=''.join(c.find('m:is',n).itertext())
    if value: cells[c.attrib['r']]=value
   if cells: rows.append(cells)
  all_sheets.append({'name':sh.attrib['name'],'rows':rows,'xml':target,'merges':[m.attrib for m in root.findall('m:mergeCells/m:mergeCell',n)],'cols':[c.attrib for c in root.findall('m:cols/m:col',n)],'features':[k.tag.split('}')[-1] for k in root]})
 Path(__file__).with_name('source.json').write_text(json.dumps(all_sheets,ensure_ascii=False,indent=2),encoding='utf-8')
 print(json.dumps(all_sheets,ensure_ascii=False,indent=2))
