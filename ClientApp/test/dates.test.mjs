import { test } from 'node:test';
import assert from 'node:assert/strict';
import { weekDays, addDays, visitsOnDay, routeBatches, isCivilDay } from '../src/dates.mjs';

test('Los extremos del calendario no crean años de cinco cifras ni días repetidos', () => {
  assert.equal(addDays('0001-01-01', -7), '0001-01-01');
  assert.equal(addDays('9999-12-31', 7), '9999-12-31');
  assert.deepEqual(weekDays('9999-12-31'), ['9999-12-27','9999-12-28','9999-12-29','9999-12-30','9999-12-31']);
  assert.equal(visitsOnDay([{ id: 1, inicio: '9999-12-31T12:00', fin: '9999-12-31T13:00' }], '9999-12-31').length, 1);
});
test('La fecha elegida debe ser civil, completa y existente', () => {
  for (const day of ['0001-01-01','9999-12-31','2028-02-29']) assert.equal(isCivilDay(day), true, day);
  for (const day of ['', '0000-01-01','10000-01-01','2026-02-29','2026-13-01','2026-1-1']) assert.equal(isCivilDay(day), false, day);
});

test('La semana incluye domingo y cruces de año sin depender de la zona local', () => {
  assert.deepEqual(weekDays('2026-10-18'), ['2026-10-12','2026-10-13','2026-10-14','2026-10-15','2026-10-16','2026-10-17','2026-10-18']);
  assert.equal(weekDays('2027-01-01')[0], '2026-12-28');
  assert.equal(addDays('2028-02-28', 1), '2028-02-29');
});
test('Una visita nocturna aparece en ambos días, sin duplicarse al terminar a medianoche', () => {
  const visit = { id: 1, inicio: '2026-10-21T23:00', fin: '2026-10-22T01:00' };
  assert.equal(visitsOnDay([visit], '2026-10-21').length, 1); assert.equal(visitsOnDay([visit], '2026-10-22').length, 1);
  assert.equal(visitsOnDay([{ ...visit, fin: '2026-10-22T00:00' }], '2026-10-22').length, 0);
});
test('La ruta excluye ubicaciones desconocidas, inválidas y visitas sin fecha', () => {
  const valid = { id: 1, inicio: '2026-10-21T10:00', lat: -17.3, lng: -66.1 };
  const routes = routeBatches([valid, { ...valid, lat: null }, { ...valid, lng: 181 }, { ...valid, inicio: null }], '2026-10-21');
  assert.equal(routes.length, 1); assert.deepEqual(routes[0].visits, [valid]);
  assert.ok(routes[0].url.includes('destination=-17.3%2C-66.1'));
});
test('Una ruta larga conserva orden y todas las paradas con el límite móvil de Maps', () => {
  const visits = Array.from({ length: 20 }, (_, id) => ({ id, inicio: `2026-10-21T${String(id).padStart(2,'0')}:00`, lat: -17, lng: -66 })).reverse();
  const batches = routeBatches(visits, '2026-10-21');
  assert.deepEqual(batches.map(b => b.visits.length), [4, 4, 4, 4, 4]);
  assert.deepEqual(batches.flatMap(b => b.visits.map(v => v.id)), Array.from({ length: 20 }, (_, i) => i));
});
