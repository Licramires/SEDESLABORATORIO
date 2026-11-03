using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Application.Tramites;

public interface IConsultaAgenda
{
    Task<AgendaSupervisorModel> Consultar(int actorId, DateTime? fecha, bool rutas);
}

public sealed class ConsultaAgenda(IAgendaDatos db, IReloj reloj) : IConsultaAgenda
{
    public async Task<AgendaSupervisorModel> Consultar(int actorId, DateTime? fecha, bool rutas)
    {
        var visitas = await db.Inspecciones.AsNoTracking().Include(i => i.Solicitud)
            .Where(i => i.SupervisorId == actorId && i.Estado == "pendiente" && i.Solicitud.Estado == EstadoSolicitud.Inspeccion)
            .OrderBy(i => i.Fecha).ThenBy(i => i.Id).ToListAsync();
        return new AgendaSupervisorModel
        {
            Hoy = reloj.HoraBolivia.ToString("yyyy-MM-dd"), Ahora = reloj.HoraBolivia.ToString("yyyy-MM-ddTHH:mm"), Fecha = (fecha ?? reloj.HoraBolivia).ToString("yyyy-MM-dd"), SoloRutas = rutas,
            Visitas = visitas.Select(i =>
            {
                var datos = TramiteDetalleModel.DatosDe(i.Solicitud);
                decimal? lat = null, lng = null;
                try { var d = ExpedienteService.ValidarDatos(i.Solicitud); lat = d.Lat; lng = d.Lng; } catch (InvalidOperationException) { }
                return new VisitaSupervisor { Id = i.Id, SolicitudId = i.SolicitudId, Nombre = datos.NombreLaboratorio,
                    Municipio = i.Solicitud.Municipio, Direccion = i.Solicitud.Direccion, Renovacion = i.Solicitud.EsRenovacion,
                    Inicio = i.Programada ? i.Fecha.ToString("yyyy-MM-ddTHH:mm") : null,
                    Fin = i.Programada ? ExpedienteService.Fin(i).ToString("yyyy-MM-ddTHH:mm") : null,
                    ActaPreparada = !string.IsNullOrWhiteSpace(i.Acta), Lat = lat, Lng = lng };
            }).ToList()
        };
    }

}
