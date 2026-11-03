import React, { Component, useEffect, useRef, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { addDays, weekDays, labelDay, visitsOnDay, routeBatches, isCivilDay, MIN_DAY, MAX_DAY } from './dates.mjs';
import { requestJson } from './request-json.mjs';

const number = new Intl.NumberFormat('es-BO', { maximumFractionDigits: 1 });
const format = value => value == null ? 'Sin datos' : number.format(value);
function Empty({ children }) { return <p className="ops-empty">{children}</p>; }
function Icon({ name }) { return <svg className="ui-icon" aria-hidden="true"><use href={'/images/ui-icons.svg#' + name} /></svg>; }
class Boundary extends Component {
  state = { failed: false };
  static getDerivedStateFromError() { return { failed: true }; }
  render() { return this.state.failed ? <div role="alert" className="alert alert-warning">No se pudo mostrar la vista interactiva. Puede usar los datos y enlaces que aparecen debajo o recargar la página.</div> : this.props.children; }
}
function Visit({ visit, now, showDate = false }) {
  const started = visit.inicio && visit.inicio <= now;
  return <article className="agenda-visit">
    <span className="ops-caption">#{visit.solicitudId} · {visit.renovacion ? 'Renovación' : 'Apertura'}</span>
    <h3>{visit.nombre || 'Laboratorio sin nombre'}</h3><p>{visit.municipio}{visit.direccion ? ' · ' + visit.direccion : ''}</p>
    {visit.inicio && <p className="agenda-time">{showDate && labelDay(visit.inicio) + ' · '}{visit.inicio.slice(11)}–{visit.fin.slice(11)}{visit.fin.slice(0, 10) !== visit.inicio.slice(0, 10) && ' (+1 día)'}</p>}
    {visit.actaPreparada && <span className="silab-status">Borrador de acta guardado</span>}
    <a className="btn silab-btn-outline" href={'/Gestion/Detalle/' + visit.solicitudId + (visit.inicio ? '#acta' : '#programacion')}>
      {!visit.inicio ? 'Programar visita' : started ? 'Abrir visita y acta' : 'Ver visita'}</a>
  </article>;
}
function Agenda({ data }) {
  const [selected, select] = useState(data.fecha); const [search, setSearch] = useState(new URLSearchParams(location.search).get('q') ?? '');
  const refresh = () => { const url = new URL(location.href); url.searchParams.set('fecha', selected); url.searchParams.set('q', search); location.assign(url); };
  const weekRef = useRef(null);
  useEffect(() => {
    const region = weekRef.current, day = region?.querySelector('.is-selected');
    if (!region || !day || region.scrollWidth <= region.clientWidth) return;
    const left = day.getBoundingClientRect().left - region.getBoundingClientRect().left + region.scrollLeft;
    if (left < region.scrollLeft) region.scrollTo({ left });
    else if (left + day.offsetWidth > region.scrollLeft + region.clientWidth) region.scrollTo({ left: left + day.offsetWidth - region.clientWidth });
  }, [selected, data.soloRutas]);
  const days = weekDays(selected); const match = v => (v.nombre + ' ' + v.municipio).toLocaleLowerCase('es').includes(search.trim().toLocaleLowerCase('es'));
  const visits = data.visitas.filter(match); const unplanned = visits.filter(v => !v.inicio);
  const batches = routeBatches(visits, selected);
  return <>
    <div className="ops-toolbar silab-panel p-3"><label>Buscar establecimiento o municipio<input className="form-control" value={search} onChange={e => setSearch(e.target.value)} type="search" /></label>
    <label>Fecha de referencia<input className="form-control" type="date" min={MIN_DAY} max={MAX_DAY} value={selected} onChange={e => { if (isCivilDay(e.target.value)) select(e.target.value); }} /></label>
    <button type="button" className="btn silab-btn-outline" onClick={() => select(data.hoy)}>Ir a hoy</button><button type="button" className="btn silab-btn-outline" onClick={refresh}>Actualizar agenda</button></div>
    <p className="text-muted">{visits.length} inspecciones pendientes con esta búsqueda · {unplanned.length} por programar · {visits.length - unplanned.length} con horario</p>
    <div className="ops-week-heading"><button type="button" className="btn silab-btn-outline" aria-label="Semana anterior" disabled={selected === MIN_DAY} onClick={() => select(addDays(selected, -7))}><Icon name="arrow" />Anterior</button>
      <h2 aria-live="polite">{labelDay(days[0])} – {labelDay(days.at(-1), { day: '2-digit', month: 'short', year: 'numeric' })}</h2><button type="button" className="btn silab-btn-outline" aria-label="Semana siguiente" disabled={selected === MAX_DAY} onClick={() => select(addDays(selected, 7))}>Siguiente</button></div>
    <div className="ops-day-selector" aria-label="Seleccionar día">{days.map(day => <button key={day} aria-pressed={day === selected} onClick={() => select(day)} className="btn silab-btn-outline">{labelDay(day, { weekday: 'short', day: '2-digit' })}</button>)}</div>
    {data.soloRutas ? <section className="silab-panel p-4"><h2>Recorrido del {labelDay(selected, { day: '2-digit', month: 'long' })}</h2><p>Visitas que comienzan este día, en orden de horario. Maps calcula el recorrido desde su ubicación cuando lo abre.</p>
      {!batches.length ? <Empty>No hay visitas programadas con una ubicación válida para este día. Elija otra fecha o complete los datos del expediente.</Empty> : batches.map((batch, i) => <div key={i} className="route-batch"><ol>{batch.visits.map(v => <li key={v.id}><strong>{v.inicio.slice(11)} · {v.nombre}</strong><p>{v.municipio} · {v.direccion || 'Dirección no registrada'}</p><a href={'/Gestion/Detalle/' + v.solicitudId}>Ver visita</a></li>)}</ol><a className="btn silab-btn-primary" target="_blank" rel="noopener noreferrer" href={batch.url}>Abrir ruta ordenada ({batch.visits.length} {batch.visits.length === 1 ? "visita" : "visitas"})</a></div>)}
      {visits.some(v => v.inicio?.slice(0, 10) === selected && (v.lat == null || v.lng == null)) && <p className="text-muted">Se excluyeron del recorrido las visitas cuya ubicación necesita validación.</p>}
    </section> : <div className="supervisor-agenda-layout">
      <section className="silab-panel p-4 agenda-pending"><h2>Por programar <span className="silab-status">{unplanned.length}</span></h2>{unplanned.length > 0 && <p>Estas asignaciones aún no tienen fecha. Abra una para elegir el horario.</p>}{unplanned.length ? unplanned.map(v => <Visit key={v.id} visit={v} now={data.ahora} />) : <Empty>Sin visitas por programar.</Empty>}
      {visits.some(v => v.inicio && v.inicio < days[0] + 'T00:00' && v.inicio < data.ahora) && <><h2 className="mt-4">Visitas anteriores sin acta enviada</h2><p>Estas visitas ya comenzaron y quedan fuera de la semana mostrada.</p>{visits.filter(v => v.inicio && v.inicio < days[0] + 'T00:00' && v.inicio < data.ahora).map(v => <Visit key={v.id} visit={v} now={data.ahora} showDate />)}</>}
      </section>
      <section className="silab-panel p-3 agenda-week"><h2>Visitas de la semana</h2><p>Horarios de Bolivia (UTC−4). En móvil se muestra el día seleccionado.</p><p><a href={'/Gestion/Rutas?fecha=' + selected}>Ver ruta del día seleccionado</a></p><div className="agenda-days" ref={weekRef} tabIndex="0" role="region" aria-label="Agenda semanal">
        {days.map(day => { const scheduled = visitsOnDay(visits, day); return <section key={day} className={'agenda-day ' + (day === selected ? 'is-selected' : '')}><h3>{labelDay(day, { weekday: 'short', day: '2-digit' })}{day === data.hoy && <span className="ops-caption">Hoy</span>}</h3>{scheduled.length ? scheduled.map(v => <Visit key={v.id} visit={v} now={data.ahora} />) : <p className="ops-caption">Sin visitas</p>}</section>; })}
      </div></section>
    </div>}
    <p className="text-muted mt-3">Las nuevas asignaciones y los cambios guardados aparecen al actualizar la agenda. Solo se muestran inspecciones pendientes asignadas a usted.</p>
  </>;
}
function Bars({ rows, total, value = 'cantidad', name = 'nombre' }) {
  const max = Math.max(1, ...rows.map(r => r[value]));
  return rows.length ? <ul className="metric-bars">{rows.map((r, i) => <li key={r[name] + i}><div><span>{r[name] || 'Sin municipio'}</span><strong>{format(r[value])}{total > 0 && <small> · {format(r[value] / total * 100)}%</small>}</strong></div><div className="metric-bar-track"><span style={{ width: (r[value] / max * 100) + '%' }} /></div></li>)}</ul> : <Empty>No hay registros para este filtro.</Empty>;
}
function Trend({ rows }) {
  const [mode, setMode] = useState('ambas'); const [selected, select] = useState(rows.at(-1)?.mes ?? '');
  const width = 640, height = 240, left = 44, bottom = 205, top = 20;
  const max = Math.max(1, ...rows.flatMap(r => [r.aperturas, r.renovaciones]));
  const x = i => rows.length === 1 ? 340 : left + i * (width - left - 18) / (rows.length - 1); const y = v => bottom - v / max * (bottom - top);
  const chosen = rows.find(r => r.mes === selected) || rows.at(-1);
  const tickIndices = [...new Set([0, Math.floor((rows.length - 1) / 2), rows.length - 1])];
  return <><div className="ops-toolbar"><label>Series<select className="form-select" value={mode} onChange={e => setMode(e.target.value)}><option value="ambas">Aperturas y renovaciones</option><option value="aperturas">Solo aperturas</option><option value="renovaciones">Solo renovaciones</option></select></label><label>Consultar mes<select className="form-select" value={selected} onChange={e => select(e.target.value)}>{rows.map(r => <option key={r.mes} value={r.mes}>{labelDay(r.mes + '-01', { month: 'short', year: 'numeric' })}</option>)}</select></label></div>
    <svg viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Evolución mensual de trámites recibidos" className="metric-trend"><title>Aperturas y renovaciones recibidas por mes</title><desc>Los valores completos están disponibles al seleccionar un mes y en las tablas de datos.</desc>
    {[...new Set([0, Math.ceil(max / 2), max])].map(n => <g key={n}><line x1={left} x2={width - 15} y1={y(n)} y2={y(n)} className="chart-grid" /><text x={left - 8} y={y(n) + 4} textAnchor="end">{format(n)}</text></g>)}
    {['aperturas', 'renovaciones'].filter(key => mode === 'ambas' || key === mode).map(key => <g key={key} className={'chart-series ' + key}><polyline points={rows.map((r, i) => `${x(i)},${y(r[key])}`).join(' ')} fill="none" strokeWidth="3" strokeDasharray={key === 'renovaciones' ? '7 4' : undefined} />{rows.map((r, i) => <circle key={r.mes} cx={x(i)} cy={y(r[key])} r={r.mes === selected ? 5 : 3}><title>{r.mes}: {r[key]} {key}</title></circle>)}</g>)}
    {tickIndices.filter(i => i >= 0).map(i => <text key={i} x={x(i)} y={height - 10} textAnchor="middle">{labelDay(rows[i].mes + '-01', { month: 'short', year: '2-digit' })}</text>)}
    </svg><div className="chart-legend"><span><i className="legend-aperturas" />Aperturas</span><span><i className="legend-renovaciones" />Renovaciones (línea discontinua)</span></div>
    {chosen && <p className="metric-selected" aria-live="polite">{labelDay(chosen.mes + '-01', { month: 'long', year: 'numeric' })}: <strong>{chosen.aperturas} aperturas · {chosen.renovaciones} renovaciones</strong></p>}
  </>;
}
function Donut({ data }) {
  const values = [{ nombre: 'Habilitados', cantidad: data.habilitadas }, { nombre: 'Pendientes', cantidad: data.pendientes }, { nombre: 'Rechazos definitivos', cantidad: data.rechazadas }];
  const circumference = 2 * Math.PI * 43; let offset = 0;
  return <div className="metric-donut-layout"><svg viewBox="0 0 120 120" role="img" aria-label={`Estado actual: ${data.habilitadas} habilitados, ${data.pendientes} pendientes y ${data.rechazadas} rechazos definitivos`}><circle cx="60" cy="60" r="43" className="donut-base" fill="none" strokeWidth="14" />
    {data.total > 0 && values.map((v, i) => { const length = v.cantidad / data.total * circumference; const start = offset; offset += length; return <circle key={v.nombre} cx="60" cy="60" r="43" fill="none" strokeWidth="14" strokeDasharray={`${length} ${circumference - length}`} strokeDashoffset={-start} transform="rotate(-90 60 60)" className={'donut-part part-' + i} />; })}
    <text x="60" y="59" textAnchor="middle" className="donut-total">{data.total}</text><text x="60" y="73" textAnchor="middle">Recibidos</text></svg>
    <ul className="donut-legend">{values.map((v, i) => <li key={v.nombre}><i className={'part-' + i} /><span>{v.nombre}</span><strong>{v.cantidad} <small>({data.total ? format(v.cantidad / data.total * 100) : '0'}%)</small></strong></li>)}</ul></div>;
}
function Metrics({ data: initial }) {
  const [data, setData] = useState(initial), [loading, setLoading] = useState(false), [error, setError] = useState('');
  async function refresh() {
    setLoading(true); setError('');
    try { const params = new URLSearchParams({ desde: data.desde, hasta: data.hasta, municipio: data.municipio ?? '' });
      setData(await requestJson('/Metricas/Datos?' + params));
    } catch { setError('No se pudieron actualizar los gráficos. Se mantienen los últimos datos mostrados; compruebe su sesión y vuelva a consultar.'); }
    finally { setLoading(false); }
  }
  return <><div className="ops-toolbar metric-update"><span>Gráficos calculados con los registros guardados · Actualización: {new Intl.DateTimeFormat('es-BO', { timeZone: 'America/La_Paz', dateStyle: 'short', timeStyle: 'short' }).format(new Date(data.actualizadoUtc.endsWith('Z') ? data.actualizadoUtc : data.actualizadoUtc + 'Z'))} (Bolivia)</span><button className="btn silab-btn-outline" disabled={loading} onClick={refresh}>{loading ? 'Actualizando…' : 'Actualizar gráficos'}</button></div>{error && <p role="alert" className="alert alert-warning">{error}</p>}
    <div aria-busy={loading}>
    <dl className="metric-summary silab-panel"><div><dt>Trámites recibidos</dt><dd>{data.total}</dd><small>{data.borradores} borradores por separado</small></div><div><dt>Tasa de aprobación</dt><dd>{data.tasaAprobacion == null ? 'Sin decisiones' : format(data.tasaAprobacion) + '%'}</dd><small>Sobre habilitados y rechazos definitivos</small></div><div><dt>Tiempo promedio de cierre</dt><dd>{data.promedioDias == null ? 'Sin cierres' : format(data.promedioDias) + ' días'}</dd><small>{data.cierresConFecha} cierres con fechas válidas</small></div></dl>
    {data.total === 0 && <p className="alert alert-info">No hay trámites recibidos en este período con el municipio seleccionado. Pruebe otro rango o quite el filtro.</p>}
    <div className="metrics-chart-grid">
      <section className="silab-panel p-4"><h2>Trámites recibidos por mes</h2><p>Aperturas y renovaciones por fecha de primer envío.</p><Trend rows={data.meses} /></section>
      <section className="silab-panel p-4"><h2>Estado actual de los trámites</h2><p>Resultados actuales de los trámites recibidos en el período.</p><Donut data={data} /></section>
      <section className="silab-panel p-4"><h2>Trámites por municipio</h2><Bars rows={data.municipios} total={data.total} /></section>
      <section className="silab-panel p-4"><h2>Tipo de laboratorio solicitado</h2><Bars rows={data.tipos} total={data.total} /></section>
      <section className="silab-panel p-4"><h2>Actas registradas por supervisor</h2><p>Visitas de los trámites del filtro; no incluye reservas sustituidas. No mide la calidad de las inspecciones.</p><Bars rows={data.supervisores} value="completadas" /><details><summary>Ver asignadas y pendientes</summary><ul>{data.supervisores.map((s, i) => <li key={s.nombre + i}>{s.nombre}: {s.asignadas} asignadas · {s.pendientes} pendientes</li>)}</ul></details></section>
      <section className="silab-panel p-4"><h2>Puntos que requieren atención</h2><p>Situación actual dentro de los trámites del filtro.</p><ul className="metric-alerts">{data.alertas.map(a => <li key={a.nombre}><strong>{a.cantidad}</strong><div><span>{a.nombre}</span><small>{a.explicacion}</small></div></li>)}</ul></section>
    </div>{data.fechasReconstruidas > 0 && <p className="text-muted mt-3">{data.fechasReconstruidas} trámites sin primer envío registrado usan la fecha de envío disponible o, en último caso, la creación.</p>}
    </div><p className="text-muted mt-3">Para actualizar también las tablas y el informe CSV, pulse «Consultar período». El informe y los gráficos usan los mismos filtros y registros guardados.</p>
  </>;
}
function ActaEditor({ data }) {
  const [checks, setChecks] = useState(data.checks);
  const [decision, setDecision] = useState(data.resultado === 'rechazado' ? 'rechazado' : !data.checks.length && data.resultado === 'aprobado' ? 'aprobado' : 'observado');
  const failed = checks.some(c => c.resultado === 'observado');
  const incomplete = checks.some(c => !c.resultado);
  const allNA = checks.length > 0 && checks.every(c => c.resultado === 'no_aplica');
  const result = checks.length === 0 ? decision : failed ? decision : 'aprobado';
  function update(index, key, value) { setChecks(old => old.map((c, i) => i === index ? { ...c, [key]: value } : c)); }
  return <>
    <p>Marque cada punto. Los incumplimientos y sus correcciones forman el resumen del acta; no necesita escribirlos otra vez.</p>
    {checks.map((c, n) => <fieldset className="inspection-check" key={c.id}><legend>{c.nombre}</legend><p>{c.referencia} · v{c.version}</p>
      <input type="hidden" name={`Verificaciones[${n}].Id`} value={c.id} />
      <label>¿Cumple este punto?<select className="form-select" name={`Verificaciones[${n}].Resultado`} value={c.resultado} required onChange={e => update(n, 'resultado', e.target.value)}><option value="">Seleccione</option><option value="cumple">Cumple</option><option value="observado">No cumple</option><option value="no_aplica">No aplica</option></select></label>
      {c.resultado === 'observado' && <label>Qué debe corregirse<textarea className="form-control" name={`Verificaciones[${n}].Observacion`} value={c.observacion} required maxLength="2000" onChange={e => update(n, 'observacion', e.target.value)} /></label>}
    </fieldset>)}
    <label>Notas adicionales {checks.length ? '(opcional)' : '(describa los hallazgos)'}<textarea className="form-control" name="Motivo" maxLength="4000" required={!checks.length} defaultValue={data.notas} /></label>
    {(failed || !checks.length) ? <label>Dictamen de los incumplimientos<select className="form-select" name="Resultado" value={decision} onChange={e => setDecision(e.target.value)}>{!checks.length && <option value="aprobado">Aprobado</option>}<option value="observado">Con observaciones: admite subsanación</option><option value="rechazado">Rechazado por incumplimiento de campo</option></select></label> : <><input type="hidden" name="Resultado" value={result} /><p role="status"><strong>Dictamen: {incomplete ? 'complete las verificaciones' : allNA ? 'debe verificar al menos un punto aplicable' : 'aprobado'}</strong></p></>}
    {result === 'observado' && <label>Plazo para subsanar (días hábiles)<input type="number" className="form-control" name="DiasSubsanacion" min="1" max="90" defaultValue={data.dias} required /></label>}
    <label>PDF del acta firmada (necesario al enviar)<input type="file" name="Pdf" accept=".pdf" className="form-control" /></label>
    <div className="workflow-actions"><button type="submit" formAction="/Gestion/PrepararActa" className="btn silab-btn-outline">Guardar contenido para firma</button><button className="btn silab-btn-primary" disabled={incomplete || allNA}>Enviar acta firmada al coordinador</button></div>
  </>;
}
for (const root of document.querySelectorAll('[data-operations]')) {
  try { const data = JSON.parse(document.getElementById(root.dataset.source).textContent); createRoot(root).render(<Boundary>{root.dataset.operations === 'agenda' ? <Agenda data={data} /> : root.dataset.operations === 'acta' ? <ActaEditor data={data} /> : <Metrics data={data} />}</Boundary>); }
  catch { root.textContent = 'La vista interactiva no está disponible. Use los datos y enlaces que aparecen debajo.'; }
}
