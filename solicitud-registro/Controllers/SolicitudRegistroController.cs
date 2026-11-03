using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEDESLABORATORIO.Data;
using SEDESLABORATORIO.Data.Entities;
using SEDESLABORATORIO.Infrastructure;
using SEDESLABORATORIO.SolicitudRegistro.Models;

namespace SEDESLABORATORIO.SolicitudRegistro.Controllers;

[Area("solicitud-registro"), Authorize(Roles = "propietario")]
public class SolicitudRegistroController(IRegistroDatos db, IAlmacenPdf storage, IWebHostEnvironment environment) : Controller
{
    public IActionResult Index() => RedirectToAction(nameof(Nueva));
    [HttpGet]
    public async Task<IActionResult> Nueva() { await Catalogos(); return View(new NuevaSolicitudViewModel()); }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(55 * 1024 * 1024)]
    public async Task<IActionResult> Nueva(NuevaSolicitudViewModel? modelo, CancellationToken cancellationToken)
    {
        if (modelo is null) return BadRequest("No se pudo leer el formulario. Revise el formato y el tamaño de los archivos.");
        if (!environment.IsDevelopment() && !await db.Publicados().AnyAsync())
            ModelState.AddModelError("", "El SEDES debe publicar el catálogo de requisitos antes de recibir nuevas solicitudes.");
        if (ModelState.IsValid)
        {
            var catalogoError = await ExpedienteService.ValidarCatalogos(db, modelo.TipoLaboratorio, modelo.Municipio, environment.IsDevelopment());
            if (catalogoError is not null) ModelState.AddModelError("", catalogoError);
        }
        if (modelo.DocumentosPdf.Sum(f => f.Length) > 50 * 1024 * 1024)
            ModelState.AddModelError(nameof(modelo.DocumentosPdf), "Los PDF seleccionados superan los 50 MB de esta carga. Guarde el borrador y agregue los restantes uno por uno desde su documentación.");
        foreach (var file in modelo.DocumentosPdf)
        {
            var error = await PdfStorage.ValidateAsync(file);
            if (error is not null) ModelState.AddModelError(nameof(modelo.DocumentosPdf), error);
        }
        if (!ModelState.IsValid) { await Catalogos(); return View(modelo); }
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var propietarioId)) return Forbid();
        var solicitud = new Solicitud
        {
            PropietarioId = propietarioId, Municipio = modelo.Municipio.Trim(),
            Direccion = modelo.Direccion?.Trim() ?? "", Telefono = modelo.Telefono?.Trim() ?? "", Codigo = modelo.Codigo?.Trim() ?? "",
            DatosLaboratorioPropuesto = JsonSerializer.Serialize(new { nombre = modelo.NombreLaboratorio.Trim(), tipo = modelo.TipoLaboratorio.Trim(), coordenadas = new { lat = modelo.Latitud, lng = modelo.Longitud } }),
            Estado = EstadoSolicitud.Borrador, FechaCreacion = DateTime.UtcNow
        };
        var files = new List<string>();
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        try
        {
            db.Solicitudes.Add(solicitud);
            foreach (var file in modelo.DocumentosPdf)
            {
                var key = await storage.SaveAsync(file, cancellationToken);
                files.Add(key);
                solicitud.Documentos.Add(new Documento { RequisitoId = 0, ArchivoPdf = key, NombreArchivo = PdfStorage.DisplayName(file) });
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch { foreach (var key in files) storage.Delete(key); throw; }
        return RedirectToAction("Detalle", "Gestion", new { area = "", id = solicitud.Id });
    }
    [HttpGet]
    public IActionResult Exito() => RedirectToAction("Index", "Gestion", new { area = "" });

    private async Task Catalogos()
    {
        ViewBag.Requisitos = await db.Publicados().ToListAsync();
        ViewBag.Tipos = await db.Catalogos.Where(c => c.Clase == "tipo" && c.Activo).OrderBy(c => c.Nombre).Select(c => c.Nombre).ToListAsync();
        ViewBag.Municipios = await db.Catalogos.Where(c => c.Clase == "municipio" && c.Activo).OrderBy(c => c.Nombre).Select(c => c.Nombre).ToListAsync();
    }
}
