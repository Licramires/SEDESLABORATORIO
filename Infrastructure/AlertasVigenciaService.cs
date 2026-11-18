using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Infrastructure;

public sealed class AlertasVigenciaService(IAlertasDatos db, IConfiguration? configuration, IReloj reloj) : IAlertasVigencia
{
    public async Task AlertasVigencia()
    {
        await using var tx = await db.BeginTransactionAsync();
        var now = reloj.UtcNow;
        var anticipacion = Math.Clamp(configuration?.GetValue<int?>("Alertas:LicenciaDias") ?? 30, 0, 365);
        var limit = now.AddDays(anticipacion);
        var labs = await db.Laboratorios.Where(l => l.PropietarioId != null && l.VigenciaHasta <= limit).ToListAsync();
        foreach (var lab in labs)
        {
            var vencido = lab.VigenciaHasta <= now;
            if (vencido) lab.Estado = EstadoLaboratorio.Cerrado;
            var key = $"vigencia:{lab.Id}:{lab.VigenciaHasta:yyyyMMdd}:{vencido}";
            if (!await db.Notificaciones.AnyAsync(n => n.Clave == key))
                db.Notificaciones.Add(new Notificacion { UsuarioId = lab.PropietarioId!.Value, Clave = key, Mensaje = $"La habilitación de {lab.Nombre} {(vencido ? "venció" : "vence")} el {lab.VigenciaHasta:dd/MM/yyyy}. Puede solicitar su renovación." });
        }
        var today = reloj.HoraBolivia.Date;
        var citaciones = await db.Citaciones.Include(c => c.Solicitud).Where(c => c.FechaAtencion == null && c.Solicitud.FechaCierre == null).ToListAsync();
        foreach (var c in citaciones.Where(c => c.Vencimiento.Date.AddDays(-c.AnticipacionDias) <= today))
            await AvisoPlazo($"citacion:{c.Id}", c.Solicitud, c.SupervisorId, c.Vencimiento, today, $"Citación #{c.Id} de solicitud #{c.SolicitudId}");
        var plazos = await db.Inspecciones.Include(i => i.Solicitud).Where(i => i.Estado == "validada" && i.Resultado == "observado" && i.PlazoSubsanacion != null && i.Solicitud.FechaCierre == null &&
            !i.Solicitud.Inspecciones.Any(n => n.Id > i.Id && n.Estado == "validada" && n.Resultado == "aprobado")).ToListAsync();
        var antesPlazo = Math.Clamp(configuration?.GetValue<int?>("Alertas:SubsanacionDias") ?? 5, 0, 30);
        foreach (var i in plazos.Where(i => i.PlazoSubsanacion!.Value.Date.AddDays(-antesPlazo) <= today))
            await AvisoPlazo($"subsanacion:{i.Id}", i.Solicitud, i.SupervisorId, i.PlazoSubsanacion!.Value, today, $"Plazo de subsanación de solicitud #{i.SolicitudId}");
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private async Task AvisoPlazo(string clave, Solicitud s, int supervisor, DateTime vence, DateTime today, string titulo)
    {
        var vencido = vence.Date < today;
        foreach (var usuario in new[] { s.PropietarioId, supervisor }.Distinct())
        {
            var key = $"{clave}:{vence:yyyyMMdd}:{vencido}:{usuario}";
            if (!await db.Notificaciones.AnyAsync(n => n.Clave == key))
                db.Notificaciones.Add(new Notificacion { UsuarioId = usuario, Clave = key, Mensaje = $"{titulo}: {(vencido ? "venció" : "vence")} el {vence:dd/MM/yyyy} (Bolivia). Revise el expediente y las acciones pendientes." });
        }
    }
}
