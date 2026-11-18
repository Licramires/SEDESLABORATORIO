using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Application.Metricas;

public interface IConsultaMetricas
{
    Task<MetricasModel> Consultar(DateTime desde, DateTime hasta, string? municipio);
}

public sealed class ConsultaMetricas(IMetricasDatos db, IReloj reloj) : IConsultaMetricas
{
    public async Task<MetricasModel> Consultar(DateTime start, DateTime end, string? municipio)
    {
        if (end < start || end >= DateTime.MaxValue.Date || (end - start).TotalDays > 3660)
            throw new ArgumentException("El rango debe ser válido y no superar 10 años.");
        var exclusiveEnd = end.AddDays(1);
        var query = db.Solicitudes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(municipio)) query = query.Where(s => s.Municipio == municipio.Trim());
        var borradores = await query.CountAsync(s => s.Estado == EstadoSolicitud.Borrador && s.FechaCreacion >= start && s.FechaCreacion < exclusiveEnd);
        var recibidas = query.Where(s => s.Estado != EstadoSolicitud.Borrador && (s.PrimerEnvio ?? s.FechaEnvio ?? s.FechaCreacion) >= start && (s.PrimerEnvio ?? s.FechaEnvio ?? s.FechaCreacion) < exclusiveEnd);
        var solicitudes = await recibidas.ToListAsync();
        var rows = solicitudes.GroupBy(s => s.Municipio).Select(g => new MetricaMunicipio
        {
            Municipio = g.Key, Solicitudes = g.Count(), Habilitadas = g.Count(s => s.Estado == EstadoSolicitud.Habilitado),
            Rechazadas = g.Count(s => s.Estado == EstadoSolicitud.Rechazado && s.MotivoRechazo != "campo"),
            Pendientes = g.Count(s => s.Estado != EstadoSolicitud.Habilitado && (s.Estado != EstadoSolicitud.Rechazado || s.MotivoRechazo == "campo")),
            Aperturas = g.Count(s => !s.EsRenovacion), Renovaciones = g.Count(s => s.EsRenovacion),
            PromedioDias = g.Where(s => s.FechaCierre.HasValue && s.FechaCierre >= (s.PrimerEnvio ?? s.FechaEnvio ?? s.FechaCreacion))
                .Select(s => (double?)(s.FechaCierre!.Value - (s.PrimerEnvio ?? s.FechaEnvio ?? s.FechaCreacion)).TotalDays).Average()
        }).OrderBy(r => r.Municipio).ToList();
        var ids = recibidas.Select(s => s.Id);
        var inspecciones = await db.Inspecciones.AsNoTracking().Include(i => i.Supervisor).Where(i => ids.Contains(i.SolicitudId) && i.Estado != "reprogramada").ToListAsync();
        var supervisores = inspecciones.GroupBy(i => new { i.SupervisorId, i.Supervisor.Nombre }).Select(g => new RendimientoSupervisor {
            Nombre = g.Key.Nombre, Asignadas = g.Count(), Completadas = g.Count(i => i.Estado is "registrada" or "validada" or "rechazada"), Pendientes = g.Count(i => i.Estado == "pendiente")
        }).OrderBy(r => r.Nombre).ToList();
        var actividad = await db.HistorialTramites.AsNoTracking().Include(h => h.Actor).Where(h => ids.Contains(h.SolicitudId)).OrderByDescending(h => h.Fecha).ThenByDescending(h => h.Id).Take(50).ToListAsync();
        var visuales = await CrearVisuales(solicitudes, rows, supervisores, borradores, start, end, municipio);
        return new MetricasModel { Desde = start, Hasta = end, Municipio = municipio, Filas = rows,
            Borradores = borradores, Supervisores = supervisores, Actividad = actividad, Visuales = visuales,
            MunicipiosDisponibles = await db.Solicitudes.AsNoTracking().Where(s => s.Municipio != "").Select(s => s.Municipio).Distinct().OrderBy(m => m).ToListAsync() };
    }

    private async Task<MetricasVisuales> CrearVisuales(List<Solicitud> solicitudes, List<MetricaMunicipio> rows,
        List<RendimientoSupervisor> supervisores, int borradores, DateTime desde, DateTime hasta, string? municipio)
    {
        static DateTime Recibida(Solicitud s) => s.PrimerEnvio ?? s.FechaEnvio ?? s.FechaCreacion;
        static bool Pendiente(Solicitud s) => s.Estado != EstadoSolicitud.Habilitado && (s.Estado != EstadoSolicitud.Rechazado || s.MotivoRechazo == "campo");
        var cierres = solicitudes.Where(s => s.FechaCierre.HasValue && s.FechaCierre >= Recibida(s))
            .Select(s => (double?)(s.FechaCierre!.Value - Recibida(s)).TotalDays).ToList();
        var habilitadas = rows.Sum(r => r.Habilitadas); var rechazadas = rows.Sum(r => r.Rechazadas);
        var now = reloj.UtcNow; var limiteDocumentos = now.AddDays(-5); var limiteTramites = now.AddDays(-30);
        var ids = solicitudes.Select(s => s.Id).ToArray();
        var documentos = await db.Documentos.CountAsync(d => ids.Contains(d.SolicitudId) && d.Actual && d.EstadoRevision == "pendiente" && d.FechaCarga != null && d.FechaCarga < limiteDocumentos);
        var visitas = await db.Inspecciones.CountAsync(i => ids.Contains(i.SolicitudId) && i.Estado == "pendiente" && i.Programada && i.Fecha < reloj.HoraBolivia);
        var sinProgramar = await db.Inspecciones.CountAsync(i => ids.Contains(i.SolicitudId) && i.Estado == "pendiente" && !i.Programada);
        var meses = new List<MetricaMes>();
        for (var mes = new DateTime(desde.Year, desde.Month, 1); mes <= hasta;)
        {
            var recibidas = solicitudes.Where(s => Recibida(s).Year == mes.Year && Recibida(s).Month == mes.Month).ToList();
            meses.Add(new MetricaMes(mes.ToString("yyyy-MM"), recibidas.Count(s => !s.EsRenovacion), recibidas.Count(s => s.EsRenovacion)));
            if (mes.Year == 9999 && mes.Month == 12) break;
            mes = mes.AddMonths(1);
        }
        return new MetricasVisuales
        {
            Desde = desde.ToString("yyyy-MM-dd"), Hasta = hasta.ToString("yyyy-MM-dd"), Municipio = municipio,
            ActualizadoUtc = now, Total = solicitudes.Count, Habilitadas = habilitadas, Rechazadas = rechazadas,
            Pendientes = rows.Sum(r => r.Pendientes), Borradores = borradores, CierresConFecha = cierres.Count,
            FechasReconstruidas = solicitudes.Count(s => s.PrimerEnvio == null), PromedioDias = cierres.Count == 0 ? null : cierres.Average(),
            TasaAprobacion = habilitadas + rechazadas == 0 ? null : 100d * habilitadas / (habilitadas + rechazadas),
            Meses = meses, Municipios = rows.Select(r => new DistribucionMetrica(r.Municipio, r.Solicitudes)).ToList(),
            Tipos = solicitudes.GroupBy(s => Models.TramiteDetalleModel.DatosDe(s).TipoLaboratorio)
                .Select(g => new DistribucionMetrica(string.IsNullOrWhiteSpace(g.Key) ? "Sin tipo registrado" : g.Key, g.Count())).OrderByDescending(g => g.Cantidad).ThenBy(g => g.Nombre).ToList(),
            Supervisores = supervisores.OrderByDescending(s => s.Completadas).ThenBy(s => s.Nombre).ToList(),
            Alertas = [new("Trámites con más de 30 días abiertos", solicitudes.Count(s => Pendiente(s) && Recibida(s) < limiteTramites), "Desde su primer envío; se excluyen cierres y rechazos definitivos."),
                new("Documentos pendientes de revisión por más de 5 días", documentos, "PDF actuales con fecha de carga registrada; se excluyen fechas desconocidas."),
                new("Visitas iniciadas sin acta enviada", visitas, "Inspecciones pendientes cuya hora de inicio ya pasó (Bolivia)."),
                new("Visitas por programar", sinProgramar, "Inspecciones asignadas que todavía no tienen horario.")]
        };
    }
}
