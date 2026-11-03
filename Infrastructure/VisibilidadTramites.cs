using SEDESLABORATORIO.Data.Entities;

namespace SEDESLABORATORIO.Infrastructure;

public sealed class VisibilidadTramites(ISolicitudesDatos db) : IVisibilidadTramites
{
    public IQueryable<Solicitud> Visibles(int actorId, string rol)
    {
        var query = db.Solicitudes.AsQueryable();
        return rol switch
        {
            "propietario" => query.Where(s => s.PropietarioId == actorId),
            "supervisor" => query.Where(s => s.Inspecciones.Any(i => i.SupervisorId == actorId)),
            "abogado" => query.Where(s => s.Estado == EstadoSolicitud.RevisionLegal || s.Estado == EstadoSolicitud.Habilitado || s.Historial.Any(h => h.ActorId == actorId)),
            "coordinador" => query.Where(s => s.Estado != EstadoSolicitud.Borrador),
            _ => query.Where(s => false)
        };
    }
}
