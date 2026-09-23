using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.SolicitudRegistro.Models;

namespace SEDESLABORATORIO.SolicitudRegistro.Controllers;

[Area("solicitud-registro")]
[Authorize(Roles = "propietario")]
public class SolicitudRegistroController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public SolicitudRegistroController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult Nueva()
    {
        return View(new NuevaSolicitudViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Nueva(
        NuevaSolicitudViewModel modelo,
        CancellationToken cancellationToken)
    {
        ValidatePdfFiles(modelo.DocumentosPdf);

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var propietarioId))
        {
            return Forbid();
        }

        var solicitud = new Solicitud
        {
            PropietarioId = propietarioId,
            DatosLaboratorioPropuesto = JsonSerializer.Serialize(new
            {
                nombre = modelo.NombreLaboratorio.Trim(),
                tipo = modelo.TipoLaboratorio.Trim(),
                coordenadas = new { lat = modelo.Latitud, lng = modelo.Longitud }
            }),
            Estado = EstadoSolicitud.EnRevision,
            FechaCreacion = DateTime.UtcNow
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _context.Solicitudes.Add(solicitud);
        await _context.SaveChangesAsync(cancellationToken);

        var uploadDirectory = Path.Combine(
            _environment.WebRootPath,
            "uploads",
            "solicitudes",
            solicitud.Id.ToString());
        Directory.CreateDirectory(uploadDirectory);

        foreach (var file in modelo.DocumentosPdf)
        {
            var storedFileName = $"{Guid.NewGuid():N}.pdf";
            var filePath = Path.Combine(uploadDirectory, storedFileName);
            await using var stream = System.IO.File.Create(filePath);
            await file.CopyToAsync(stream, cancellationToken);

            _context.Documentos.Add(new Documento
            {
                SolicitudId = solicitud.Id,
                RequisitoId = 0,
                ArchivoPdf = Path.Combine("uploads", "solicitudes", solicitud.Id.ToString(), storedFileName)
                    .Replace('\\', '/')
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["SolicitudCreada"] = solicitud.Id;
        return RedirectToAction(nameof(Exito));
    }

    [HttpGet]
    public IActionResult Exito()
    {
        return View();
    }

    private void ValidatePdfFiles(IEnumerable<Microsoft.AspNetCore.Http.IFormFile> files)
    {
        const long maxFileSize = 10 * 1024 * 1024;

        foreach (var file in files)
        {
            if (file.Length == 0 || file.Length > maxFileSize)
            {
                ModelState.AddModelError(nameof(NuevaSolicitudViewModel.DocumentosPdf),
                    "Cada PDF debe tener contenido y no superar los 10 MB.");
                continue;
            }

            if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(NuevaSolicitudViewModel.DocumentosPdf),
                    "Solo se permiten archivos PDF.");
            }
        }
    }
}