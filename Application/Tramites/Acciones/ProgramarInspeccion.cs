using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Application.Tramites;

public sealed class ProgramarInspeccion(IAvisosRol avisos, IAgendaReservas agenda, IReloj reloj) : IAccionTramite
{
    public string Permiso => "inspecciones";
    public IReadOnlyCollection<string> Acciones { get; } = ["programar"];
    public async Task<string> Ejecutar(ContextoDecision contexto)
    {
        var s = contexto.Solicitud;
        var model = contexto.Modelo;
        var reason = model.Motivo?.Trim() ?? "";
        string detail;
        contexto.ExigirRol("supervisor"); contexto.ExigirEstado(EstadoSolicitud.Inspeccion);
        var reserva = s.Inspecciones.SingleOrDefault(i => i.Estado == "pendiente" && i.SupervisorId == contexto.Actor.Id);
        ContextoDecision.Exigir(reserva is not null, "No tiene una inspección pendiente asignada.");
        ContextoDecision.Exigir(reason.Length > 0 && model.Fecha.HasValue && model.Fecha > reloj.HoraBolivia && model.Fecha <= DateTime.MaxValue.AddHours(-12) && model.FechaFin > model.Fecha && model.FechaFin <= model.Fecha.Value.AddHours(12), "Indique motivo, inicio futuro y fin posterior, hasta 12 horas después.");
        ContextoDecision.Exigir(!await agenda.HayConflicto(contexto.Actor.Id, model.Fecha!.Value, model.FechaFin!.Value, s.Id), "Tiene otra inspección en ese intervalo.");
        reserva!.Estado = "reprogramada";
        s.Inspecciones.Add(new Inspeccion { SupervisorId = contexto.Actor.Id, Fecha = model.Fecha.Value, FechaFin = model.FechaFin.Value, Programada = true, ListaVerificacionJson = JsonSerializer.Serialize(ExpedienteService.LeerVerificaciones(reserva).Select(v => v with { Resultado = "", Observacion = "" })), PlantillaPdf = reserva.PlantillaPdf, PlantillaVersion = reserva.PlantillaVersion });
        detail = $"Visita programada de {model.Fecha:dd/MM/yyyy HH:mm} a {model.FechaFin:HH:mm} (Bolivia). {reason}";
        await avisos.AvisarRol(RolUsuario.Coordinador, $"Solicitud #{s.Id}: {detail}");
        return detail;
    }
}
