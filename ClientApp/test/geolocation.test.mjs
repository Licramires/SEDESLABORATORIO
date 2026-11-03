import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';

// Pruebas unitarias del código publicado: el proveedor GPS es sintético; no es una
// comprobación del permiso del sistema ni de la ubicación física del usuario.
const index = await readFile(new URL('../../publico/Views/Publico/Index.cshtml', import.meta.url), 'utf8');
const route = await readFile(new URL('../../publico/Views/Publico/Ruta.cshtml', import.meta.url), 'utf8');
function fixture(source, labs, geolocation) {
  const button = { addEventListener: (_, callback) => { button.click = () => callback({ preventDefault() {} }); } };
  const message = { textContent: '' }; const navigated = [];
  const map = { dataset: { laboratories: JSON.stringify(labs) } };
  const elements = { 'public-map': map, 'nearest-lab': button, 'nearest-message': message, 'route-link': button, 'route-message': message };
  const script = [...source.matchAll(/<script\b[^>]*>([\s\S]*?)<\/script>/g)].map(m => m[1]).find(s => s.includes('getCurrentPosition'));
  vm.runInNewContext(script.replaceAll('@ViewData["Destination"]', '-17.39,-66.15'), { document: { getElementById: id => elements[id] }, navigator: { geolocation }, window: { location: { assign: url => navigated.push(url) } } });
  return { button, message, navigated };
}
const labs = [{ Id: 1, Lat: -17.39, Lng: -66.15, EstaAbierto: true }, { Id: 2, Lat: -17.4, Lng: -66.16, EstaAbierto: false }, { Id: 3, Lat: -18, Lng: -67, EstaAbierto: true }];
test('Sin laboratorios abiertos no se solicita acceso GPS', () => {
  const f = fixture(index, labs.map(l => ({ ...l, EstaAbierto: false })), { getCurrentPosition() { throw Error('No debe pedir GPS'); } });
  f.button.click(); assert.match(f.message.textContent, /No hay laboratorios abiertos/); assert.equal(f.navigated.length, 0);
});
for (const [name, source] of [['mapa', index], ['ruta', route]]) {
  test(`GPS no soportado en ${name}: mensaje y ausencia de navegación`, () => {
    const f = fixture(source, labs, undefined); f.button.click(); assert.match(f.message.textContent, /no permite/); assert.equal(f.navigated.length, 0);
  });
  for (const code of [1, 2, 3]) test(`GPS ${name}: denegado/no disponible/tiempo agotado, código ${code}`, () => {
    const f = fixture(source, labs, { getCurrentPosition(_, fail) { fail({ code }); } }); f.button.click();
    assert.ok(f.message.textContent.length > 0); assert.equal(f.navigated.length, 0);
  });
}
test('La ruta calculada usa el abierto más cercano y conserva las coordenadas sintéticas', () => {
  const f = fixture(index, labs, { getCurrentPosition(done) { done({ coords: { latitude: -17.4, longitude: -66.16 } }); } }); f.button.click();
  const url = new URL(f.navigated[0]); assert.equal(url.hostname, 'www.google.com');
  assert.equal(url.searchParams.get('origin'), '-17.4,-66.16'); assert.equal(url.searchParams.get('destination'), '-17.39,-66.15');
});
test('El botón de detalle genera la ruta al destino, con espera limitada del GPS', () => {
  const f = fixture(route, [], { getCurrentPosition(done, _, options) { assert.equal(options.timeout, 10000); done({ coords: { latitude: -17.4, longitude: -66.16 } }); } });
  f.button.click(); assert.equal(new URL(f.navigated[0]).searchParams.get('destination'), '-17.39,-66.15');
});
