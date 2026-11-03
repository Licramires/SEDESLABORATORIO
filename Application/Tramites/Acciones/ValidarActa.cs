using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Application.Tramites;

public sealed class ValidarActa(IAvisosRol avisos, IReloj reloj) : IAccionTramite
{
    public string Permiso => "actas";
    public IReadOnlyCollection<string> Acciones { get; } = ["aprobar_acta", "rechazar_acta"];
    public async Task<string> Ejecutar(ContextoDecision contexto)
    {
        var s = contexto.Solicitud;
        var model = contexto.Modelo;
        var reason = model.Motivo?.Trim() ?? "";
        string detail;
        contexto.ExigirRol("coordinador"); contexto.ExigirEstado(EstadoSolicitud.RevisionActa);
        ContextoDecision.Exigir(reason.Length > 0, "Registre el fundamento de la decisión.");
        var acta = s.Inspecciones.OrderByDescending(i => i.Id).First(i => i.Estado == "registrada");
        acta.Estado = model.Accion == "aprobar_acta" ? "validada" : "rechazada";
        s.Estado = model.Accion == "rechazar_acta" ? EstadoSolicitud.Aprobado : acta.Resultado switch
        {
            "aprobado" => EstadoSolicitud.RevisionLegal,
            "observado" => EstadoSolicitud.Observado,
            _ => EstadoSolicitud.Rechazado
        };
        if (s.Estado is EstadoSolicitud.Observado or EstadoSolicitud.Rechazado) s.OrigenObservacion = "campo";
        if (s.Estado == EstadoSolicitud.Rechazado) s.MotivoRechazo = "campo";
        if (s.Estado == EstadoSolicitud.RevisionLegal) s.RequiereNuevaInspeccion = false;
        if (model.Accion == "aprobar_acta" && acta.Resultado == "aprobado")
            foreach (var c in s.Citaciones.Where(c => c.FechaAtencion == null)) c.FechaAtencion = reloj.UtcNow;
        detail = $"Revisión de acta: {acta.Estado}. {reason}";
        if (s.Estado == EstadoSolicitud.RevisionLegal) await avisos.AvisarRol(RolUsuario.Abogado, $"Solicitud #{s.Id} pendiente de revisión legal (licencia y plano).");
        return detail;
    }
}
