using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;

namespace SEDESLABORATORIO.Controllers;
[Authorize, AutoValidateAntiforgeryToken]
public class NotificacionesController(INotificacionesDatos db) : Controller
{
    private int Actor => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet]
    public async Task<IActionResult> Index() => View(await db.Notificaciones.AsNoTracking().Where(n => n.UsuarioId == Actor).OrderByDescending(n => n.Fecha).Take(200).ToListAsync());
    [HttpPost]
    public async Task<IActionResult> Leer([Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] int id)
    {
        if (!ModelState.IsValid) return BadRequest("Revise los identificadores, versiones y opciones del formulario.");
        var n = await db.Notificaciones.SingleOrDefaultAsync(n => n.Id == id && n.UsuarioId == Actor);
        if (n is null) return NotFound();
        n.Leida = true; await db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
}
