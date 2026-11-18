using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Infrastructure;

public sealed class RegistroTramite(IRegistroHistorialDatos db) : IRegistroTramite
{
    public void Registrar(Solicitud solicitud, int actorId, string detalle)
    {
        solicitud.Version++;
        db.HistorialTramites.Add(new HistorialTramite { Solicitud = solicitud, ActorId = actorId, Detalle = detalle });
        db.Notificaciones.Add(new Notificacion { UsuarioId = solicitud.PropietarioId, Mensaje = $"Solicitud #{solicitud.Id}: {detalle}" });
    }
}
