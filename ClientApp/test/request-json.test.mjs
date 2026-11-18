import { test } from 'node:test';
import assert from 'node:assert/strict';
import { requestJson } from '../src/request-json.mjs';

test('La consulta usa la sesión actual y no devuelve datos hasta recibir JSON completo', async () => {
  const value = await requestJson('/Metricas/Datos', async (url, options) => {
    assert.equal(url, '/Metricas/Datos'); assert.equal(options.credentials, 'same-origin'); assert.equal(options.cache, 'no-store');
    return new Response(JSON.stringify({ total: 7 }), { headers: { 'Content-Type': 'application/json' } });
  });
  assert.deepEqual(value, { total: 7 });
});
for (const status of [401, 403, 409, 500, 503]) test(`Una respuesta ${status} no reemplaza los datos por una respuesta de error`, async () => {
  await assert.rejects(requestJson('/Metricas/Datos', async () => new Response('{"error":"no disponible"}', { status, headers: { 'Content-Type': 'application/json' } })));
});
test('Una red caída y una sesión redirigida a HTML se rechazan de forma recuperable', async () => {
  await assert.rejects(requestJson('/Metricas/Datos', async () => { throw new TypeError('Failed to fetch'); }));
  await assert.rejects(requestJson('/Metricas/Datos', async () => new Response('<html>Iniciar sesión</html>', { headers: { 'Content-Type': 'text/html' } })));
  await assert.rejects(requestJson('/Metricas/Datos', async () => new Response('{incompleto', { headers: { 'Content-Type': 'application/json' } })));
});
test('Una red sin respuesta se cancela y libera la actualización para poder reintentar', async () => {
  await assert.rejects(requestJson('/Metricas/Datos', (_, options) => new Promise((resolve, reject) => {
    options.signal.addEventListener('abort', () => reject(new DOMException('Timeout', 'AbortError')), { once: true });
  }), 10), { name: 'AbortError' });
  assert.deepEqual(await requestJson('/Metricas/Datos', async () => new Response('{"total":8}', { headers: { 'Content-Type': 'application/json' } })), { total: 8 });
});
