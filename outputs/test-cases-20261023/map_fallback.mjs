import {readFile,writeFile} from 'node:fs/promises';
import vm from 'node:vm';
import assert from 'node:assert/strict';
const rows=[];
for(const [name,file,id] of [['directorio','Index.cshtml','public-map'],['ficha','Detalle.cshtml','detail-map']]){
 const source=await readFile(new URL('../../publico/Views/Publico/'+file,import.meta.url),'utf8');
 const scripts=[...source.matchAll(/<script\b[^>]*>([\s\S]*?)<\/script>/g)].map(m=>m[1]);
 const script=scripts.find(x=>x.includes("typeof L"));
 assert.ok(script,'Bloque de mapa publicado '+file);
 const element={dataset:{laboratories:'[]',coordinates:'[-17.39,-66.15]'},textContent:''};
 const button={addEventListener(){}};
 vm.runInNewContext(script,{document:{getElementById:key=>key===id?element:button},navigator:{},window:{}});
 assert.match(element.textContent,/Mapa no disponible/i);
 rows.push({page:name,script:file,passed:true,message:element.textContent});
}
await writeFile(new URL('PUB-EXT-08-fallback.json',import.meta.url),JSON.stringify({method:'Código JavaScript publicado ejecutado en VM sin proveedor Leaflet; no bloqueo de CDN desde navegador',checks:rows},null,2));
console.log(JSON.stringify(rows));
