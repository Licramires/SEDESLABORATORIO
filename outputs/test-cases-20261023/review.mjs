import fs from 'node:fs/promises';
import {FileBlob, SpreadsheetFile} from '@oai/artifact-tool';
const base=new URL('.',import.meta.url).pathname.replace(/^\/([A-Za-z]:)/,'$1');
const wb=await SpreadsheetFile.importXlsx(await FileBlob.load(process.argv.includes('--verify-final')?base+'TEST_CASES_PROYECTO_REVISADOS.xlsx':'C:/Users/Usuario/Downloads/TEST_CASES_SPRINT1 1.xlsx'));
if(process.argv.includes('--verify-final')) {
 console.log((await wb.inspect({kind:'sheet',include:'id,name',maxChars:600})).ndjson);
 console.log((await wb.inspect({kind:'table',range:"'Casos de prueba'!B65:F65",tableMaxRows:1,tableMaxCols:5,maxChars:1200})).ndjson);
 const p=await wb.render({sheetName:'Casos de prueba',range:'B65:F66',scale:1,format:'png'});
 await fs.writeFile(base+'final-verificado.png',new Uint8Array(await p.arrayBuffer()));
 process.exit(0);
}
if(process.argv.includes('--build')) {
 const s=wb.worksheets.getItemAt(0);
 const rows=JSON.parse(await fs.readFile(base+'cases.json','utf8'));
 const end=rows.length+2;
 for(let r=39;r<=end;r++) s.getRange(`A${r}:I${r}`).copyFrom(s.getRange('A3:I3'),'all');
 s.getRange(`A3:I${end}`).values=rows;
 // El título original en I1 quedaba recortado. Se conserva el estilo en una banda de texto.
 s.getRange('A1:I1').clear({applyTo:'contents'});
 s.mergeCells('A1:I1');
 s.getRange('A1').values=[['SI_Lab — 183 casos: 36 revisados y 147 añadidos. Revisión: 23/10/2026. Todos No ejecutado.\nFuentes: docs/CASOS_DE_USO.md, CONTRATO_DATOS.md y flujos actuales. Ensayos en copia local; producción excluida. LL: caso puntual. HL: recorrido. EXT: caso añadido.']];
 s.getRange('A1:I1').format.wrapText=true;
 s.getRange('A1:I1').format.horizontalAlignment='left';
 s.getRange('A1:I1').format.verticalAlignment='center';
 s.getRange('A1:I1').format.font={name:'Calibri',size:11,bold:true,color:'#0785C5'};
 s.getRange('A1:I1').format.rowHeight=42;
 s.getRange('C2').values=[['Título']]; s.getRange('D2').values=[['Descripción y precondición']];
 s.getRange('F2').values=[['Resultados esperados']];s.getRange('H2').values=[['Módulo']];
 s.getRange('I2').values=[['Herramienta de gestión de TC']];
 s.getRange('A2:I2').format.rowHeight=32;
 const widths=[12,18,30,42,48,42,16,25,24];
 const lines=(text,chars)=>text.split('\n').reduce((n,line)=>n+Math.max(1,Math.ceil(line.length/chars)),0);
 rows.forEach((row,i)=>{
   // Altura suficiente para pasos y expectativas sin encoger la tipografía de la plantilla.
   const l=Math.max(...row.map((v,j)=>lines(v,Math.max(8,widths[j]-5))));
   s.getRange(`A${i+3}:I${i+3}`).format.rowHeight=Math.max(90,l*14+14);
   s.getRange(`A${i+3}:I${i+3}`).format.verticalAlignment='top';
 });
 s.getRange(`G3:G${end}`).dataValidation={rule:{type:'list',values:['No ejecutado','Exitoso','Fallido','Bloqueado','No aplica']}};
 // Extender las mismas cinco categorías del original a las filas añadidas.
 s.getRange(`G3:G${end}`).conditionalFormats.clear();
 for(const [status,fill,color] of [['No ejecutado','#E7F3FF','#1F4E78'],['Exitoso','#E2F0D9','#375623'],['Fallido','#FCE4D6','#9C0006'],['Bloqueado','#FFF2CC','#7F6000'],['No aplica','#E7E6E6','#595959']])
   s.getRange(`G3:G${end}`).conditionalFormats.add('cellIs',{operator:'equal',formula:`"${status}"`,format:{fill,font:{color}}});
 s.freezePanes.freezeRows(2);
 wb.recalculate();
 console.log((await wb.inspect({kind:'table',range:`'Casos de prueba'!B${end-2}:I${end}`,include:'values,formulas',tableMaxRows:3,tableMaxCols:8,maxChars:1800})).ndjson);
 console.log((await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!',options:{useRegex:true,maxResults:10},maxChars:1000})).ndjson);
 await (await SpreadsheetFile.exportXlsx(wb)).save(base+'TEST_CASES_PROYECTO_REVISADOS.xlsx');
 for(const [name,range] of [['revisado-inicio','A1:I5'],['revisado-supervisor','B100:F102'],['revisado-final',`B${end-1}:I${end}`]]) {
  const p=await wb.render({sheetName:s.name,range,scale:1,format:'png'});
  await fs.writeFile(base+name+'.png',new Uint8Array(await p.arrayBuffer()));
 }
 console.log(JSON.stringify({file:base+'TEST_CASES_PROYECTO_REVISADOS.xlsx',cases:rows.length,lastRow:end}));
 process.exit(0);
}
if(process.argv.includes('--help-filter')) {
 console.log(wb.help('*',{search:'autoFilter|AutoFilter',include:'index,examples,notes',maxChars:2500}).ndjson);
 console.log('tables',wb.worksheets.getItemAt(0).tables.items.map(t=>({name:t.name})));
 process.exit(0);
}
console.log((await wb.inspect({kind:'workbook,sheet,table',maxChars:3500,tableMaxRows:3,tableMaxCols:9})).ndjson);
console.log((await wb.inspect({kind:'computedStyle',range:"'Casos de prueba'!A2:I4",maxChars:4000})).ndjson);
const p=await wb.render({sheetName:'Casos de prueba',range:'A1:I5',scale:1,format:'png'});
await fs.writeFile(base+'original.png',new Uint8Array(await p.arrayBuffer()));
