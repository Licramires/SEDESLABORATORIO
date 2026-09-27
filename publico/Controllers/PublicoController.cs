using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Publico.Models;

namespace SEDESLABORATORIO.Publico.Controllers;

[Area("publico")]
public class PublicoController : Controller
{
    private readonly ApplicationDbContext _context;

    public PublicoController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? tipo,
        EstadoLaboratorio? estado,
        CancellationToken cancellationToken)
    {
        var query = _context.Laboratorios.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(laboratorio =>
                laboratorio.Nombre.ToLower().Contains(normalizedSearch)
                || laboratorio.Tipo.ToLower().Contains(normalizedSearch)
                || laboratorio.Servicios.ToLower().Contains(normalizedSearch));
        }

        if (!string.IsNullOrWhiteSpace(tipo))
        {
            query = query.Where(laboratorio => laboratorio.Tipo == tipo);
        }

        if (estado.HasValue)
        {
            query = query.Where(laboratorio => laboratorio.Estado == estado.Value);
        }

        var laboratorios = await query
            .OrderBy(laboratorio => laboratorio.Nombre)
            .Select(laboratorio => new LaboratorioPublicoViewModel
            {
                Id = laboratorio.Id,
                Nombre = laboratorio.Nombre,
                Tipo = laboratorio.Tipo,
                Lat = laboratorio.Lat,
                Lng = laboratorio.Lng,
                Estado = laboratorio.Estado.ToString().ToLowerInvariant(),
                Servicios = laboratorio.Servicios
            })
            .ToListAsync(cancellationToken);

        var tipos = await _context.Laboratorios
            .AsNoTracking()
            .Select(laboratorio => laboratorio.Tipo)
            .Distinct()
            .OrderBy(tipoDisponible => tipoDisponible)
            .ToListAsync(cancellationToken);

        return View(new PublicoViewModel
        {
            Search = search,
            Tipo = tipo,
            Estado = estado,
            TiposDisponibles = tipos,
            Laboratorios = laboratorios
        });
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id, CancellationToken cancellationToken)
    {
        var laboratorio = await _context.Laboratorios
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new LaboratorioDetalleViewModel
            {
                Id = item.Id,
                Nombre = item.Nombre,
                Tipo = item.Tipo,
                Lat = item.Lat,
                Lng = item.Lng,
                Estado = item.Estado.ToString().ToLowerInvariant(),
                Servicios = item.Servicios
            })
            .SingleOrDefaultAsync(cancellationToken);

        return laboratorio is null ? NotFound() : View(laboratorio);
    }

    [HttpGet]
    public async Task<IActionResult> Ruta(int laboratorioId, CancellationToken cancellationToken)
    {
        var laboratorio = await _context.Laboratorios
            .AsNoTracking()
            .Where(item => item.Id == laboratorioId)
            .Select(item => new { item.Lat, item.Lng })
            .SingleOrDefaultAsync(cancellationToken);

        if (laboratorio is null)
        {
            return NotFound();
        }

        ViewData["Destination"] = $"{laboratorio.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{laboratorio.Lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        return View();
    }
}
