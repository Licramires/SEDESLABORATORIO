from pathlib import Path
import importlib.util, os, sys, shutil, json
from pypdf import PdfReader
from docx import Document
HERE=Path(__file__).parent.resolve()
runtime=Path('C:/Users/Usuario/.cache/codex-runtimes/codex-primary-runtime/dependencies')
poppler=next((runtime/'native/poppler').rglob('pdfinfo.exe')).parent
os.environ['PATH']=str(poppler)+os.pathsep+os.environ['PATH']
skill=Path('C:/Users/Usuario/.codex/plugins/cache/openai-primary-runtime/documents/26.1025.11240/skills/documents')
spec=importlib.util.spec_from_file_location('docx_renderer',skill/'render_docx.py')
renderer=importlib.util.module_from_spec(spec); spec.loader.exec_module(renderer)
def use_word_pdf(doc_path,user_profile,convert_tmp_dir,stem,verbose=False):
 return str(HERE/'Informe_integral_SI_Lab.pdf'),'Vista previa exportada por Microsoft Word'
renderer.convert_to_pdf=use_word_pdf
renderer.rasterize(str(HERE/'Informe_integral_SI_Lab.docx'),str(HERE/'render'),120,False,False)
pdf=PdfReader(HERE/'Informe_integral_SI_Lab.pdf').pages
report=[]
for i,page in enumerate(pdf):
 text=page.extract_text()
 report.append({'page':i+1,'characters':len(text),'first_lines':text.splitlines()[:3]})
(HERE/'revision_paginas.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))
print('pages',len(pdf))
