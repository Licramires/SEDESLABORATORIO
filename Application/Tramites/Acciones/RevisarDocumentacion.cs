using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Application.Tramites;

public sealed class RevisarDocumentacion(IRequisitosDatos db, IAlmacenPdf storage, IAvisosRol avisos, IReloj reloj) : IAccionTramite
{
    public string Permiso => "documentos";
    public IReadOnlyCollection<string> Acciones { get; } = ["aprobar_documentos", "observar_documentos", "rechazar_documentos"];
    public async Task<string> Ejecutar(ContextoDecision contexto)
    {
        var s = contexto.Solicitud;
        var model = contexto.Modelo;
        var reason = model.Motivo?.Trim() ?? "";
        string detail;
        contexto.ExigirRol("coordinador"); contexto.ExigirEstado(EstadoSolicitud.EnRevision);
        if (model.Accion == "aprobar_documentos" && reason.Length == 0) reason = "Documentación completa y revisada individualmente.";
        if (model.Accion == "observar_documentos" && reason.Length == 0)
            reason = string.Join("\n", s.Documentos.Where(d => d.Actual && d.EstadoRevision == "observado" && !string.IsNullOrWhiteSpace(d.Observacion)).Select(d => $"{d.NombreRequisito}: {d.Observacion}"));
        ContextoDecision.Exigir(reason.Length > 0, "Registre el fundamento de la revisión.");
        if (model.Accion == "aprobar_documentos")
        {
            ExpedienteService.ComprobarDocumentos(s, await ExpedienteService.Requisitos(db, s), storage, revisados: true);
            s.Estado = s.OrigenObservacion == "legal" && !s.RequiereNuevaInspeccion ? EstadoSolicitud.RevisionLegal : EstadoSolicitud.Aprobado;
            if (s.Estado == EstadoSolicitud.RevisionLegal) await avisos.AvisarRol(RolUsuario.Abogado, $"Solicitud #{s.Id}: subsanación documental lista para revisión legal.");
            s.OrigenObservacion = "";
        }
        else if (model.Accion == "observar_documentos")
        {
            ContextoDecision.Exigir(s.Documentos.Any(d => d.Actual && d.EstadoRevision == "observado"), "Observe al menos un documento e indique qué debe corregirse.");
            s.Estado = EstadoSolicitud.Observado;
            if (s.OrigenObservacion != "legal") s.OrigenObservacion = "documental";
        }
        else { s.Estado = EstadoSolicitud.Rechazado; s.MotivoRechazo = "documental"; s.FechaCierre = reloj.UtcNow; }
        detail = $"Revisión documental: {ApplicationDbContext.EstadoTexto(s.Estado)}. {reason}";
        return detail;
    }
}
