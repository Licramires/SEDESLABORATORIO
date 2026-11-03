using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.Models;

namespace SEDESLABORATORIO.Controllers;

public partial class GestionController
{
    private Task<AgendaSupervisorModel> CargarAgenda(DateTime? fecha, bool rutas) => agendaSupervisor.Consultar(Actor, fecha, rutas);

    [HttpGet, Authorize(Roles = "supervisor")]
    public async Task<IActionResult> Rutas(DateTime? fecha)
    {
        if (!ModelState.IsValid) return BadRequest("Fecha inválida.");
        return View("Agenda", await CargarAgenda(fecha, true));
    }

    [HttpGet, Authorize(Roles = "supervisor")]
    public async Task<IActionResult> Actas()
    {
        return View(await db.Inspecciones.AsNoTracking().Include(i => i.Solicitud)
            .Where(i => i.SupervisorId == Actor && (i.Estado == "registrada" || i.Estado == "validada" || i.Estado == "rechazada"))
            .OrderByDescending(i => i.Fecha).ThenByDescending(i => i.Id).ToListAsync());
    }
}
