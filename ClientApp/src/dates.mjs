// Fechas civiles de Bolivia recibidas del servidor. No dependen de la zona del navegador.
export const dayKey = d => d.toISOString().slice(0, 10);
export const parseDay = text => new Date(text.slice(0, 10) + 'T00:00:00Z');
export const MIN_DAY = '0001-01-01', MAX_DAY = '9999-12-31';
export const isCivilDay = text => /^\d{4}-\d{2}-\d{2}$/.test(text) && text >= MIN_DAY && text <= MAX_DAY && Number.isFinite(+parseDay(text)) && dayKey(parseDay(text)) === text;
export function addDays(text, n) {
  const d = parseDay(text); d.setUTCDate(d.getUTCDate() + n);
  return dayKey(new Date(Math.max(+parseDay(MIN_DAY), Math.min(+parseDay(MAX_DAY), +d))));
}
export function weekDays(text) {
  const d = parseDay(text); const monday = addDays(text, -((d.getUTCDay() + 6) % 7));
  return [...new Set(Array.from({ length: 7 }, (_, i) => addDays(monday, i)))];
}
export const labelDay = (text, options = { day: '2-digit', month: 'short' }) => new Intl.DateTimeFormat('es-BO', { ...options, timeZone: 'UTC' }).format(parseDay(text));
export function visitsOnDay(visits, day) {
  const end = day === MAX_DAY ? '9999-12-31T23:59:59.9999999' : addDays(day, 1) + 'T00:00';
  return visits.filter(v => v.inicio && v.inicio < end && v.fin > day + 'T00:00').sort((a, b) => a.inicio.localeCompare(b.inicio) || a.id - b.id);
}
export function routeBatches(visits, day) {
  const ordered = visits.filter(v => v.inicio?.slice(0, 10) === day && Number.isFinite(v.lat) && Number.isFinite(v.lng) && Math.abs(v.lat) <= 90 && Math.abs(v.lng) <= 180)
    .sort((a, b) => a.inicio.localeCompare(b.inicio) || a.id - b.id);
  const chunks = [];
  // Maps admite tres puntos intermedios en navegador móvil, más el destino.
  for (let i = 0; i < ordered.length; i += 4) {
    const batch = ordered.slice(i, i + 4); const coords = batch.map(v => `${v.lat},${v.lng}`);
    chunks.push({ visits: batch, url: 'https://www.google.com/maps/dir/?api=1&destination=' + encodeURIComponent(coords.at(-1)) + (coords.length > 1 ? '&waypoints=' + encodeURIComponent(coords.slice(0, -1).join('|')) : '') });
  }
  return chunks;
}
