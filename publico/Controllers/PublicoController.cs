using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Publico.Models;
using SEDESLABORATORIO.Infrastructure;

namespace SEDESLABORATORIO.Publico.Controllers;

[Area("publico")]
public class PublicoController : Controller
{
    private readonly IPublicoDatos _context;
    private readonly ILectorArchivos _storage;
    private readonly IWebHostEnvironment _environment;

    public PublicoController(IPublicoDatos context, ILectorArchivos storage, IWebHostEnvironment environment)
    {
        _context = context;
        _storage = storage;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [System.ComponentModel.DataAnnotations.StringLength(200), TextoSeguro] string? search,
        [System.ComponentModel.DataAnnotations.StringLength(100), TextoSeguro] string? tipo,
        EstadoLaboratorio? estado,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var query = Publicables().Where(l => l.VigenciaHasta == null || l.VigenciaHasta > now);
        if (!ModelState.IsValid) { Response.StatusCode = 400; TempData["Error"] = "Revise los filtros: búsqueda de hasta 200 caracteres y un tipo y estado válidos."; query = query.Where(l => false); }

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
                Servicios = laboratorio.Servicios,
                DatosVerificados = laboratorio.DatosVerificados
            })
            .ToListAsync(cancellationToken);

        var tipos = await Publicables()
            .AsNoTracking()
            .Where(l => l.VigenciaHasta == null || l.VigenciaHasta > now)
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
            ,Requisitos = await _context.Publicados().ToListAsync(cancellationToken)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Requerimientos(CancellationToken cancellationToken)
        => View(await _context.Publicados().ToListAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Detalle(int id, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var laboratorio = await Publicables()
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new LaboratorioDetalleViewModel
            {
                Id = item.Id,
                Nombre = item.Nombre,
                Tipo = item.Tipo,
                Lat = item.Lat,
                Lng = item.Lng,
                Estado = item.VigenciaHasta != null && item.VigenciaHasta <= now ? "cerrado" : item.Estado.ToString().ToLowerInvariant(),
                Servicios = item.Servicios,
                Municipio = item.Municipio, Direccion = item.Direccion, Telefono = item.Telefono, Codigo = item.Codigo,
                DatosVerificados = item.DatosVerificados
            })
            .SingleOrDefaultAsync(cancellationToken);

        return laboratorio is null ? NotFound() : View(laboratorio);
    }

    [HttpGet]
    public async Task<IActionResult> Ruta(int laboratorioId, CancellationToken cancellationToken)
    {
        var laboratorio = await Publicables()
            .AsNoTracking()
            .Where(item => item.Id == laboratorioId)
            .Select(item => new { item.Lat, item.Lng })
            .SingleOrDefaultAsync(cancellationToken);

        if (laboratorio is null)
        {
            return NotFound();
        }

        if (Coordenadas.Validar(laboratorio.Lat, laboratorio.Lng))
            ViewData["Destination"] = $"{laboratorio.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{laboratorio.Lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        return View();
    }

    private IQueryable<Laboratorio> Publicables() => _context.Laboratorios.AsNoTracking().Where(l => _environment.IsDevelopment() || l.DatosVerificados && l.PropietarioId != null && l.VigenciaHasta != null);

    [HttpGet]
    public async Task<IActionResult> Plantilla(int id)
    {
        var requisito = await _context.Publicados().SingleOrDefaultAsync(r => r.Id == id);
        var path = requisito is null ? null : _storage.Resolve(requisito.PlantillaPdf);
        return path is null ? NotFound() : PhysicalFile(path, "application/pdf", $"requisito-{id}.pdf");
    }
}
