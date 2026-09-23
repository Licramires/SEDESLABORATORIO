using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.SolicitudSeguimiento.Models;

namespace SEDESLABORATORIO.SolicitudSeguimiento.Controllers;

[Area("solicitud-seguimiento")]
public class SolicitudSeguimientoController : Controller
{
    private readonly ApplicationDbContext _context;

    public SolicitudSeguimientoController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var propietarioId))
        {
            return Unauthorized();
        }

        var solicitudes = await _context.Solicitudes
            .AsNoTracking()
            .Where(solicitud => solicitud.PropietarioId == propietarioId)
            .OrderByDescending(solicitud => solicitud.FechaCreacion)
            .Select(solicitud => new SolicitudSeguimientoItemViewModel
            {
                Id = solicitud.Id,
                Estado = solicitud.Estado.ToString().ToLowerInvariant(),
                FechaCreacion = solicitud.FechaCreacion,
                DatosLaboratorioPropuesto = solicitud.DatosLaboratorioPropuesto
            })
            .ToListAsync(cancellationToken);

        return View(solicitudes);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var propietarioId))
        {
            return Unauthorized();
        }

        var solicitud = await _context.Solicitudes
            .AsNoTracking()
            .Where(item => item.Id == id && item.PropietarioId == propietarioId)
            .Select(item => new SolicitudSeguimientoDetalleViewModel
            {
                Id = item.Id,
                Estado = item.Estado.ToString().ToLowerInvariant(),
                FechaCreacion = item.FechaCreacion,
                DatosLaboratorioPropuesto = item.DatosLaboratorioPropuesto,
                PuedeEnviarse = item.Estado == EstadoSolicitud.EnRevision
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (solicitud is null)
        {
            return NotFound();
        }

        return View(solicitud);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enviar(int id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var propietarioId))
        {
            return Unauthorized();
        }

        var solicitud = await _context.Solicitudes
            .SingleOrDefaultAsync(
                item => item.Id == id && item.PropietarioId == propietarioId,
                cancellationToken);

        if (solicitud is null)
        {
            return NotFound();
        }

        if (solicitud.Estado != EstadoSolicitud.EnRevision)
        {
            TempData["Error"] = "La solicitud ya no puede enviarse en su estado actual.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // El contrato actual representa una solicitud enviada como en_revision.
        solicitud.Estado = EstadoSolicitud.EnRevision;
        await _context.SaveChangesAsync(cancellationToken);

        TempData["Success"] = "La solicitud fue enviada para revisión.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private bool TryGetCurrentUserId(out int propietarioId)
    {
        propietarioId = 0;
        var role = User.FindFirstValue(ClaimTypes.Role);
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return string.Equals(role, "propietario", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(claimValue, out propietarioId)
            && propietarioId > 0;
    }
}
