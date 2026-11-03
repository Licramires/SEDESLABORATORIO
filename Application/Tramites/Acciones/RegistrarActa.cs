using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Application.Tramites;

public sealed class RegistrarActa(IAlmacenPdf storage, IAvisosRol avisos, ICalendarioLaboral calendario, IWebHostEnvironment environment, IReloj reloj) : IAccionTramite
{
    public string Permiso => "inspecciones";
    public IReadOnlyCollection<string> Acciones { get; } = ["preparar_acta", "acta"];
    public async Task<string> Ejecutar(ContextoDecision contexto)
    {
        var s = contexto.Solicitud;
        var model = contexto.Modelo;
        var reason = model.Motivo?.Trim() ?? "";
        string detail;
        contexto.ExigirRol("supervisor"); contexto.ExigirEstado(EstadoSolicitud.Inspeccion);
        var inspection = s.Inspecciones.SingleOrDefault(i => i.Estado == "pendiente" && i.SupervisorId == contexto.Actor.Id);
        ContextoDecision.Exigir(inspection is not null, "No tiene una inspección pendiente asignada.");
        ContextoDecision.Exigir(inspection!.Programada && inspection.Fecha <= reloj.HoraBolivia, "No puede registrar un acta antes del inicio de la visita programada.");
        ContextoDecision.Exigir(model.Accion == "preparar_acta" || model.Pdf is not null, "Adjunte el acta firmada en PDF.");
        var checks = ExpedienteService.LeerVerificaciones(inspection);
        ContextoDecision.Exigir(checks.Count > 0 || environment.IsDevelopment(), "Configure la lista institucional de verificación antes de registrar actas reales.");
        ContextoDecision.Exigir(environment.IsDevelopment() || storage.Resolve(inspection.PlantillaPdf) != null, "La inspección necesita una plantilla institucional disponible. Configure el formulario y reasigne la visita.");
        ContextoDecision.Exigir(model.Verificaciones.Count == checks.Count && model.Verificaciones.Select(v => v.Id).Distinct().Count() == checks.Count && checks.All(c => model.Verificaciones.Any(v => v.Id == c.Id)), "Complete cada punto de la lista de verificación del acta.");
        model.Resultado ??= model.Verificaciones.Any(v => v.Resultado == "observado") ? "observado" : "aprobado";
        ContextoDecision.Exigir(model.Resultado is "aprobado" or "observado" or "rechazado", "Seleccione un dictamen válido.");
        ContextoDecision.Exigir(checks.Count > 0 || reason.Length > 0, "Describa los hallazgos de la inspección.");
        ContextoDecision.Exigir(model.Accion == "preparar_acta" || checks.Count == 0 || model.Verificaciones.Any(v => v.Resultado != "no_aplica"), "No puede enviar un acta con todos los puntos marcados como no aplicables.");
        ContextoDecision.Exigir(model.Resultado != "aprobado" || model.Verificaciones.All(v => v.Resultado != "observado"), "Un acta con incumplimientos no puede tener resultado aprobado.");
        ContextoDecision.Exigir(model.Verificaciones.All(v => v.Resultado != "observado" || !string.IsNullOrWhiteSpace(v.Observacion)), "Describa cada incumplimiento observado.");
        ContextoDecision.Exigir(model.Resultado == "aprobado" || model.Verificaciones.Any(v => v.Resultado == "observado") || reason.Length > 0, "Fundamente el dictamen si no hay incumplimientos en la lista.");
        var completadas = checks.Select(c => { var v = model.Verificaciones.Single(v => v.Id == c.Id); return c with { Resultado = v.Resultado, Observacion = v.Resultado == "observado" ? v.Observacion!.Trim() : "" }; }).ToList();
        inspection.ListaVerificacionJson = JsonSerializer.Serialize(completadas);
        inspection.NotasAdicionales = reason;
        var hallazgos = completadas.Where(c => c.Resultado == "observado").Select(c => $"{c.Nombre}: {c.Observacion}");
        inspection.Acta = string.Join("\n", hallazgos.Append(reason).Where(t => !string.IsNullOrWhiteSpace(t)));
        if (inspection.Acta.Length == 0) inspection.Acta = "Verificaciones aplicables conformes; sin incumplimientos registrados.";
        inspection.Resultado = model.Resultado!;
        inspection.DiasSubsanacion = model.DiasSubsanacion;
        inspection.Citacion = model.Citacion?.Trim() ?? "";
        if (model.Resultado == "observado") inspection.PlazoSubsanacion = await calendario.PlazoHabil(reloj.HoraBolivia, model.DiasSubsanacion);
        else inspection.PlazoSubsanacion = null;
        if (model.Accion == "preparar_acta") { return "Contenido del acta preparado para descargar, firmar y adjuntar."; }
        contexto.ArchivoNuevo = await storage.SaveAsync(model.Pdf!);
        inspection.ActaPdf = contexto.ArchivoNuevo; inspection.Estado = "registrada";
        s.Estado = EstadoSolicitud.RevisionActa;
        detail = $"Acta registrada: {model.Resultado}. {reason}";
        await avisos.AvisarRol(RolUsuario.Coordinador, $"Acta de solicitud #{s.Id} pendiente de revisión.");
        return detail;
    }
}
