using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Infrastructure;

public sealed class AgendaReservas(IAgendaDatos db) : IAgendaReservas
{
    public async Task<bool> HayConflicto(int supervisorId, DateTime inicio, DateTime fin, int solicitudId)
    {
        var otras = await db.Inspecciones.Where(i => i.SupervisorId == supervisorId && i.Estado != "reprogramada" && i.Programada &&
            !(i.SolicitudId == solicitudId && i.Estado == "pendiente") && i.Fecha < fin).ToListAsync();
        return otras.Any(i => ExpedienteService.Fin(i) > inicio);
    }
}
