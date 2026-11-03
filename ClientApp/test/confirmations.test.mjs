import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';

const source = await readFile(new URL('../../wwwroot/js/site.js', import.meta.url), 'utf8');
function submit({ allowed, formPrompt, buttonPrompt }) {
  let callback, prevented = false; const prompts = [];
  vm.runInNewContext(source, { document: { addEventListener(type, handler) { assert.equal(type, 'submit'); callback = handler; } }, window: { confirm(prompt) { prompts.push(prompt); return allowed; } } });
  callback({ target: { dataset: { confirm: formPrompt } }, submitter: buttonPrompt ? { dataset: { confirm: buttonPrompt } } : null, preventDefault() { prevented = true; } });
  return { prevented, prompts };
}
test('Cancelar una confirmación impide enviar el formulario de retirada', () => {
  assert.deepEqual(submit({ allowed: false, formPrompt: '¿Quitar el archivo?' }), { prevented: true, prompts: ['¿Quitar el archivo?'] });
});
test('El botón de rechazo muestra su confirmación y respetar aceptar permite el envío', () => {
  assert.deepEqual(submit({ allowed: true, formPrompt: 'Otro mensaje', buttonPrompt: '¿Rechazo definitivo?' }), { prevented: false, prompts: ['¿Rechazo definitivo?'] });
});
test('Las acciones sin advertencia no abren una confirmación artificial', () => {
  assert.deepEqual(submit({ allowed: false }), { prevented: false, prompts: [] });
});
