using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Application.Tramites;

public sealed class AsignarInspeccion(IAsignacionesDatos db, IAlmacenPdf storage, IAgendaReservas agenda, IReloj reloj) : IAccionTramite
{
    public string Permiso => "asignaciones";
    public IReadOnlyCollection<string> Acciones { get; } = ["asignar"];
    public async Task<string> Ejecutar(ContextoDecision contexto)
    {
        var s = contexto.Solicitud;
        var model = contexto.Modelo;
        var reason = model.Motivo?.Trim() ?? "";
        string detail;
        contexto.ExigirRol("coordinador"); contexto.ExigirEstado(EstadoSolicitud.Aprobado, EstadoSolicitud.Inspeccion, EstadoSolicitud.Observado, EstadoSolicitud.Rechazado);
        ContextoDecision.Exigir(s.Estado != EstadoSolicitud.Observado || s.OrigenObservacion == "campo", "Debe subsanar y revisar la documentación antes de asignar una inspección.");
        ContextoDecision.Exigir(s.Estado != EstadoSolicitud.Rechazado || (s.MotivoRechazo is "" or "campo" && s.Inspecciones.Any(i => i.Estado == "validada" && i.Resultado == "rechazado")), "Una solicitud rechazada definitivamente requiere un nuevo trámite.");
        ExpedienteService.ValidarDatos(s);
        ExpedienteService.ComprobarDocumentos(s, await ExpedienteService.Requisitos(db, s), storage, revisados: true);
        if (s.EsRenovacion)
            ContextoDecision.Exigir(!await db.Solicitudes.AnyAsync(x => x.Id != s.Id && x.LaboratorioId == s.LaboratorioId && x.Estado != EstadoSolicitud.Habilitado && (x.Estado != EstadoSolicitud.Rechazado || x.MotivoRechazo == "campo")), "Ya existe otra renovación activa para este establecimiento.");
        ContextoDecision.Exigir(reason.Length > 0, "Indique el motivo de asignación o reprogramación.");
        ContextoDecision.Exigir(model.Fecha.HasValue || !model.FechaFin.HasValue, "Indique el inicio antes de establecer el fin de la visita.");
        ContextoDecision.Exigir(!model.Fecha.HasValue || model.Fecha.Value > reloj.HoraBolivia && model.Fecha.Value <= DateTime.MaxValue.AddHours(-12), "La inspección debe programarse en una fecha futura (hora Bolivia).");
        ContextoDecision.Exigir(await db.Usuarios.AnyAsync(u => u.Id == model.SupervisorId && u.Activo && u.Rol == RolUsuario.Supervisor), "Seleccione un supervisor activo.");
        var fin = model.FechaFin ?? model.Fecha?.AddHours(1);
        if (model.Fecha.HasValue)
        {
            ContextoDecision.Exigir(fin > model.Fecha && fin <= model.Fecha.Value.AddHours(12), "Indique un fin posterior al inicio, hasta 12 horas después.");
            ContextoDecision.Exigir(!await agenda.HayConflicto(model.SupervisorId!.Value, model.Fecha.Value, fin!.Value, s.Id), "El supervisor ya tiene otra inspección en ese intervalo.");
        }
        foreach (var old in s.Inspecciones.Where(i => i.Estado == "pendiente")) old.Estado = "reprogramada";
        var lista = await db.Catalogos.Where(c => c.Activo && c.Clase == "inspeccion").OrderBy(c => c.Id).Select(c => new Verificacion(c.Id, c.Nombre, c.Referencia, c.Version, "", "")).ToListAsync();
        var plantillaActa = await db.Catalogos.Where(c => c.Activo && c.Clase == "formulario_acta").OrderByDescending(c => c.Id).FirstOrDefaultAsync();
        s.Inspecciones.Add(new Inspeccion { SupervisorId = model.SupervisorId!.Value, Fecha = model.Fecha ?? default, FechaFin = fin, Programada = model.Fecha.HasValue, ListaVerificacionJson = JsonSerializer.Serialize(lista), PlantillaPdf = plantillaActa?.ArchivoPdf ?? "", PlantillaVersion = plantillaActa?.Version ?? 0 });
        s.Estado = EstadoSolicitud.Inspeccion;
        db.Notificaciones.Add(new Notificacion { UsuarioId = model.SupervisorId.Value, Mensaje = $"Inspección asignada: solicitud #{s.Id}. {(model.Fecha.HasValue ? $"{model.Fecha:dd/MM/yyyy HH:mm} (Bolivia)" : "Organice fecha y horario en su agenda.")}" });
        s.FechaCierre = null;
        detail = $"Supervisor #{model.SupervisorId} asignado. {(model.Fecha.HasValue ? $"Visita: {model.Fecha:dd/MM/yyyy HH:mm} a {fin:HH:mm} (Bolivia)." : "Pendiente de programación por el supervisor.")} {reason}";
        return detail;
    }
}
