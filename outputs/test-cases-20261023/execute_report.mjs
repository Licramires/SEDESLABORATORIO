import fs from 'node:fs/promises';
import {FileBlob,SpreadsheetFile} from '@oai/artifact-tool';
const base=new URL('.',import.meta.url).pathname.replace(/^\/([A-Za-z]:)/,'$1');
if(process.argv.includes('--verify-saved')){
 for(const [file,ranges] of [['TEST_CASES_PROYECTO_EJECUTADOS.xlsx',['B3:G4','B183:G185','J183:N185']],['REPORTE_DE_BUGS_PROYECTO.xlsx',['A2:M5','C25:D32']]]){
  const saved=await SpreadsheetFile.importXlsx(await FileBlob.load(base+file));
  console.log((await saved.inspect({kind:'sheet',include:'id,name',maxChars:600})).ndjson);
  for(let i=0;i<ranges.length;i++){
   const p=await saved.render({sheetName:saved.worksheets.getItemAt(0).name,range:ranges[i],scale:1,format:'png'});
   await fs.writeFile(base+'verificado-'+file.replace('.xlsx','')+'-'+i+'.png',new Uint8Array(await p.arrayBuffer()));
  }
 }
 process.exit(0);
}
const execution=JSON.parse(await fs.readFile(base+'ejecucion-consolidada.json','utf8'));
const rows=JSON.parse(await fs.readFile(base+'cases.json','utf8'));
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load(base+'TEST_CASES_PROYECTO_REVISADOS.xlsx'));
const s=wb.worksheets.getItemAt(0);const end=rows.length+2;
// Se conservan los identificadores y la definición revisada; se corrigen los pasos
// legales cruzados antes de registrar la ejecución y se agregan columnas de prueba.
s.getRange(`A3:I${end}`).values=rows.map((r,i)=>r.map((v,j)=>j===6?execution.cases[i].status:v));
s.getRange('J2:N2').copyFrom(s.getRange('E2:I2'),'all');
s.getRange('J2:N2').values=[['Método de ejecución','Resultado real / alcance','Evidencia (carpeta de este Excel)','Fecha de campaña','Bug / paso pendiente']];
s.getRange(`J3:N${end}`).copyFrom(s.getRange(`D3:H${end}`),'all');
s.getRange(`J3:N${end}`).values=execution.cases.map(r=>[r.method.join('\n'),r.actual.join('\n\n'),r.evidence.join('\n'),execution.date,r.note.join('\n')]);
s.getRange(`J3:N${end}`).format.wrapText=true;s.getRange(`J3:N${end}`).format.verticalAlignment='top';
for(const [col,width] of [['J',34],['K',72],['L',49],['M',23],['N',41]])s.getRange(`${col}:${col}`).format.columnWidth=width;
s.getRange('J2:N2').format.rowHeight=46;
s.getRange('A1').values=[['SI_Lab · Campaña local 23/10/2026: 183 casos — 172 exitosos, 2 fallidos y 9 bloqueados. 3 bugs confirmados.\nCada fila indica método, alcance y evidencia. Bloqueado = ejecución parcial pendiente; no equivale a aprobado. Producción excluida.']];
s.getRange('A1:I1').format.rowHeight=46;
execution.cases.forEach((r,i)=>{
 s.getRange(`G${i+3}`).format.fill={Exitoso:'#C6EFCE',Fallido:'#FFC7CE',Bloqueado:'#FFEB9C'}[r.status];
 const lengths=[r.method.join('\n'),r.actual.join('\n\n'),r.evidence.join('\n'),r.note.join('\n')];
 const widths=[28,64,41,34];
 const lines=Math.max(...lengths.map((t,j)=>t.split('\n').reduce((n,l)=>n+Math.max(1,Math.ceil(l.length/widths[j])),0)));
 const original=Math.max(...rows[i].map((v,j)=>v.split('\n').reduce((n,l)=>n+Math.ceil((l.length||1)/[8,14,25,36,42,36,14,20,20][j]),0)));
 s.getRange(`A${i+3}:N${i+3}`).format.rowHeight=Math.min(409,Math.max(105,lines*14+16,original*14+16));
});
wb.recalculate();
console.log((await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!',options:{useRegex:true,maxResults:10},maxChars:500})).ndjson);
await (await SpreadsheetFile.exportXlsx(wb)).save(base+'TEST_CASES_PROYECTO_EJECUTADOS.xlsx');
for(const [name,range] of [['ejecucion-inicio','B1:G4'],['ejecucion-evidencias','J2:N4'],['ejecucion-cierre','B179:G185']]){
 const p=await wb.render({sheetName:s.name,range,scale:1,format:'png'});await fs.writeFile(base+name+'.png',new Uint8Array(await p.arrayBuffer()));
}
const bugs=[
 ['Conflictos y validaciones dejan una respuesta sin regreso al expediente','Media','Media',
 'BUG-01 · 23/10/2026 · E2E-EXT-04. Un error 409 o la validación 400 del motivo abandona el panel y deja solo texto. El control de datos funciona, pero la recuperación del flujo resulta confusa.',
 'Entorno local: http://localhost:5281 · navegador integrado de escritorio. Copia desechable. Propietario, solicitud #16 en borrador con requisitos completos, abierta en dos pestañas.',
 '1. Abrir el mismo borrador en dos pestañas.\n2. Enviar a revisión desde la primera.\n3. Enviar desde la segunda con la versión antigua.\n4. Comprobar los controles para volver al expediente y corregir.',
 'HTTP 409: «El trámite cambió. Recargue la página antes de continuar». No hay enlaces ni formulario. Recargar repite el POST obsoleto. También ocurre al observar un PDF sin motivo (HTTP 400).\nEvidencia: evidencia-ui/SEG-LL-04.txt; COO-EXT-03-vacio.txt; bug-409-sin-regreso.png.',
 'Mostrar el conflicto o el error dentro del panel, con enlace GET al expediente y una forma clara de corregir/reintentar sobre datos actuales, conservando los bloqueos de validación.',
 'bug-409-sin-regreso.png'],
 ['Aprobar otra vez el mismo PDF duplica la decisión en el historial','Media','Media',
 'BUG-02 · 23/10/2026 · E2E-EXT-04 y observación en COO-EXT-02. Tras aprobar un documento, el formulario actualizado mantiene la aprobación disponible; repetirla genera otro movimiento idéntico para la misma versión del PDF.',
 'Entorno local: http://localhost:5281 · navegador integrado de escritorio. Coordinador. Solicitud #16 en revisión documental; documento #40 «Licencia municipal», PDF v1, inicialmente pendiente.',
 '1. Abrir la solicitud y aprobar el documento #40.\n2. Volver a abrir el detalle para usar una versión reciente del formulario.\n3. Pulsar otra vez «Aprobar este documento» para el mismo PDF v1.\n4. Revisar el historial de la solicitud.',
 'Se guardan dos aprobaciones iguales del documento #40, versión 1; se vuelve a modificar la fecha de revisión y la versión del trámite sin una nueva revisión documental.\nEvidencia: evidencia-ui/BUG-02-aprobacion-repetida.txt; bug-02-historial.png.',
 'Una aprobación repetida del mismo PDF ya conforme debe ser idempotente o quedar deshabilitada; no debe añadir una decisión redundante. Debe mantenerse la opción legítima de observar cuando corresponda.',
 'bug-02-historial.png'],
 ['La ruta pública admite coordenadas persistidas fuera de rango','Media','Media',
 'BUG-03 · 23/10/2026 · PUB-EXT-09. Ficha y ruta públicas no validan las coordenadas guardadas antes de generar el destino. El formulario de alta sí bloquea coordenadas inválidas.',
 'Entorno local: http://localhost:5281 · navegador integrado de escritorio. Fixture de datos dañados creado únicamente en la copia desechable: laboratorio #6 «Laboratorio ubicación fuera de rango», latitud/longitud 999. No es un registro original afectado.',
 '1. Preparar en la copia local el fixture con latitud y longitud 999.\n2. Abrir /publico/Publico/Detalle/6.\n3. Abrir /publico/Publico/Ruta?laboratorioId=6.\n4. Revisar coordenadas y destino del enlace de Maps sin navegar al servicio externo.',
 'La ficha presenta 999,999 y genera un marcador; la página de ruta ofrece Google Maps con destination=999,999. El laboratorio inexistente sí devuelve 404.\nEvidencia: evidencia-ui/PUB-EXT-09.txt; bug-03-ruta-invalida.png; PUB-EXT-09-ruta-invalida.html.',
 'Validar latitud −90…90 y longitud −180…180 al consultar datos persistidos. Si la ubicación es inválida, informar la carencia y no generar marcador ni ruta a un destino falso.',
 'bug-03-ruta-invalida.png']
];
const template=await SpreadsheetFile.importXlsx(await FileBlob.load('C:/Users/Usuario/Downloads/Template Reporte de Bugs.xlsx'));
const b=template.worksheets.getItem('Bugs_Reporting');b.deleteAllDrawings();
for(let i=0;i<3;i++){
 const top=5+i*10;const data=bugs[i];b.getRange(`D${top}:D${top+7}`).values=data.slice(0,8).map(x=>[x]);
 b.getRange(`A${top}`).values=[['QA local']];b.getRange(`B${top}`).values=[['BUG-0'+(i+1)]];
 b.getRange(`D${top}:D${top+7}`).format.wrapText=true;b.getRange(`D${top}:D${top+7}`).format.verticalAlignment='top';
 for(const [offset,height] of [[0,32],[1,20],[2,20],[3,64],[4,76],[5,82],[6,400],[7,64]])b.getRange(`C${top+offset}:D${top+offset}`).format.rowHeight=height;
 const png=await fs.readFile(base+'evidencia-ui/'+data[8]);
 b.images.add({dataUrl:'data:image/png;base64,'+png.toString('base64'),alt:'Evidencia BUG-0'+(i+1),anchor:{from:{row:top+5,col:3,rowOffsetPx:86,colOffsetPx:12},extent:{widthPx:760,heightPx:427}}});
}
template.recalculate();
console.log((await template.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!',options:{useRegex:true,maxResults:10},maxChars:500})).ndjson);
await (await SpreadsheetFile.exportXlsx(template)).save(base+'REPORTE_DE_BUGS_PROYECTO.xlsx');
for(let i=0;i<3;i++){
 const p=await template.render({sheetName:b.name,range:`A${i?4+i*10:2}:${i?'D':'M'}${12+i*10}`,scale:1,format:'png'});await fs.writeFile(base+'reporte-bug-'+(i+1)+'.png',new Uint8Array(await p.arrayBuffer()));
}
console.log(JSON.stringify({cases:execution.counts,bugs:bugs.length}));
